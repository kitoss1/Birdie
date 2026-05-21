using System;
using System.Collections.Generic;
using System.Threading;
using Birdie.Data;
using Birdie.Debug;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Birdie.Managers
{
    /// <summary>
    /// Manages sequential or shuffled playback of a MusicPlaylist through a provided AudioSource.
    /// Advances to the next track automatically when each clip finishes.
    /// </summary>
    public class MusicPlaylistPlayer
    {
        private readonly AudioSource m_source;
        private MusicPlaylist m_playlist;
        private CancellationTokenSource m_cts;
        private List<int> m_trackOrder;
        private int m_cursor;
        private Func<float> m_getVolume;

        public MusicPlaylistPlayer(AudioSource source)
        {
            m_source = source;
        }

        public void Play(MusicPlaylist playlist, Func<float> getVolume)
        {
            Stop();

            m_playlist = playlist;
            m_getVolume = getVolume;
            m_cursor = 0;
            m_trackOrder = BuildTrackOrder(playlist);
            m_cts = new CancellationTokenSource();

            PlayLoopAsync(m_cts.Token).Forget();
        }

        public void Stop()
        {
            if (m_cts != null)
            {
                m_cts.Cancel();
                m_cts.Dispose();
                m_cts = null;
            }

            m_playlist = null;
        }

        private async UniTaskVoid PlayLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                int trackIndex = m_trackOrder[m_cursor % m_trackOrder.Count];
                AudioClip track = m_playlist.GetTrack(trackIndex);

                if (track == null)
                {
                    break;
                }

                m_source.clip = track;
                m_source.loop = false;
                m_source.volume = m_getVolume();
                m_source.Play();

                DebugBase.Log($"[{nameof(MusicPlaylistPlayer)}] Playing: {track.name}", DebugCategory.Audio);

                bool cancelled = await UniTask
                    .WaitUntil(() => !m_source.isPlaying, cancellationToken: ct)
                    .SuppressCancellationThrow();

                if (cancelled)
                {
                    break;
                }

                m_cursor = (m_cursor + 1) % m_trackOrder.Count;
            }
        }

        private static List<int> BuildTrackOrder(MusicPlaylist playlist)
        {
            var indices = new List<int>(playlist.TrackCount);
            for (int i = 0; i < playlist.TrackCount; i++)
            {
                indices.Add(i);
            }

            if (!playlist.Shuffle)
            {
                return indices;
            }

            for (int i = indices.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (indices[i], indices[j]) = (indices[j], indices[i]);
            }

            return indices;
        }
    }
}
