using UnityEngine;

namespace BowlingGame
{
    public class BallAimer : MonoBehaviour
    {
        [SerializeField] float laneHalfWidth = 0.43f;
        [SerializeField] float oscSpeed = 1.2f;
        [SerializeField, Tooltip("BallSpawnPoint 미발견 시 사용할 fallback Z.")] float ballStartZ = 0.5f;

        [Header("C 방식 — 방향만 (1번 입력)")]
        [SerializeField, Tooltip("조준선이 공을 중심으로 좌우로 흔들리는 최대 각도(도). 레인 가장자리가 약 3.7°, 거터가 약 3.9° 라 이보다 커야 빗나갈 여지가 생긴다. 레인 폭 프리셋이 배율을 곱한다.")]
        private float arcHalfAngleDegrees = 7f;
        [SerializeField, Tooltip("호 왕복 속도. 한 왕복 주기 = 4 / 이 값 (초). 1.6 이면 2.5초.")]
        private float arcSweepSpeed = 1.6f;
        [SerializeField, Range(0f, 1f), Tooltip("C 방식의 고정 투구 세기. BowlingBall 의 minForce~maxForce 를 보간한 값이 된다.")]
        private float fixedPower = 0.65f;

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
        private Vector3 confirmedDirection = Vector3.forward;

        // C 방식(방향만) 여부와 현재 호 각도. 조작 방식은 게임 시작 전에만 선택 가능하므로
        // Start 에서 1회만 읽는다 — 투구 도중 단계 구성이 바뀌면 상태 머신이 중간 상태에 갇힌다.
        private bool singleInput;
        private float currentAimAngle;

        // 접근성 — 조준 음향 가이드 (좌/우 끝단 도달 시 1회 비프).
        // AimingPosition 진입 시 SaveSystem.Load 로 1회 캐싱 (매 프레임 I/O 방지). 설정 변경은 다음 조준 사이클부터 반영.
        private bool audioGuideEnabled;
        // edgeLatched: true 면 현재 끝단 구간에 머무는 중 — 같은 도달에서 중복 발화 방지.
        // |x| 가 임계 아래로 내려갈 때 false 로 풀려 다음 도달에서 다시 1회 발화.
        private bool edgeLatched;

        public Vector3 ConfirmedPosition => confirmedPosition;

        /// <summary>확정된 발사 방향 (정규화, XZ 평면). A/B 방식에서는 항상 <see cref="Vector3.forward"/>.</summary>
        public Vector3 ConfirmedDirection => confirmedDirection;

        /// <summary>C 방식(방향만, 1번 입력) 여부. 게임 씬 로드 시점에 고정된다.</summary>
        public bool SingleInputAiming => singleInput;

        /// <summary>C 방식은 세기 단계가 없으므로 고정 세기를 쓴다.</summary>
        public bool UsesFixedPower => singleInput;

        /// <summary>C 방식의 고정 투구 세기 [0,1].</summary>
        public float FixedPower => fixedPower;

        /// <summary>조준 중 실시간 방향. 예상 경로 가이드가 사용한다.</summary>
        public Vector3 CurrentAimDirection =>
            singleInput ? DirectionFromAngle(currentAimAngle) : Vector3.forward;

        /// <summary>각도(도) → XZ 평면 방향 벡터. 0° 가 정면(+Z), 양수가 오른쪽.</summary>
        private static Vector3 DirectionFromAngle(float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(r), 0f, Mathf.Cos(r));
        }

        /// <summary>
        void Awake()
        {
            var spawnGo = GameObject.Find("BallSpawnPoint");
            spawnPoint = spawnGo != null ? spawnGo.transform : null;
            if (spawnPoint == null)
                Debug.LogWarning($"[BallAimer] BallSpawnPoint 미발견 — Y 는 transform 차용, Z 는 ballStartZ({ballStartZ}) fallback");
        }

        void Start()
        {
            // 조작 방식은 게임 시작 전에만 선택 가능 — 여기서 1회 고정한다.
            singleInput = ControlScheme.IsSingleInput(SaveSystem.Load().controlScheme);
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

            // Y/Z 는 spawnPoint 의 canonical 값 차용 — transform 차용 시의 interpolation/catch-up 잔여 영향 차단.
            float y = spawnPoint != null ? spawnPoint.position.y : transform.position.y;
            float z = spawnPoint != null ? spawnPoint.position.z : ballStartZ;

            if (singleInput)
            {
                // C 방식 — 공은 레인 중앙에 고정하고, 조준 방향만 공을 중심으로 호를 그리며 왕복한다.
                float sweep = Mathf.PingPong(pingPongTime * arcSweepSpeed, 2f) - 1f;   // -1 ~ +1
                currentAimAngle = sweep * arcHalfAngleDegrees;
                transform.position = new Vector3(0f, y, z);
                // 끝단 비프 재사용 — 호의 양 끝에서 울리도록 값 범위만 환산한다.
                if (audioGuideEnabled) UpdateEdgeBeep(sweep * laneHalfWidth);
                return;
            }

            float x = Mathf.PingPong(pingPongTime * oscSpeed, laneHalfWidth * 2f) - laneHalfWidth;
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
                // 물리 엔진이 transform 직접 제어를 방해하지 않도록 kinematic 전환.
                // velocity 클리어는 동적일 때만 — 이미 kinematic 인 경우(ForceResetCoroutine 진행 중
                // 상태 전이가 겹친 케이스) velocity setter 는 Unity 경고 + 무시.
                if (rb != null)
                {
                    if (!rb.isKinematic) rb.linearVelocity = Vector3.zero;
                    rb.isKinematic = true;
                }
                // 조준 사이클 진입 시점에 접근성 설정 1회 캐싱 — 매 프레임 I/O 방지.
                audioGuideEnabled = SaveSystem.Load().aimingAudioGuide;
                edgeLatched = false;
            }

            if (prev == GameState.AimingPosition)
            {
                isAiming = false;
                // kinematic 해제는 하지 않는다 — 공은 발사 순간(BowlingBall.Launch)까지 얼려 둔다.
                // 이전에는 여기서 dynamic 으로 되돌려, 세기/커브 게이지를 재는 동안
                // 공이 중력·접촉 해소로 스스로 움직였다.
                // 조준(위치) 절차 종료 — 끝단 비프 latch 해제 + 재생 중이던 비프 즉시 정지.
                edgeLatched = false;
                AudioManager.Instance?.StopAimBeep();
            }
        }

        private void OnConfirmInput()
        {
            if (stateManager.CurrentState != GameState.AimingPosition) return;
            if (Time.frameCount == enteredFrame) return;

            confirmedPosition = transform.position;

            if (singleInput)
            {
                // C 방식 — 방향 하나로 끝. 세기·커브 단계를 건너뛰고 바로 발사한다.
                confirmedDirection = DirectionFromAngle(currentAimAngle);
                Debug.Log($"[BallAimer] 방향 확정: {currentAimAngle:F1}° " +
                          $"(고정 세기 {fixedPower * 100:F0}%, 커브 없음)");
                stateManager.ChangeState(GameState.Rolling);
                return;
            }

            confirmedDirection = Vector3.forward;
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
