using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BowlingGame
{
    public class PowerGaugeUI : MonoBehaviour
    {
        [SerializeField] RectTransform arrowShaft;
        [SerializeField] TMP_Text powerValueText;
        [SerializeField] float minHeight = 20f;
        [SerializeField] float maxHeight = 160f;
        [SerializeField] float gaugeSpeed = 1.5f;

        private float pingPongTime = 0f;
        private float currentNormalized = 0f;
        private int enteredFrame = -1;
        public float ConfirmedNormalized { get; private set; }

        private GameStateManager stateManager;
        private InputController inputController;
        private Image shaftImage;

        // 색상 상수
        private static readonly Color ColorLow    = new Color(0f,        200f/255f,  83f/255f); // #00C853
        private static readonly Color ColorMid    = new Color(1f,        215f/255f,  0f);        // #FFD700
        private static readonly Color ColorHigh   = new Color(1f,         23f/255f, 68f/255f);   // #FF1744

        void Start()
        {
            stateManager    = GameStateManager.Instance;
            inputController = InputController.Instance;
            shaftImage      = arrowShaft.GetComponent<Image>();

            // 이벤트 구독은 inactive 상태에서도 유지됨
            stateManager.OnStateChanged      += HandleStateChanged;
            inputController.OnConfirmPressed += OnConfirmInput;

            // 구독 완료 후 즉시 숨김 — Start()는 첫 프레임 렌더링 전에 실행되므로 화면 깜빡임 없음
            gameObject.SetActive(false);
        }

        void Update()
        {
            if (stateManager.CurrentState != GameState.AimingPower) return;

            pingPongTime      += Time.deltaTime;
            currentNormalized  = Mathf.PingPong(pingPongTime * gaugeSpeed, 1f);

            float h = Mathf.Lerp(minHeight, maxHeight, currentNormalized);
            arrowShaft.sizeDelta = new Vector2(arrowShaft.sizeDelta.x, h);

            powerValueText.text = $"{Mathf.RoundToInt(currentNormalized * 100)}%";

            if (shaftImage != null)
                shaftImage.color = GetGaugeColor(currentNormalized);
        }

        private void HandleStateChanged(GameState prev, GameState next)
        {
            if (next == GameState.AimingPower)
            {
                gameObject.SetActive(true);
                pingPongTime = 0f;
                // 같은 프레임에 전이가 일어났다면 진입 직후의 confirm 입력은 무시한다
                // (BallAimer가 동일 입력 이벤트로 AimingPosition→AimingPower 전이를 트리거할 수 있음)
                enteredFrame = Time.frameCount;
            }
            if (prev == GameState.AimingPower)
                gameObject.SetActive(false);
        }

        private void OnConfirmInput()
        {
            if (stateManager.CurrentState != GameState.AimingPower) return;
            if (Time.frameCount == enteredFrame) return;

            ConfirmedNormalized = currentNormalized;
            Debug.Log($"[PowerGauge] 세기 확정: {ConfirmedNormalized * 100:F1}%");
            stateManager.ChangeState(GameState.Rolling);
        }

        private Color GetGaugeColor(float normalized)
        {
            float pct = normalized * 100f;
            if (pct <= 40f) return ColorLow;
            if (pct <= 70f) return ColorMid;
            return ColorHigh;
        }

        void OnDestroy()
        {
            if (stateManager   != null) stateManager.OnStateChanged     -= HandleStateChanged;
            if (inputController != null) inputController.OnConfirmPressed -= OnConfirmInput;
        }
    }
}
