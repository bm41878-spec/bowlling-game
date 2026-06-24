using UnityEngine;

namespace BowlingGame
{
    public class BallAimer : MonoBehaviour
    {
        [SerializeField] float laneHalfWidth = 0.43f;
        [SerializeField] float oscSpeed = 1.2f;
        [SerializeField, Tooltip("BallSpawnPoint 미발견 시 사용할 fallback Z.")] float ballStartZ = 0.5f;

        // 끝단 도달 판정 — |x| 가 laneHalfWidth * (1 - epsilon) 이상이면 도달로 간주.
        // PingPong 은 수치 정밀도로 정확히 ±halfWidth 에 닿지 않을 수 있어 약간의 여유가 필요.
        private const float EdgeEpsilon = 0.02f;

        private GameStateManager stateManager;
        private InputController inputController;
        private Rigidbody rb;
        // BowlingBall.cs 와 동일하게 Awake 에서 1회 캐싱 — 매 Update 의 Find 풀스캔 방지.
        private Transform spawnPoint;
        private float pingPongTime = 0f;
        private bool isAiming = false;
        private int enteredFrame = -1;
        private Vector3 confirmedPosition;

        // 접근성 — 조준 음향 가이드 (좌/우 끝단 도달 시 1회 비프).
        // AimingPosition 진입 시 SaveSystem.Load 로 1회 캐싱 (매 프레임 I/O 방지). 설정 변경은 다음 조준 사이클부터 반영.
        private bool audioGuideEnabled;
        // edgeLatched: true 면 현재 끝단 구간에 머무는 중 — 같은 도달에서 중복 발화 방지.
        // |x| 가 임계 아래로 내려갈 때 false 로 풀려 다음 도달에서 다시 1회 발화.
        private bool edgeLatched;

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

            if (audioGuideEnabled) UpdateEdgeBeep(x);
        }

        // 끝단 도달 시 1회 비프. edgeLatched 로 같은 끝에 머무는 동안 중복 발화를 막고,
        // 임계 아래로 내려갈 때 latch 해제 — 다음 사이클 반대편 끝에서 다시 1회 발화.
        private void UpdateEdgeBeep(float x)
        {
            float threshold = laneHalfWidth - EdgeEpsilon;
            float ax = Mathf.Abs(x);

            if (ax >= threshold)
            {
                if (edgeLatched) return;
                edgeLatched = true;
                // x > 0 = 우측 끝 (pan +1), x < 0 = 좌측 끝 (pan -1).
                AudioManager.Instance?.PlayAimEdgeBeep(x > 0f ? 1f : -1f);
            }
            else if (ax < threshold * 0.7f)
            {
                // 중간 구간 통과 시 latch 해제 — 0.7 hysteresis 로 임계 근처 진동에서 깜빡 발화 방지.
                edgeLatched = false;
            }
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
                // 조준 사이클 진입 시점에 접근성 설정 1회 캐싱 — 매 프레임 I/O 방지.
                audioGuideEnabled = SaveSystem.Load().aimingAudioGuide;
                edgeLatched = false;
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
