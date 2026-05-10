using System.Collections.Generic;
using System.Threading;
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
        private readonly Dictionary<string, CancellationTokenSource> m_groupTokens = new Dictionary<string, CancellationTokenSource>();

        public void ShowToast(string text, Transform anchor = null, ToastSettings settings = null, string group = null)
        {
            if (!EnsureInitialized())
            {
                return;
            }

            // Cancel the existing toast in this group so it quick-dismisses.
            bool replacingGroupToast = false;
            if (group != null && m_groupTokens.TryGetValue(group, out CancellationTokenSource existingCts))
            {
                existingCts.Cancel();
                m_groupTokens.Remove(group);
                replacingGroupToast = true;
            }

            if (!replacingGroupToast && m_activeToastCount >= m_maxActiveToasts)
            {
                DebugBase.Log($"[{nameof(ToastManager)}] Max active toasts reached, skipping: \"{text}\"");
                return;
            }

            CancellationTokenSource cts = new CancellationTokenSource();
            if (group != null)
            {
                m_groupTokens[group] = cts;
            }

            ToastUI toast = Instantiate(m_toastPrefab, m_canvas.transform);
            PositionToast(toast.GetComponent<RectTransform>(), anchor, settings);
            TrackToastAsync(toast, text, settings, cts, group).Forget();

            DebugBase.Log($"[{nameof(ToastManager)}] Showing toast: \"{text}\"");
        }

        private async UniTaskVoid TrackToastAsync(ToastUI toast, string text, ToastSettings settings, CancellationTokenSource cts, string group)
        {
            m_activeToastCount++;
            try
            {
                await toast.ShowAsync(text, settings, cts.Token);
            }
            finally
            {
                m_activeToastCount--;

                // Only remove from the group dictionary if we're still the active entry (a newer toast may have replaced us).
                if (group != null && m_groupTokens.TryGetValue(group, out CancellationTokenSource currentCts) && ReferenceEquals(currentCts, cts))
                {
                    m_groupTokens.Remove(group);
                }

                cts.Dispose();
            }
        }

        private void PositionToast(RectTransform toastRect, Transform anchor, ToastSettings settings)
        {
            Vector2 basePosition = anchor == null ? Vector2.zero : WorldToCanvasPosition(anchor.position);
            Vector2 offset = settings != null ? settings.PositionOffset : Vector2.zero;
            toastRect.anchoredPosition = basePosition + offset;
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

        private void OnDestroy()
        {
            foreach (CancellationTokenSource cts in m_groupTokens.Values)
            {
                cts.Cancel();
                cts.Dispose();
            }

            m_groupTokens.Clear();
        }
    }
}
