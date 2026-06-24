using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BowlingGame
{
    /// <summary>
    /// 디스플레이 설정 (해상도 / 창 모드 / UI 스케일) 적용 단일 진입점.
    /// </summary>
    /// <remarks>
    /// <see cref="AudioManager"/> 가 MonoBehaviour 인 것과 달리 본 클래스는 정적 — <see cref="Screen"/> API 자체가 static 이고
    /// CanvasScaler 조회는 호출 시점의 모든 활성 캔버스를 대상으로 하면 충분하기 때문.
    /// 씬 전이 후 새 캔버스에 UI 스케일을 재적용하는 책임은 <see cref="SettingsApplier"/> 의 sceneLoaded 후크가 갖는다.
    /// 로그 prefix <c>[Display]</c>.
    /// </remarks>
    public static class DisplaySetter
    {
        private const string LogPrefix = "[Display]";

        // 모든 씬의 CanvasScaler 가 이 기준 해상도로 통일되어 있다 (AI_PROMPT_REFERENCE.md §11-5).
        // UI 스케일 변경은 referenceResolution 을 1/scale 로 나눠 적용한다 — 작은 ref = 각 픽셀이 화면 더 큰 비율 = UI 가 크게 보임.
        private static readonly Vector2 BaseReferenceResolution = new Vector2(1920f, 1080f);

        // Screen.resolutions 는 호출마다 새 배열을 반환하므로 1회 캐싱 (드롭다운 채울 때 매번 호출하지 않음).
        private static List<Resolution> _cachedSupported;

        /// <summary>
        /// 시스템이 지원하는 해상도 목록을 반환한다 (width×height 중복 제거, 가장 높은 refresh rate 보존, 오름차순).
        /// </summary>
        public static IReadOnlyList<Resolution> GetSupportedResolutions()
        {
            if (_cachedSupported != null) return _cachedSupported;

            var byKey = new Dictionary<(int w, int h), Resolution>();
            foreach (var r in Screen.resolutions)
            {
                var key = (r.width, r.height);
                if (!byKey.TryGetValue(key, out var existing) || RefreshHz(r) > RefreshHz(existing))
                    byKey[key] = r;
            }

            _cachedSupported = new List<Resolution>(byKey.Values);
            _cachedSupported.Sort((a, b) =>
            {
                int c = a.width.CompareTo(b.width);
                return c != 0 ? c : a.height.CompareTo(b.height);
            });
            return _cachedSupported;
        }

        /// <summary>해상도와 창 모드를 동시에 적용한다. 무효 입력은 경고 후 무시.</summary>
        public static void ApplyResolution(int width, int height, FullScreenMode mode)
        {
            if (width <= 0 || height <= 0)
            {
                Debug.LogWarning($"{LogPrefix} 잘못된 해상도 {width}x{height} — 무시");
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            // 브라우저가 viewport 를 관리하며 ExclusiveFullScreen 은 보안 정책상 직접 호출 불가.
            Debug.Log($"{LogPrefix} WebGL — 해상도/창모드 적용 스킵 ({width}x{height} {mode})");
#else
            Screen.SetResolution(width, height, mode);
            Debug.Log($"{LogPrefix} 해상도 적용: {width}x{height} ({mode})");
#endif
        }

        /// <summary>
        /// UI 스케일을 적용한다. 활성 씬의 모든 <see cref="CanvasScaler"/> (mode=ScaleWithScreenSize) 의 referenceResolution 을 조정한다.
        /// 0.5 ~ 2.0 범위로 클램프되며 그 외 모드의 CanvasScaler 는 건드리지 않는다.
        /// </summary>
        public static void ApplyUIScale(float scale)
        {
            scale = Mathf.Clamp(scale, 0.5f, 2.0f);
            var scalers = Object.FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            int touched = 0;
            foreach (var cs in scalers)
            {
                if (cs.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize) continue;
                cs.referenceResolution = BaseReferenceResolution / scale;
                touched++;
            }
            Debug.Log($"{LogPrefix} UI 스케일 적용: {scale:F2} (캔버스 {touched}/{scalers.Length}개 갱신)");
        }

        /// <summary>드롭다운 인덱스 (0=전체화면, 1=테두리 없는 창, 2=창 모드) → <see cref="FullScreenMode"/>.</summary>
        public static FullScreenMode IndexToMode(int idx) => idx switch
        {
            0 => FullScreenMode.ExclusiveFullScreen,
            1 => FullScreenMode.FullScreenWindow,
            2 => FullScreenMode.Windowed,
            _ => FullScreenMode.FullScreenWindow
        };

        /// <summary><see cref="FullScreenMode"/> → 드롭다운 인덱스. mac 전용 MaximizedWindow 는 1(FullScreenWindow) 로 폴백.</summary>
        public static int ModeToIndex(FullScreenMode mode) => mode switch
        {
            FullScreenMode.ExclusiveFullScreen => 0,
            FullScreenMode.FullScreenWindow => 1,
            FullScreenMode.Windowed => 2,
            _ => 1
        };

        // refreshRate (deprecated in 2022.2+) vs refreshRateRatio — 양쪽 호환.
        private static double RefreshHz(Resolution r)
        {
#if UNITY_2022_2_OR_NEWER
            return r.refreshRateRatio.value;
#else
            return r.refreshRate;
#endif
        }
    }
}
