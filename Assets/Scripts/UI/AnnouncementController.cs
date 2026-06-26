using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Bowling.Scoring;

namespace BowlingGame
{
    /// <summary>
    /// 스트라이크 / 스페어 / 거터 발생 시 화면 중앙에 연출 이미지를 팝업으로 띄우는 UI 컴포넌트.
    /// 음성(보이스)은 <see cref="AudioManager"/> 가 동일 이벤트를 구독해 재생하므로,
    /// 이 컴포넌트는 <b>시각 연출만</b> 담당한다 (오디오/표시 책임 분리).
    /// </summary>
    /// <remarks>
    /// 배치: <c>Game.unity</c> 의 Canvas 하위. 평소엔 CanvasGroup.alpha=0 으로 숨겨져 있다.
    /// 이벤트 소스:
    /// <list type="bullet">
    ///   <item>스트라이크/스페어 — <see cref="FrameManager.OnFrameCompleted"/> (frame.IsStrike()/IsSpare())</item>
    ///   <item>거터 — <see cref="ThrowTransitionController.OnGutterBall"/> (거터 진입 + 0핀 동시 충족 시에만 판정)</item>
    /// </list>
    /// 와이어링 타이밍: <see cref="GameManager"/> 가 [DefaultExecutionOrder(1000)] 이라 Start 시점엔
    /// FrameManager 가 아직 없을 수 있다 → AudioManager 와 동일하게 코루틴으로 한 프레임 뒤 재시도.
    /// 예시 스프라이트는 placeholder 이며 인스펙터에서 실제 아트로 교체하면 된다.
    /// </remarks>
    [RequireComponent(typeof(CanvasGroup))]
    public class AnnouncementController : MonoBehaviour
    {
        public enum Kind { Strike, Spare, Gutter }

        private const string LogPrefix = "[Announcement]";

        [Header("표시 요소")]
        [Tooltip("연출 배너를 그릴 Image. sprite 가 종류별로 교체된다.")]
        [SerializeField] private Image bannerImage;

        [Tooltip("선택: 종류별 텍스트 라벨 (STRIKE! / SPARE! / GUTTER). null 허용.")]
        [SerializeField] private TMP_Text label;

        [Header("종류별 예시 스프라이트 (인스펙터에서 실제 아트로 교체)")]
        [SerializeField] private Sprite strikeSprite;
        [SerializeField] private Sprite spareSprite;
        [SerializeField] private Sprite gutterSprite;

        [Header("애니메이션")]
        [Tooltip("등장(스케일 업 + 페이드 인) 시간(초).")]
        [SerializeField] private float popInSeconds = 0.25f;
        [Tooltip("가장 큰 상태로 유지되는 시간(초).")]
        [SerializeField] private float holdSeconds = 1.0f;
        [Tooltip("퇴장(페이드 아웃) 시간(초).")]
        [SerializeField] private float fadeOutSeconds = 0.4f;
        [Tooltip("등장 시 시작 스케일. 1 미만이면 작게 시작해 커진다.")]
        [SerializeField] private float popStartScale = 0.5f;
        [Tooltip("가장 커졌을 때의 오버슈트 스케일. 1.0 이면 오버슈트(바운스) 없음.")]
        [SerializeField] private float popOvershootScale = 1.15f;

        private CanvasGroup canvasGroup;
        private RectTransform rect;
        private Coroutine playing;

        // 구독 추적 (씬 종료 시 해제)
        private FrameManager subscribedFrameManager;
        private ThrowTransitionController subscribedTransition;

        void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            rect = (RectTransform)transform;
        }

        void Start()
        {
            // 시작 시 숨김 — 첫 프레임 렌더 전이라 깜빡임 없음.
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            // 스트라이크/스페어(FrameManager) + 거터(TransitionController) 는 모두 GameManager 가 보유.
            // GameManager 가 [DefaultExecutionOrder(1000)] 이라 Start 시점엔 아직 없을 수 있다 → 코루틴 재시도.
            if (!TryWireGameManagerRefs())
                StartCoroutine(WireGameManagerRefsDeferred());
        }

        private bool TryWireGameManagerRefs()
        {
            if (GameManager.Instance == null) return false;
            var fm = GameManager.Instance.FrameManager;
            var tc = GameManager.Instance.TransitionController;
            if (fm == null || tc == null) return false;

            fm.OnFrameCompleted += HandleFrameCompleted;   // 스트라이크/스페어 연출
            subscribedFrameManager = fm;
            tc.OnGutterBall += HandleGutter;               // 거터 판정(거터 진입 + 0핀) 연출
            subscribedTransition = tc;
            return true;
        }

        // GameManager.Start 가 확실히 끝난 뒤 잡도록 2프레임 마진. (AudioManager 와 동일 패턴)
        private IEnumerator WireGameManagerRefsDeferred()
        {
            yield return null;
            yield return null;
            if (subscribedFrameManager != null && subscribedTransition != null) yield break;
            if (!TryWireGameManagerRefs())
                Debug.LogWarning($"{LogPrefix} GameManager 미발견 — 연출 비활성 (mainmenu / Gameover_scene 정상)");
        }

        void OnDestroy()
        {
            if (subscribedFrameManager != null) subscribedFrameManager.OnFrameCompleted -= HandleFrameCompleted;
            if (subscribedTransition != null) subscribedTransition.OnGutterBall -= HandleGutter;
        }

        // ---------- 이벤트 핸들러 ----------
        // 표시 로직 인라인 금지 (§7-12) — 핸들러는 어떤 Kind 를 띄울지만 결정.

        private void HandleFrameCompleted(int frameIndex, Frame frame)
        {
            if (frame == null) return;
            if (frame.IsStrike()) Show(Kind.Strike);
            else if (frame.IsSpare()) Show(Kind.Spare);
        }

        private void HandleGutter() => Show(Kind.Gutter);

        // ---------- 공개 표시 API ----------

        /// <summary>지정한 종류의 연출을 즉시 띄운다. 재생 중이면 새 연출로 덮어쓴다.</summary>
        public void Show(Kind kind)
        {
            if (bannerImage == null)
            {
                Debug.LogWarning($"{LogPrefix} bannerImage 미배선 — 연출 무시 ({kind})");
                return;
            }

            ApplyKind(kind);

            if (playing != null) StopCoroutine(playing);
            playing = StartCoroutine(PlayRoutine());

            Debug.Log($"{LogPrefix} 연출 표시: {kind}");
        }

        private void ApplyKind(Kind kind)
        {
            Sprite sprite = kind switch
            {
                Kind.Strike => strikeSprite,
                Kind.Spare  => spareSprite,
                Kind.Gutter => gutterSprite,
                _ => null
            };
            if (sprite != null) bannerImage.sprite = sprite;

            if (label != null)
            {
                label.text = kind switch
                {
                    Kind.Strike => "STRIKE!",
                    Kind.Spare  => "SPARE!",
                    Kind.Gutter => "GUTTER",
                    _ => string.Empty
                };
            }
        }

        // 팝인(스케일 + 페이드 인) → 유지 → 페이드 아웃. WaitForSeconds 는 timeScale 영향을 받으므로
        // 일시정지(Time.timeScale=0) 시 연출도 함께 멈춘다 (의도된 동작).
        private IEnumerator PlayRoutine()
        {
            // 1) 팝인: 스케일 popStart→overshoot→1, alpha 0→1
            float t = 0f;
            while (t < popInSeconds)
            {
                t += Time.deltaTime;
                float n = Mathf.Clamp01(t / popInSeconds);
                canvasGroup.alpha = n;
                float s = n < 0.6f
                    ? Mathf.Lerp(popStartScale, popOvershootScale, n / 0.6f)
                    : Mathf.Lerp(popOvershootScale, 1f, (n - 0.6f) / 0.4f);
                rect.localScale = Vector3.one * s;
                yield return null;
            }
            canvasGroup.alpha = 1f;
            rect.localScale = Vector3.one;

            // 2) 유지
            yield return new WaitForSeconds(holdSeconds);

            // 3) 페이드 아웃
            t = 0f;
            while (t < fadeOutSeconds)
            {
                t += Time.deltaTime;
                canvasGroup.alpha = 1f - Mathf.Clamp01(t / fadeOutSeconds);
                yield return null;
            }
            canvasGroup.alpha = 0f;
            playing = null;
        }
    }
}
