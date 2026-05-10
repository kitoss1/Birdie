using Birdie.Debug;
using UnityEngine;
using UnityEngine.UI;

namespace Birdie.UI
{
    public sealed class MapPopupUI : MonoBehaviour
    {
        [SerializeField] private Image m_mapImage;
        [SerializeField] private Button m_backdropButton;

        private void Awake()
        {
            if (m_backdropButton != null)
                m_backdropButton.onClick.AddListener(Hide);
        }

        private void OnDestroy()
        {
            if (m_backdropButton != null)
                m_backdropButton.onClick.RemoveListener(Hide);
        }

        public void Show(Sprite mapSprite)
        {
            if (m_mapImage != null && mapSprite != null)
                m_mapImage.sprite = mapSprite;

            gameObject.SetActive(true);
            DebugBase.Log($"[{nameof(MapPopupUI)}] Showing map popup");
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (m_mapImage == null)
                UnityEngine.Debug.LogWarning($"[{nameof(MapPopupUI)}] Map Image reference is missing!", this);

            if (m_backdropButton == null)
                UnityEngine.Debug.LogWarning($"[{nameof(MapPopupUI)}] Backdrop Button reference is missing!", this);
        }
#endif
    }
}
