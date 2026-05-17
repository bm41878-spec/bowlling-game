using System.Collections;
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
            StartCoroutine(WaitUntilStopped());
        }

        private IEnumerator WaitUntilStopped()
        {
            yield return new WaitForSeconds(1.5f);
            yield return new WaitUntil(() => !IsRolling);
            Debug.Log("[Ball] 정지 감지 → Scoring 전환");
            stateManager.ChangeState(GameState.Scoring);
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
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            transform.position = position;
            transform.rotation = Quaternion.identity;
            rb.isKinematic = false;
            hasLaunched = false;
        }

        public bool IsInGutter => Mathf.Abs(transform.position.x) > 0.533f;

        void OnDestroy()
        {
            if (stateManager != null)
                stateManager.OnStateChanged -= HandleStateChanged;
        }
    }
}
