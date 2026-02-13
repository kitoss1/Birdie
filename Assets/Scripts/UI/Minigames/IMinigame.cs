using System;
using Birdie.Data;

namespace Birdie.UI.Minigames
{
    public interface IMinigame
    {
        event Action GameClosed;

        int FriendshipReward { get; }

        void SetRewardTiers(MinigameRewardTier[] rewardTiers);

        void SetDifficulty(MinigameDifficultySettings settings);

        void StartGame();
    }
}
