using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace FastGame
{
    /// <summary>
    /// Optional local splash video — assign on <b>SPLASH_VIDEO</b>; playback end is reported to
    /// <see cref="FastGameSplashBehaviour"/>. Requires Unity Video module when used.
    /// </summary>
    [AddComponentMenu("Fast Game/Splash/Video View")]
    public sealed class FastGameSplashVideoView : MonoBehaviour
    {
        [Tooltip("VideoPlayer component on this object or a child.")]
        public Component VideoPlayer;

        Component _player;
        Coroutine _waitCoroutine;
        Action _onFinished;

        void Reset()
        {
            VideoPlayer = ResolvePlayer();
        }

        public bool HasLocalClip()
        {
            var vp = ResolvePlayer();
            if (vp == null)
                return false;
            var clip = vp.GetType().GetProperty("clip")?.GetValue(vp);
            return clip != null;
        }

        public void Hide()
        {
            StopWait();
            gameObject.SetActive(false);
        }

        public bool TryShowLocal(Action onFinished)
        {
            if (!HasLocalClip())
                return false;

            _onFinished = onFinished;
            gameObject.SetActive(true);
            Play();
            _waitCoroutine = StartCoroutine(WaitForPlaybackFinished());
            return true;
        }

        void StopWait()
        {
            if (_waitCoroutine != null)
            {
                StopCoroutine(_waitCoroutine);
                _waitCoroutine = null;
            }
            _onFinished = null;
        }

        IEnumerator WaitForPlaybackFinished()
        {
            var vp = ResolvePlayer();
            if (vp == null)
            {
                Finish();
                yield break;
            }

            var type = vp.GetType();
            var isPlayingProp = type.GetProperty("isPlaying");
            var isLoopingProp = type.GetProperty("isLooping");

            if (isPlayingProp == null)
            {
                Finish();
                yield break;
            }

            while (true)
            {
                var isPlaying = isPlayingProp.GetValue(vp) is bool playing && playing;
                var isLooping = isLoopingProp?.GetValue(vp) is bool looping && looping;
                if (!isPlaying && !isLooping)
                    break;
                yield return null;
            }

            Finish();
        }

        void Finish()
        {
            _waitCoroutine = null;
            var callback = _onFinished;
            _onFinished = null;
            callback?.Invoke();
        }

        void Play()
        {
            ResolvePlayer()?.GetType().GetMethod("Play")?.Invoke(ResolvePlayer(), null);
        }

        Component ResolvePlayer()
        {
            if (_player != null)
                return _player;

            if (VideoPlayer != null)
            {
                if (VideoPlayer.GetType().Name == "VideoPlayer")
                {
                    _player = VideoPlayer;
                    return _player;
                }

                var onSame = VideoPlayer.GetComponent("VideoPlayer") as Component;
                if (onSame != null)
                {
                    _player = onSame;
                    return _player;
                }
            }

            foreach (var c in GetComponents<Component>())
            {
                if (c != null && c.GetType().Name == "VideoPlayer")
                {
                    _player = c;
                    VideoPlayer = c;
                    return _player;
                }
            }

            foreach (var c in GetComponentsInChildren<Component>(true))
            {
                if (c != null && c.GetType().Name == "VideoPlayer")
                {
                    _player = c;
                    VideoPlayer = c;
                    return _player;
                }
            }

            return null;
        }
    }
}
