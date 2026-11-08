using UnityEngine;

namespace BowlingGame
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Vector3 stopPosition     = new Vector3(0f, 1.70000005f, 12f);
        [SerializeField] private float   followSmoothTime = 0.25f;
        [SerializeField] private float   returnSmoothTime = 0.4f;
        [SerializeField] private float   stopOffsetFromHeadPin = 2.4f;

        // 추적 중 공 뒤로 유지할 거리. 시점 프리셋마다 달라야 해서 const 가 아닌 필드다
        // (CameraPreset.Apply → ApplyPreset 이 덮어쓴다). 인스펙터 값은 프리셋 미적용 시의 폴백.
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
            // 저장된 시점 프리셋을 먼저 적용 — 카메라 FOV/위치/추적 거리를 모두 세팅한 뒤
            // 복귀 지점과 정지 Z 를 그 값 기준으로 잡는다.
            ApplyPreset(CameraPreset.Presets[CameraPreset.Clamp(SaveSystem.Load().cameraPreset)]);

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
        public void ApplyPreset(CameraPreset.Preset preset)
        {
            var cam = GetComponent<Camera>();
            if (cam != null) cam.fieldOfView = preset.FieldOfView;

            transform.position = preset.Position;
            transform.rotation = Quaternion.Euler(preset.PitchDegrees, 0f, 0f);
            initialPosition    = preset.Position;

            followOffsetZ         = preset.FollowOffsetZ;
            stopOffsetFromHeadPin = preset.StopOffsetFromHeadPin;
            stopPosition.x        = preset.Position.x;
            stopPosition.y        = preset.StopHeight;
            RecomputeStopZ();

            // 진행 중이던 보간은 취소 — 새 좌표로 갈아탄 상태에서 이어가면 카메라가 튄다.
            mode         = Mode.Idle;
            dampVelocity = Vector3.zero;
        }

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
