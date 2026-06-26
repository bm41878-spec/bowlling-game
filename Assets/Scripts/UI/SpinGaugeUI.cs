using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BowlingGame
{
    /// <summary>
    /// 회전(커브) 입력용 가로 진동 게이지. <see cref="PowerGaugeUI"/> 와 동일한 패턴이며 축만 가로.
    /// 마커가 트랙 좌↔우를 왕복하고, 단일 확정 입력(Space / 게임패드 A)으로 회전 방향+세기를 동시에 확정한다.
    /// </summary>
    /// <remarks>
    /// 값 매핑: 마커의 중앙 기준 부호 = 커브 방향(좌 −, 우 +), 거리 = 커브 양.
    /// <see cref="ConfirmedSpin"/> ∈ [-1, +1] (중앙 0 = 직구). <see cref="BowlingBall"/> 가 발사 시 읽어 커브에 사용.
    /// 상태 흐름: AimingPower(세기 확정) → <b>AimingSpin(본 게이지)</b> → Rolling(발사).
    /// </remarks>
    public class SpinGaugeUI : MonoBehaviour
    {
        [Tooltip("좌우로 움직이는 마커 RectTransform. anchoredPosition.x 만 갱신된다.")]
        [SerializeField] RectTransform marker;
        [Tooltip("마커가 도달하는 트랙 절반 폭(px). +halfWidth = 완전 우(+1), -halfWidth = 완전 좌(-1).")]
        [SerializeField] float trackHalfWidth = 300f;
        [Tooltip("선택: 현재 회전값 표시 라벨 (예: 'L 60%' / '직구' / 'R 40%').")]
        [SerializeField] TMP_Text spinValueText;
        [Tooltip("왕복 속도. PowerGaugeUI.gaugeSpeed 와 유사.")]
        [SerializeField] float gaugeSpeed = 0.75f;

        [Tooltip("중앙 직구 구간(±값). 게이지 전체 범위(-1~+1, 폭 2)의 중간 10% = ±0.1. 이 구간 안에서 확정하면 회전 0(직구)으로 발사된다.")]
        [SerializeField, Range(0f, 0.45f)] float straightZone = 0.1f;

        [Tooltip("커브 강조 지수. 1=선형(균일), 클수록 끝으로 갈수록 커브가 급격히 강해진다(중앙 부근은 더 완만). 2 권장.")]
        [SerializeField, Range(1f, 4f)] float curveExponent = 2f;

        // 색상 — 좌/우/중앙 구분 (PowerGaugeUI 의 색 상수 패턴 답습).
        private static readonly Color ColorLeft   = new Color(0f,        0.69f,     1f);        // #00B0FF
        private static readonly Color ColorCenter = new Color(1f,        1f,        1f);        // 흰색(직구)
        private static readonly Color ColorRight  = new Color(1f,        0.77f,     0f);        // #FFC400

        private float pingPongTime = 0f;
        private float currentSigned = 0f;   // [-1, +1]
        private int enteredFrame = -1;

        /// <summary>확정된 회전값 [-1(좌) ~ +1(우)]. 중앙 0 = 직구.</summary>
        public float ConfirmedSpin { get; private set; }

        private GameStateManager stateManager;
        private InputController inputController;
        private Image markerImage;

        void Start()
        {
            stateManager    = GameStateManager.Instance;
            inputController = InputController.Instance;
            if (marker != null) markerImage = marker.GetComponent<Image>();

            stateManager.OnStateChanged      += HandleStateChanged;
            inputController.OnConfirmPressed += OnConfirmInput;

            // 구독 후 즉시 숨김 — 첫 프레임 렌더 전이라 깜빡임 없음.
            gameObject.SetActive(false);
        }

        void Update()
        {
            if (stateManager.CurrentState != GameState.AimingSpin) return;

            pingPongTime += Time.deltaTime;
            // 0..2 왕복 → -1..+1 로 변환 (중앙 0).
            currentSigned = Mathf.PingPong(pingPongTime * gaugeSpeed, 2f) - 1f;

            if (marker != null)
                marker.anchoredPosition = new Vector2(currentSigned * trackHalfWidth, marker.anchoredPosition.y);

            if (markerImage != null)
                markerImage.color = GetSpinColor(currentSigned);

            if (spinValueText != null)
                spinValueText.text = FormatSpin(currentSigned);
        }

        private void HandleStateChanged(GameState prev, GameState next)
        {
            if (next == GameState.AimingSpin)
            {
                gameObject.SetActive(true);
                pingPongTime = 0f;
                // 같은 프레임 confirm 캐스케이드 방지 (PowerGaugeUI 가 같은 입력으로 AimingPower→AimingSpin 전이를 트리거).
                enteredFrame = Time.frameCount;
            }
            if (prev == GameState.AimingSpin)
                gameObject.SetActive(false);
        }

        private void OnConfirmInput()
        {
            if (stateManager.CurrentState != GameState.AimingSpin) return;
            if (Time.frameCount == enteredFrame) return;

            ConfirmedSpin = ShapeSpin(currentSigned);
            string side = ConfirmedSpin < 0f ? "좌" : ConfirmedSpin > 0f ? "우" : "직구";
            Debug.Log($"[SpinGauge] 회전 확정: {side} (게이지 {currentSigned:F2} → 적용 {ConfirmedSpin:F2})");
            stateManager.ChangeState(GameState.Rolling);
        }

        // 게이지 위치(raw, -1~+1)를 실제 적용 회전값으로 변환:
        //  1) 중앙 직구 구간(±straightZone) 안이면 0 (직구)
        //  2) 바깥은 [straightZone,1] → [0,1] 로 재매핑 후 curveExponent 거듭제곱 → 끝으로 갈수록 커브 강조
        private float ShapeSpin(float raw)
        {
            float a = Mathf.Abs(raw);
            if (a <= straightZone) return 0f;
            float denom = Mathf.Max(1f - straightZone, 0.0001f);
            float m = (a - straightZone) / denom;            // [straightZone,1] → [0,1]
            m = Mathf.Pow(Mathf.Clamp01(m), curveExponent);  // 끝으로 갈수록 급격히 증가
            return Mathf.Sign(raw) * m;
        }

        private Color GetSpinColor(float signed)
        {
            float a = Mathf.Abs(signed);
            if (a <= straightZone) return ColorCenter;       // 직구 구간 = 흰색
            return signed < 0f
                ? Color.Lerp(ColorCenter, ColorLeft,  a)
                : Color.Lerp(ColorCenter, ColorRight, a);
        }

        private string FormatSpin(float signed)
        {
            if (Mathf.Abs(signed) <= straightZone) return "직구";  // 직구 구간
            int pct = Mathf.RoundToInt(Mathf.Abs(signed) * 100f);
            return signed < 0f ? $"L {pct}%" : $"R {pct}%";
        }

        void OnDestroy()
        {
            if (stateManager   != null) stateManager.OnStateChanged      -= HandleStateChanged;
            if (inputController != null) inputController.OnConfirmPressed -= OnConfirmInput;
        }
    }
}
