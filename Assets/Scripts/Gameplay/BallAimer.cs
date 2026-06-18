using UnityEngine;

namespace BowlingGame
{
    public class BallAimer : MonoBehaviour
    {
        [SerializeField] float laneHalfWidth = 0.43f;
        [SerializeField] float oscSpeed = 1.2f;
        [SerializeField, Tooltip("BallSpawnPoint 미발견 시 사용할 fallback Z.")] float ballStartZ = 0.5f;

        private GameStateManager stateManager;
        private InputController inputController;
        private Rigidbody rb;
        // BowlingBall.cs 와 동일하게 Awake 에서 1회 캐싱 — 매 Update 의 Find 풀스캔 방지.
        private Transform spawnPoint;
        private float pingPongTime = 0f;
        private bool isAiming = false;
        private int enteredFrame = -1;
        private Vector3 confirmedPosition;

        public Vector3 ConfirmedPosition => confirmedPosition;

        void Awake()
        {
            var spawnGo = GameObject.Find("BallSpawnPoint");
            spawnPoint = spawnGo != null ? spawnGo.transform : null;
            if (spawnPoint == null)
                Debug.LogWarning($"[BallAimer] BallSpawnPoint 미발견 — Y 는 transform 차용, Z 는 ballStartZ({ballStartZ}) fallback");
        }

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
            // Y/Z 는 spawnPoint 의 canonical 값 차용 — transform 차용 시의 interpolation/catch-up 잔여 영향 차단.
            float y = spawnPoint != null ? spawnPoint.position.y : transform.position.y;
            float z = spawnPoint != null ? spawnPoint.position.z : ballStartZ;
            transform.position = new Vector3(x, y, z);
        }

        private void HandleStateChanged(GameState prev, GameState next)
        {
            if (next == GameState.AimingPosition)
            {
                isAiming = true;
                pingPongTime = 0f;
                // 동일 프레임 confirm 캐스케이드 방지 (다른 핸들러가 이 프레임에 상태를 AimingPosition으로 바꿨을 수 있음)
                enteredFrame = Time.frameCount;
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
            if (Time.frameCount == enteredFrame) return;

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
