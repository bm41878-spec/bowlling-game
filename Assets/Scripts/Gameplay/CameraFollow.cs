using UnityEngine;

namespace BowlingGame
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Vector3 stopPosition     = new Vector3(0f, 1.70000005f, 12f);
        [SerializeField] private float   followSmoothTime = 0.25f;
        [SerializeField] private float   returnSmoothTime = 0.4f;
        [SerializeField] private float   stopOffsetFromHeadPin = 2.4f;

        // 추적 중 공 뒤로 유지할 거리. 씬에 직렬화된 값이 그대로 쓰인다
        // (2026-11-18 시점 프리셋 제거 — 카메라는 A값 고정).
        [SerializeField] private float followOffsetZ = 3.66f;

        private Transform        ballTransform;
        private GameStateManager stateManager;

        private Vector3 initialPosition;
        private Vector3 dampVelocity;

        private enum Mode { Idle, Following, Returning }
        private Mode mode = Mode.Idle;

        private const float ArriveSqrEpsilon = 0.0001f;

        void Start()
        {
            // 카메라 값은 씬에 고정돼 있다 (2026-11-18 프리셋 설정 제거).
            initialPosition = transform.position;
            RecomputeStopZ();

            var ball = FindFirstObjectByType<BowlingBall>();
            if (ball != null) ballTransform = ball.transform;

            stateManager = GameStateManager.Instance;
            if (stateManager != null)
                stateManager.OnStateChanged += HandleStateChanged;
        }

        /// <summary>
        /// 시점 프리셋을 적용한다 — 카메라 FOV / 위치 / 피치 + 추적 거리 3종을 한 번에.
        /// 설정에서 값을 바꾼 즉시 반영할 수 있도록 런타임 중에도 호출 가능하며,
        /// 복귀 지점(<c>initialPosition</c>) 과 정지 Z 를 함께 다시 계산한다.
        /// </summary>
        // head pin(Pin_01) 기준으로 정지 Z를 자동 계산
        private void RecomputeStopZ()
        {
            foreach (var pin in FindObjectsByType<Pin>(FindObjectsSortMode.None))
            {
                if (pin.pinId == 1)
                {
                    stopPosition.z = pin.transform.position.z - stopOffsetFromHeadPin;
                    break;
                }
            }
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

            // 공 Z좌표에서 followOffsetZ 만큼 뒤에서 추적 (거리는 시점 프리셋이 결정)
            float   targetZ = Mathf.Min(ballTransform.position.z - followOffsetZ, stopPosition.z);
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
