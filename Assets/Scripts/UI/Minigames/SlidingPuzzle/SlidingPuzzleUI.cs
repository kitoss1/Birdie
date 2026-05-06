using System;
using System.Collections.Generic;
using Birdie.Data;
using Birdie.Debug;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Birdie.UI.Minigames
{
    /// <summary>
    /// Main controller for the Sliding Puzzle minigame.
    /// Manages grid state, image slicing, tile spawning, shuffle, input handling,
    /// solve detection, scoring, and game over flow.
    /// </summary>
    public sealed class SlidingPuzzleUI : MonoBehaviour, IMinigame
    {
        private const int EmptyCell = -1;

        [Header("Grid")]
        [SerializeField]
        [Tooltip("Inactive tile template with RawImage and Button components")]
        private GameObject m_tileTemplate;

        [SerializeField]
        [Tooltip("Parent transform where tiles are spawned")]
        private RectTransform m_gridContainer;

        [SerializeField]
        [Tooltip("Pool of textures to randomly pick from when building the puzzle")]
        private Texture[] m_sourceImages;

        [Header("HUD")]
        [SerializeField]
        [Tooltip("Text displaying the current move count")]
        private TextMeshProUGUI m_moveCountText;

        [SerializeField]
        [Tooltip("Reusable score display component")]
        private MinigameScoreDisplay m_scoreDisplay;

        [Header("Reward Bar")]
        [SerializeField]
        [Tooltip("Progress bar showing score thresholds and friendship rewards")]
        private MinigameRewardBar m_rewardBar;

        [Header("Game Over")]
        [SerializeField]
        [Tooltip("Reusable game over panel component")]
        private MinigameGameOverPanel m_gameOverPanel;

        [Header("Difficulty Defaults")]
        [SerializeField]
        [Tooltip("Grid dimension (e.g. 3 for 3x3)")]
        [Min(2)]
        private int m_gridSize = 3;

        [SerializeField]
        [Tooltip("Number of random moves to shuffle the puzzle")]
        [Min(1)]
        private int m_shuffleMoves = 50;

        [SerializeField]
        [Tooltip("Base score before move penalty")]
        [Min(1)]
        private int m_maxScore = 100;

        [SerializeField]
        [Tooltip("Duration of tile slide animation in seconds")]
        [Min(0.01f)]
        private float m_slideDuration = 0.15f;

        public event Action GameClosed;

        public int FriendshipReward => MinigameRewardTier.ResolveReward(m_rewardTiers, m_score, m_completionReward);

        private MinigameRewardTier[] m_rewardTiers;
        private int m_completionReward;
        private readonly List<SlidingPuzzleTile> m_tiles = new List<SlidingPuzzleTile>();
        private int[] m_grid;
        private int m_emptyIndex;
        private int m_moveCount;
        private int m_score;
        private bool m_isInputLocked;
        private SlidingPuzzleState m_currentState;

        private enum SlidingPuzzleState
        {
            WaitingToStart,
            Playing,
            GameOver,
        }

        private void Awake()
        {
            if (m_gameOverPanel != null)
            {
                m_gameOverPanel.CloseClicked += OnCloseClicked;
            }
        }

        private void OnDestroy()
        {
            if (m_gameOverPanel != null)
            {
                m_gameOverPanel.CloseClicked -= OnCloseClicked;
            }

            CleanupTiles();
        }

        public void StartGame()
        {
            m_moveCount = 0;
            m_score = m_maxScore;
            m_isInputLocked = false;
            m_currentState = SlidingPuzzleState.WaitingToStart;

            m_scoreDisplay?.UpdateScore(m_score);
            m_rewardBar?.UpdateScore(m_score);
            m_gameOverPanel?.Hide();
            UpdateMoveCountDisplay();

            CleanupTiles();
            BuildGrid();
            ShuffleGrid(m_shuffleMoves);
            PositionAllTiles();
            SetAllTilesInteractable(true);

            m_currentState = SlidingPuzzleState.Playing;

            DebugBase.Log(
                $"[{nameof(SlidingPuzzleUI)}] Game started with {m_gridSize}x{m_gridSize} grid",
                DebugCategory.UI);
        }

        public void SetRewardTiers(MinigameRewardTier[] rewardTiers, int completionReward)
        {
            m_rewardTiers = rewardTiers;
            m_completionReward = completionReward;

            if (m_rewardBar != null)
            {
                m_rewardBar.Initialize(rewardTiers, completionReward, reversed: true);
            }
        }

        public void SetDifficulty(MinigameDifficultySettings settings)
        {
            if (settings is SlidingPuzzleDifficultySettings puzzleSettings)
            {
                m_gridSize = puzzleSettings.GridSize;
                m_shuffleMoves = puzzleSettings.ShuffleMoves;
                m_maxScore = puzzleSettings.MaxScore;
                m_slideDuration = puzzleSettings.SlideDuration;
            }
            else if (settings != null)
            {
                DebugBase.LogWarning(
                    $"[{nameof(SlidingPuzzleUI)}] Received unexpected difficulty settings type: {settings.GetType().Name}",
                    DebugCategory.UI);
            }
        }

        private void BuildGrid()
        {
            int totalCells = m_gridSize * m_gridSize;
            m_grid = new int[totalCells];
            m_emptyIndex = totalCells - 1;

            for (int i = 0; i < totalCells - 1; i++)
            {
                m_grid[i] = i;
            }

            m_grid[m_emptyIndex] = EmptyCell;

            if (m_tileTemplate == null || m_gridContainer == null || m_sourceImages == null || m_sourceImages.Length == 0)
            {
                DebugBase.LogWarning(
                    $"[{nameof(SlidingPuzzleUI)}] Missing references: template, container, or source images",
                    DebugCategory.UI);
                return;
            }

            Texture selectedImage = m_sourceImages[UnityEngine.Random.Range(0, m_sourceImages.Length)];
            Vector2 tileSize = CalculateTileSize();

            for (int i = 0; i < totalCells - 1; i++)
            {
                int row = i / m_gridSize;
                int col = i % m_gridSize;
                Rect uvRect = CalculateUvRect(row, col);

                GameObject tileObj = Instantiate(m_tileTemplate, m_gridContainer);
                tileObj.SetActive(true);

                var tile = tileObj.GetComponent<SlidingPuzzleTile>();
                if (tile == null)
                {
                    Destroy(tileObj);
                    continue;
                }

                tile.Initialize(selectedImage, uvRect, i, i);
                tile.SetSize(tileSize);
                tile.Pressed += OnTilePressed;
                m_tiles.Add(tile);
            }
        }

        private void ShuffleGrid(int moveCount)
        {
            int previousEmpty = -1;

            for (int i = 0; i < moveCount; i++)
            {
                List<int> neighbors = GetValidNeighbors(m_emptyIndex);

                if (neighbors.Count > 1 && previousEmpty >= 0)
                {
                    neighbors.Remove(previousEmpty);
                }

                int randomNeighbor = neighbors[UnityEngine.Random.Range(0, neighbors.Count)];
                previousEmpty = m_emptyIndex;

                m_grid[m_emptyIndex] = m_grid[randomNeighbor];
                m_grid[randomNeighbor] = EmptyCell;
                m_emptyIndex = randomNeighbor;
            }

            UpdateTileGridIndices();
        }

        private void UpdateTileGridIndices()
        {
            int totalCells = m_gridSize * m_gridSize;

            for (int gridPos = 0; gridPos < totalCells; gridPos++)
            {
                int tileId = m_grid[gridPos];
                if (tileId == EmptyCell || tileId < 0 || tileId >= m_tiles.Count)
                {
                    continue;
                }

                m_tiles[tileId].SetGridIndex(gridPos);
            }
        }

        private void PositionAllTiles()
        {
            int totalCells = m_gridSize * m_gridSize;

            for (int gridPos = 0; gridPos < totalCells; gridPos++)
            {
                int tileId = m_grid[gridPos];
                if (tileId == EmptyCell || tileId < 0 || tileId >= m_tiles.Count)
                {
                    continue;
                }

                m_tiles[tileId].SetPosition(GetCellPosition(gridPos));
            }
        }

        private void OnTilePressed(int gridIndex)
        {
            if (m_currentState != SlidingPuzzleState.Playing || m_isInputLocked)
            {
                return;
            }

            if (!IsAdjacentToEmpty(gridIndex))
            {
                return;
            }

            SlideTileAsync(gridIndex).Forget();
        }

        private async UniTaskVoid SlideTileAsync(int gridIndex)
        {
            m_isInputLocked = true;

            int tileId = m_grid[gridIndex];
            if (tileId == EmptyCell || tileId < 0 || tileId >= m_tiles.Count)
            {
                m_isInputLocked = false;
                return;
            }

            SlidingPuzzleTile tile = m_tiles[tileId];

            int targetIndex = m_emptyIndex;
            Vector2 targetPosition = GetCellPosition(targetIndex);

            m_grid[targetIndex] = tileId;
            m_grid[gridIndex] = EmptyCell;
            m_emptyIndex = gridIndex;
            tile.SetGridIndex(targetIndex);

            await tile.SlideToAsync(targetPosition, m_slideDuration);

            m_moveCount++;
            UpdateMoveCountDisplay();
            UpdateScore();

            if (IsSolved())
            {
                OnPuzzleSolved();
            }
            else
            {
                m_isInputLocked = false;
            }
        }

        private bool IsAdjacentToEmpty(int gridIndex)
        {
            int gridRow = gridIndex / m_gridSize;
            int gridCol = gridIndex % m_gridSize;
            int emptyRow = m_emptyIndex / m_gridSize;
            int emptyCol = m_emptyIndex % m_gridSize;

            bool sameRow = gridRow == emptyRow && Mathf.Abs(gridCol - emptyCol) == 1;
            bool sameCol = gridCol == emptyCol && Mathf.Abs(gridRow - emptyRow) == 1;

            return sameRow || sameCol;
        }

        private bool IsSolved()
        {
            int totalCells = m_gridSize * m_gridSize;

            for (int i = 0; i < totalCells - 1; i++)
            {
                if (m_grid[i] != i)
                {
                    return false;
                }
            }

            return true;
        }

        private void OnPuzzleSolved()
        {
            m_currentState = SlidingPuzzleState.GameOver;
            SetAllTilesInteractable(false);

            DebugBase.Log(
                $"[{nameof(SlidingPuzzleUI)}] Puzzle solved! Moves: {m_moveCount}, Score: {m_score}",
                DebugCategory.UI);

            m_gameOverPanel?.Show(m_score, FriendshipReward);
        }

        private void UpdateScore()
        {
            m_score = Mathf.Max(0, m_maxScore - m_moveCount);
            m_scoreDisplay?.UpdateScore(m_score);
            m_rewardBar?.UpdateScore(m_score);
        }

        private void UpdateMoveCountDisplay()
        {
            if (m_moveCountText != null)
            {
                m_moveCountText.text = $"Moves: {m_moveCount}";
            }
        }

        private List<int> GetValidNeighbors(int index)
        {
            int row = index / m_gridSize;
            int col = index % m_gridSize;
            var neighbors = new List<int>(4);

            if (row > 0)
            {
                neighbors.Add(index - m_gridSize);
            }

            if (row < m_gridSize - 1)
            {
                neighbors.Add(index + m_gridSize);
            }

            if (col > 0)
            {
                neighbors.Add(index - 1);
            }

            if (col < m_gridSize - 1)
            {
                neighbors.Add(index + 1);
            }

            return neighbors;
        }

        private Vector2 GetCellPosition(int index)
        {
            if (m_gridContainer == null)
            {
                return Vector2.zero;
            }

            int row = index / m_gridSize;
            int col = index % m_gridSize;

            float containerWidth = m_gridContainer.rect.width;
            float containerHeight = m_gridContainer.rect.height;
            float tileWidth = containerWidth / m_gridSize;
            float tileHeight = containerHeight / m_gridSize;

            float x = (col * tileWidth) + (tileWidth / 2f) - (containerWidth / 2f);
            float y = (containerHeight / 2f) - (row * tileHeight) - (tileHeight / 2f);

            return new Vector2(x, y);
        }

        private Vector2 CalculateTileSize()
        {
            if (m_gridContainer == null)
            {
                return Vector2.zero;
            }

            float tileWidth = m_gridContainer.rect.width / m_gridSize;
            float tileHeight = m_gridContainer.rect.height / m_gridSize;

            return new Vector2(tileWidth, tileHeight);
        }

        private Rect CalculateUvRect(int row, int col)
        {
            float uvWidth = 1f / m_gridSize;
            float uvHeight = 1f / m_gridSize;
            float uvX = col * uvWidth;
            float uvY = 1f - ((row + 1) * uvHeight);

            return new Rect(uvX, uvY, uvWidth, uvHeight);
        }

        private void SetAllTilesInteractable(bool interactable)
        {
            foreach (SlidingPuzzleTile tile in m_tiles)
            {
                if (tile != null)
                {
                    tile.SetInteractable(interactable);
                }
            }
        }

        private void CleanupTiles()
        {
            foreach (SlidingPuzzleTile tile in m_tiles)
            {
                if (tile != null)
                {
                    tile.Pressed -= OnTilePressed;
                    Destroy(tile.gameObject);
                }
            }

            m_tiles.Clear();
        }

        private void OnCloseClicked()
        {
            DebugBase.Log(
                $"[{nameof(SlidingPuzzleUI)}] Close clicked after game over",
                DebugCategory.UI);
            GameClosed?.Invoke();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (m_gridSize < 2)
            {
                m_gridSize = 2;
            }
        }
#endif
    }
}
