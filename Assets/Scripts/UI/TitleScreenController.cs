using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace BowlingGame
{
    /// <summary>
    /// 게임 첫 진입 시 보이는 대기 화면 — Cinemachine 가상 카메라 N개를 페이드 인/아웃과 동기 순환하고,
    /// 사용자가 키보드/마우스/게임패드의 아무 버튼이나 누르면 mainmenu 로 전환한다 (2026-06-23: Cinemachine 도입).
    /// </summary>
    /// <remarks>
    /// 책임 분리:
    /// <list type="bullet">
    ///   <item>각 vcam 의 시작 Transform (위치/회전) / LookAt target 은 씬에 정적으로 배치 — 인스펙터 시각화</item>
    ///   <item>샷 내부 카메라 이동(천천히 패닝): vcam.transform.position 을 코드로 <see cref="Mathf.SmoothStep"/> 보간</item>
    ///   <item>샷 전환: 활성 vcam 의 Priority 만 10, 나머지 1 — 페이드 hold 동안 컷 전환되어 사용자 비가시</item>
    ///   <item>회전: 각 vcam 의 LookAt 슬롯으로 Cinemachine 이 자동 보간 (카메라가 움직여도 target 응시)</item>
    /// </list>
    /// 위치만 보간하고 회전은 LookAt 자동 — 영화적 ease + 자연스러운 회전 동시 확보.
    /// </remarks>
    public class TitleScreenController : MonoBehaviour
    {
        private const string LogPrefix = "[Title]";
        private const int ActivePriority   = 10;
        private const int InactivePriority = 1;

        [Serializable]
        public struct CinematicShot
        {
            public string label;                   // 인스펙터 식별 (예: "Pin Closeup")
            public CinemachineCamera virtualCamera;// 시작 위치 = vcam.transform.position, LookAt = vcam.LookAt
            public Vector3 endPosition;            // 보간 종료 위치 (vcam Transform 보간)
            public float duration;
            public float fadeInTime;
            public float fadeOutTime;
            public float holdAtBlack;
        }

        [Header("Sequence")]
        [SerializeField] private CinematicShot[] shots;

        [Header("UI")]
        [Tooltip("검은 알파 1 시작. 페이드인 시 0 으로 보간.")]
        [SerializeField] private Image fadeImage;
        [SerializeField] private TMP_Text anyKeyHint;
        [SerializeField] private TMP_Text versionLabel;
        [SerializeField] private float anyKeyBlinkInterval = 0.8f;
        [SerializeField] private float transitionFadeOutTime = 0.4f;

        [Header("Scene")]
        [SerializeField] private string mainMenuSceneName = "mainmenu";

        private bool transitioning;
        // 각 vcam 의 인스펙터 시작 위치 캐시 — 매 순환 시 시작 좌표 복원용.
        private Vector3[] cachedStartPositions;

        void Start()
        {
            if (versionLabel != null) versionLabel.text = "v" + Application.version;

            SetFadeAlpha(1f);

            if (shots != null && shots.Length > 0)
            {
                cachedStartPositions = new Vector3[shots.Length];
                for (int i = 0; i < shots.Length; i++)
                {
                    if (shots[i].virtualCamera != null)
                        cachedStartPositions[i] = shots[i].virtualCamera.transform.position;
                    SetVcamPriority(shots[i].virtualCamera, InactivePriority);
                }
                StartCoroutine(RunSequence());
            }
            else
            {
                Debug.LogWarning($"{LogPrefix} 시네마틱 샷 미설정 — 카메라 정적");
            }

            if (anyKeyHint != null) StartCoroutine(BlinkHint());

            Debug.Log($"{LogPrefix} 초기화 완료 (version={Application.version}, shots={shots?.Length ?? 0})");
        }

        void Update()
        {
            if (transitioning) return;
            if (AnyInputPressed())
            {
                transitioning = true;
                Debug.Log($"{LogPrefix} 입력 감지 — mainmenu 로 전환");
                StopAllCoroutines();
                StartCoroutine(FadeOutAndLoad());
            }
        }

        // ---------- 입력 감지 ----------

        private bool AnyInputPressed()
        {
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;

            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame)   return true;
                if (Mouse.current.rightButton.wasPressedThisFrame)  return true;
                if (Mouse.current.middleButton.wasPressedThisFrame) return true;
            }

            if (Gamepad.current != null)
            {
                var pad = Gamepad.current;
                if (pad.buttonSouth.wasPressedThisFrame)   return true;
                if (pad.buttonNorth.wasPressedThisFrame)   return true;
                if (pad.buttonEast.wasPressedThisFrame)    return true;
                if (pad.buttonWest.wasPressedThisFrame)    return true;
                if (pad.startButton.wasPressedThisFrame)   return true;
                if (pad.selectButton.wasPressedThisFrame)  return true;
                if (pad.leftShoulder.wasPressedThisFrame)  return true;
                if (pad.rightShoulder.wasPressedThisFrame) return true;
            }

            return false;
        }

        // ---------- 시네마틱 시퀀스 ----------

        private IEnumerator RunSequence()
        {
            int i = 0;
            while (true)
            {
                yield return PlayShot(i);
                i = (i + 1) % shots.Length;
            }
        }

        private IEnumerator PlayShot(int shotIndex)
        {
            CinematicShot shot = shots[shotIndex];
            if (shot.virtualCamera == null) yield break;

            // 활성 vcam 만 Priority 높이고 나머지는 낮춤 — 페이드 hold 동안 컷 전환
            for (int i = 0; i < shots.Length; i++)
                SetVcamPriority(shots[i].virtualCamera, i == shotIndex ? ActivePriority : InactivePriority);

            // 시작 위치 (인스펙터 캐싱값) 으로 복원
            Vector3 startPos = cachedStartPositions[shotIndex];
            shot.virtualCamera.transform.position = startPos;

            // 페이드 인 (검은 → 보임)
            yield return FadeAlpha(1f, 0f, Mathf.Max(shot.fadeInTime, 0.01f));

            // 위치 보간 — SmoothStep ease-in-out. 회전은 LookAt 으로 Cinemachine 자동 처리.
            float t = 0f;
            float dur = Mathf.Max(shot.duration, 0.1f);
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / dur);
                shot.virtualCamera.transform.position = Vector3.Lerp(startPos, shot.endPosition, k);
                yield return null;
            }

            // 페이드 아웃 (보임 → 검은)
            yield return FadeAlpha(0f, 1f, Mathf.Max(shot.fadeOutTime, 0.01f));

            // 시작 위치로 복원 — 다음 순환 시 같은 자리에서 다시 시작
            shot.virtualCamera.transform.position = startPos;

            if (shot.holdAtBlack > 0f) yield return new WaitForSeconds(shot.holdAtBlack);
        }

        private void SetVcamPriority(CinemachineCamera vcam, int value)
        {
            if (vcam == null) return;
            var p = vcam.Priority;
            p.Value = value;
            vcam.Priority = p;
        }

        // ---------- 페이드 ----------

        private IEnumerator FadeAlpha(float from, float to, float duration)
        {
            float t = 0f;
            SetFadeAlpha(from);
            while (t < duration)
            {
                t += Time.deltaTime;
                SetFadeAlpha(Mathf.Lerp(from, to, t / duration));
                yield return null;
            }
            SetFadeAlpha(to);
        }

        private void SetFadeAlpha(float a)
        {
            if (fadeImage == null) return;
            Color c = fadeImage.color;
            c.a = a;
            fadeImage.color = c;
        }

        private IEnumerator FadeOutAndLoad()
        {
            float t = 0f;
            float dur = Mathf.Max(transitionFadeOutTime, 0.01f);
            float startAlpha = fadeImage != null ? fadeImage.color.a : 1f;
            while (t < dur)
            {
                t += Time.deltaTime;
                SetFadeAlpha(Mathf.Lerp(startAlpha, 1f, t / dur));
                yield return null;
            }
            SetFadeAlpha(1f);
            SceneManager.LoadScene(mainMenuSceneName);
        }

        // ---------- 안내 텍스트 깜빡임 ----------

        private IEnumerator BlinkHint()
        {
            while (true)
            {
                yield return BlinkAlphaLerp(0.4f, 1f, anyKeyBlinkInterval);
                yield return BlinkAlphaLerp(1f, 0.4f, anyKeyBlinkInterval);
            }
        }

        private IEnumerator BlinkAlphaLerp(float from, float to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                if (anyKeyHint != null)
                {
                    Color c = anyKeyHint.color;
                    c.a = Mathf.Lerp(from, to, t / duration);
                    anyKeyHint.color = c;
                }
                yield return null;
            }
        }
    }
}
