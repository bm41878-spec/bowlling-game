using UnityEngine;

namespace BowlingGame
{
    public class BallAimer : MonoBehaviour
    {
        [SerializeField] float laneHalfWidth = 0.43f;
        [SerializeField] float oscSpeed = 1.2f;
        [SerializeField] float ballStartZ = 0.5f;

        private GameStateManager stateManager;
        private InputController inputController;
        private Rigidbody rb;
        private float pingPongTime = 0f;
        private bool isAiming = false;
        private Vector3 confirmedPosition;

        public Vector3 ConfirmedPosition => confirmedPosition;

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            stateManager = GameStateManager.Instance;
            inputController = InputController.Instance;
            stateManager.OnStateChanged += HandleStateChanged;
            inputController.OnConfirmPressed += OnConfirmInput;
        }

        void Update()
        {
            if (!isAiming) return;

            pingPongTime += Time.deltaTime;
            float x = Mathf.PingPong(pingPongTime * oscSpeed, laneHalfWidth * 2f) - laneHalfWidth;
            transform.position = new Vector3(x, transform.position.y, ballStartZ);
        }

        private void HandleStateChanged(GameState prev, GameState next)
        {
            if (next == GameState.AimingPosition)
            {
                isAiming = true;
                pingPongTime = 0f;
                // 물리 엔진이 transform 직접 제어를 방해하지 않도록 kinematic 전환
                if (rb != null) { rb.linearVelocity = Vector3.zero; rb.isKinematic = true; }
            }

            if (prev == GameState.AimingPosition)
            {
                isAiming = false;
                if (rb != null) rb.isKinematic = false;
            }
        }

        private void OnConfirmInput()
        {
            if (stateManager.CurrentState != GameState.AimingPosition) return;

            confirmedPosition = transform.position;
            Debug.Log($"[BallAimer] 위치 확정: {confirmedPosition.x:F3}");
            stateManager.ChangeState(GameState.AimingPower);
        }

        void OnDestroy()
        {
            if (stateManager != null) stateManager.OnStateChanged -= HandleStateChanged;
            if (inputController != null) inputController.OnConfirmPressed -= OnConfirmInput;
        }
    }
}
