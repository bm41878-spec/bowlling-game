// [TEST ONLY] R키로 볼링공 + 핀 전체 초기화. Phase 5 이전 테스트용으로 사용.
using UnityEngine;
using UnityEngine.InputSystem;

namespace BowlingGame
{
    public class DebugResetController : MonoBehaviour
    {
        private BowlingBall bowlingBall;
        private Pin[] pins;

        private Vector3        ballSpawnPos;
        private Vector3[]      pinInitialPositions;
        private Quaternion[]   pinInitialRotations;

        private InputAction resetAction;

        void Start()
        {
            // 볼링공 스폰 위치
            var spawnPoint = GameObject.Find("BallSpawnPoint");
            ballSpawnPos   = spawnPoint != null
                ? spawnPoint.transform.position
                : new Vector3(0f, 0.15f, 0.5f);

            bowlingBall = FindFirstObjectByType<BowlingBall>();

            // 핀 초기 위치/회전 저장 (Start 시점 = 물리 적용 전)
            pins = FindObjectsByType<Pin>(FindObjectsSortMode.InstanceID);
            pinInitialPositions = new Vector3[pins.Length];
            pinInitialRotations = new Quaternion[pins.Length];
            for (int i = 0; i < pins.Length; i++)
            {
                pinInitialPositions[i] = pins[i].transform.position;
                pinInitialRotations[i] = pins[i].transform.rotation;
            }

            // R 키 바인딩
            resetAction = new InputAction(type: InputActionType.Button);
            resetAction.AddBinding("<Keyboard>/r");
            resetAction.performed += _ => ResetAll();
            resetAction.Enable();
        }

        void OnDestroy()
        {
            resetAction?.Disable();
        }

        private void ResetAll()
        {
            StopDanglingCoroutines(); // 반드시 상태 전환 전에 코루틴 정리
            ResetBall();
            ResetPins();
            ResetState();
            Debug.Log("[Debug] R키 — 볼링공 + 핀 초기화 완료");
        }

        // WaitUntilStopped(BowlingBall)와 TempScoringDelay(GameStateManager)가
        // 리셋 이후에도 실행되어 상태를 오염시키는 것을 방지
        private void StopDanglingCoroutines()
        {
            if (bowlingBall != null)
                bowlingBall.StopAllCoroutines();

            var gsm = GameStateManager.Instance;
            if (gsm != null)
                gsm.StopAllCoroutines();
        }

        private void ResetBall()
        {
            if (bowlingBall != null)
                bowlingBall.ResetBall(ballSpawnPos);
        }

        private void ResetPins()
        {
            var fallenField = typeof(Pin).GetField(
                "_fallen",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            for (int i = 0; i < pins.Length; i++)
            {
                if (pins[i] == null) continue;

                var rb = pins[i].GetComponent<Rigidbody>();

                if (rb != null)
                {
                    rb.isKinematic      = true;
                    rb.linearVelocity   = Vector3.zero;
                    rb.angularVelocity  = Vector3.zero;
                }

                pins[i].transform.position = pinInitialPositions[i];
                pins[i].transform.rotation = pinInitialRotations[i];

                if (rb != null) rb.isKinematic = false;

                // Pin._fallen 플래그 초기화
                fallenField?.SetValue(pins[i], false);
            }
        }

        private void ResetState()
        {
            var gsm = GameStateManager.Instance;
            if (gsm == null) return;

            // Rolling 중이면 중단 후 리셋, 그 외 상태에서도 강제 복귀
            if (gsm.CurrentState != GameState.AimingPosition)
                gsm.ChangeState(GameState.AimingPosition);
        }
    }
}
