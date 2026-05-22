using UnityEngine;

namespace BowlingGame
{
    public class BowlingBall : MonoBehaviour
    {
        [SerializeField] float minForce = 8f;
        [SerializeField] float maxForce = 18f;

        private Rigidbody rb;
        private BallAimer ballAimer;
        private PowerGaugeUI powerGauge;
        private GameStateManager stateManager;
        private InputController inputController;
        private bool hasLaunched = false;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
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
            if (next == GameState.Rolling)
                ExecuteLaunch();

            if (next == GameState.AimingPosition)
            {
                var spawnPoint = GameObject.Find("BallSpawnPoint");
                Vector3 resetPos = spawnPoint != null
                    ? spawnPoint.transform.position
                    : new Vector3(0f, 0.15f, 0.5f);
                ResetBall(resetPos);
            }
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
            // 동적 바디일 때 velocity 0 → 이후 kinematic 으로 이동/회전 → 다시 동적 복귀.
            // (kinematic 상태에서 velocity 설정 시 Unity 경고 발생하므로 순서 중요)
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.isKinematic = true;
            transform.position = position;
            transform.rotation = Quaternion.identity;
            rb.isKinematic = false;
            hasLaunched = false;
        }

        // BallController 분리 전 임시 래퍼 — ThrowTransitionController 등 조정자 코드의 호출 지점을 안정화한다.
        public void ResetToStartPosition()
        {
            var spawnPoint = GameObject.Find("BallSpawnPoint");
            Vector3 resetPos = spawnPoint != null
                ? spawnPoint.transform.position
                : new Vector3(0f, 0.15f, 0.5f);
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
