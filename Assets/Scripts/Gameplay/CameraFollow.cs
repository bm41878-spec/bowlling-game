using UnityEngine;

namespace BowlingGame
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Vector3 stopPosition     = new Vector3(0f, 1.70000005f, 12f);
        [SerializeField] private float   followSmoothTime = 0.25f;
        [SerializeField] private float   returnSmoothTime = 0.4f;

        // ✅ [추가] SerializeField 대신 const → 컴파일 타임 인라인, 매 프레임 필드 조회 비용 제거
        private const float FollowOffsetZ = 1.7f;

        private Transform        ballTransform;
        private GameStateManager stateManager;

        private Vector3 initialPosition;
        private Vector3 dampVelocity;

        private enum Mode { Idle, Following, Returning }
        private Mode mode = Mode.Idle;

        private const float ArriveSqrEpsilon = 0.0001f;

        void Start()
        {
            initialPosition = transform.position;

            var ball = FindFirstObjectByType<BowlingBall>();
            if (ball != null) ballTransform = ball.transform;

            stateManager = GameStateManager.Instance;
            if (stateManager != null)
                stateManager.OnStateChanged += HandleStateChanged;
        }

        void OnDestroy()
        {
            if (stateManager != null)
                stateManager.OnStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(GameState _, GameState next)
        {
            switch (next)
            {
                case GameState.Rolling:
                    mode         = Mode.Following;
                    dampVelocity = Vector3.zero;
                    break;
                case GameState.AimingPosition:
                    mode         = Mode.Returning;
                    dampVelocity = Vector3.zero;
                    break;
            }
        }

        void LateUpdate()
        {
            if (mode == Mode.Idle) return;

            if (mode == Mode.Following) StepFollow();
            else                        StepReturn();
        }

        private void StepFollow()
        {
            if (ballTransform == null) { mode = Mode.Idle; return; }

            // ✅ [수정] 공 Z좌표에서 FollowOffsetZ(1.7f)만큼 뒤에서 추적
            float   targetZ = Mathf.Min(ballTransform.position.z - FollowOffsetZ, stopPosition.z);
            Vector3 target  = new Vector3(stopPosition.x, stopPosition.y, targetZ);

            transform.position = Vector3.SmoothDamp(
                transform.position, target, ref dampVelocity, followSmoothTime);

            if (targetZ >= stopPosition.z &&
                (transform.position - target).sqrMagnitude < ArriveSqrEpsilon)
            {
                transform.position = target;
                mode = Mode.Idle;
            }
        }

        private void StepReturn()
        {
            transform.position = Vector3.SmoothDamp(
                transform.position, initialPosition, ref dampVelocity, returnSmoothTime);

            if ((transform.position - initialPosition).sqrMagnitude < ArriveSqrEpsilon)
            {
                transform.position = initialPosition;
                mode = Mode.Idle;
            }
        }
    }
}
