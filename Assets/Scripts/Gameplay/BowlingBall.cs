using UnityEngine;

namespace BowlingGame
{
    public class BowlingBall : MonoBehaviour
    {
        [SerializeField] float minForce = 8f;
        [SerializeField] float maxForce = 18f;

        // BallSpawnPoint 의 시작 위치 미발견 시 사용하는 fallback. Awake 에서 한 번 캐싱하여
        // 매 리셋마다 GameObject.Find 호출을 피한다.
        private static readonly Vector3 FallbackSpawnPosition = new Vector3(0f, 0.15f, 0.5f);

        private Rigidbody rb;
        private BallAimer ballAimer;
        private PowerGaugeUI powerGauge;
        private GameStateManager stateManager;
        private InputController inputController;
        private Transform spawnPoint;
        private bool hasLaunched = false;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            var spawnGo = GameObject.Find("BallSpawnPoint");
            spawnPoint = spawnGo != null ? spawnGo.transform : null;
            if (spawnPoint == null)
                Debug.LogWarning($"[Ball] BallSpawnPoint 미발견 — fallback 위치 {FallbackSpawnPosition} 사용");
        }

        void Start()
        {
            stateManager    = GameStateManager.Instance;
            inputController = InputController.Instance;
            ballAimer       = GetComponent<BallAimer>();
            // PowerGaugeUI는 비활성 자식까지 포함해 탐색
            powerGauge = FindFirstObjectByType<PowerGaugeUI>(FindObjectsInactive.Include);

            stateManager.OnStateChanged += HandleStateChanged;
        }

        private void HandleStateChanged(GameState prev, GameState next)
        {
            // AimingPosition 진입 시 위치 리셋은 GameManager.BeginGame / ThrowTransitionController
            // 가 명시적으로 호출한다 (이중 호출 방지 — 단일 리셋 경로 유지).
            if (next == GameState.Rolling)
                ExecuteLaunch();
        }

        private void ExecuteLaunch()
        {
            if (hasLaunched) return;
            hasLaunched = true;

            Vector3 launchPos = ballAimer.ConfirmedPosition;
            transform.position = launchPos;

            float force = powerGauge.ConfirmedNormalized;
            Launch(launchPos, force);

            Debug.Log($"[Ball] 발사! 위치: {launchPos.x:F2}, 세기: {force * 100:F1}%");
            // Scoring 전이는 GameManager 가 PhysicsSettleDetector.OnSettled 를 받아 처리한다.
        }

        public void Launch(Vector3 startPos, float normalizedForce)
        {
            transform.position = startPos;
            float force = Mathf.Lerp(minForce, maxForce, normalizedForce);
            rb.AddForce(Vector3.forward * force, ForceMode.Impulse);
        }

        public bool IsRolling => rb.linearVelocity.magnitude > 0.05f;

        public void ResetBall(Vector3 position)
        {
            // 1) 운동 상태 클리어 — 동적 일 때만. kinematic 상태에서 velocity setter 호출 시 Unity 경고.
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // 2) 물리 시뮬레이션 차단 — CCD / Interpolation / 중력 영향 0.
            rb.isKinematic = true;

            // 3) Transform 위치·회전 갱신 (kinematic 상태에서 안전).
            transform.position = position;
            transform.rotation = Quaternion.identity;

            // 4) Transform → 물리 엔진 명시적 sync.
            //    Unity 6 기본값 autoSyncTransforms=false 환경에서 이 호출이 없으면
            //    Rigidbody.position 이 stale 한 채 dynamic 으로 복귀하여 잘못된 위치에서 시뮬레이션.
            Physics.SyncTransforms();

            // 5) 누적 force / contact buffer / Interpolation 의 previousPosition 등 내부 상태 정리.
            //    이전 버전의 y 드리프트 회귀 원인 — Sleep() 누락으로 잔여 상태가 매 리셋마다 누적됨.
            rb.Sleep();

            // 6) 동적 복귀 — 깨끗한 상태로 시뮬레이션 재개.
            rb.isKinematic = false;
            hasLaunched = false;
        }

        // BallController 분리 전 임시 래퍼 — ThrowTransitionController 등 조정자 코드의 호출 지점을 안정화한다.
        public void ResetToStartPosition()
        {
            Vector3 resetPos = spawnPoint != null ? spawnPoint.position : FallbackSpawnPosition;
            ResetBall(resetPos);
        }

        // TODO: 거터 진입 시 점수 시스템 연계 예정 (Phase 미정).
        //   - 활용 후보: 거터 강제 0점 처리 / 거터 진입 효과음 / UI 안내.
        //   - 현재는 판정만 노출하고 호출처 없음.
        public bool IsInGutter => Mathf.Abs(transform.position.x) > 0.533f;

        void OnDestroy()
        {
            if (stateManager != null)
                stateManager.OnStateChanged -= HandleStateChanged;
        }
    }
}
