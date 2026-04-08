using Birdie.Debug;
using Birdie.UI.Toast;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Birdie.Managers
{
    public sealed class ToastManager : BaseManager, IToastService
    {
        [SerializeField] private Canvas m_canvas;
        [SerializeField] private Camera m_camera;
        [SerializeField] private ToastUI m_toastPrefab;
        [SerializeField] private int m_maxActiveToasts = 3;

        private int m_activeToastCount = 0;

        public void ShowToast(string text, Transform anchor = null)
        {
            if (!EnsureInitialized())
            {
                return;
            }

            if (m_activeToastCount >= m_maxActiveToasts)
            {
                DebugBase.Log($"[{nameof(ToastManager)}] Max active toasts reached, skipping: \"{text}\"");
                return;
            }

            ToastUI toast = Instantiate(m_toastPrefab, m_canvas.transform);
            PositionToast(toast.GetComponent<RectTransform>(), anchor);
            TrackToastAsync(toast, text).Forget();

            DebugBase.Log($"[{nameof(ToastManager)}] Showing toast: \"{text}\"");
        }

        private async UniTaskVoid TrackToastAsync(ToastUI toast, string text)
        {
            m_activeToastCount++;
            await toast.ShowAsync(text);
            m_activeToastCount--;
        }

        private void PositionToast(RectTransform toastRect, Transform anchor)
        {
            toastRect.anchoredPosition = anchor == null
                ? Vector2.zero
                : WorldToCanvasPosition(anchor.position);
        }

        private Vector2 WorldToCanvasPosition(Vector3 worldPosition)
        {
            Camera renderCamera = m_canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : m_camera;
            Vector3 screenPosition = m_camera.WorldToScreenPoint(worldPosition);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                m_canvas.GetComponent<RectTransform>(),
                screenPosition,
                renderCamera,
                out Vector2 localPoint
            );

            return localPoint;
        }
    }
}
