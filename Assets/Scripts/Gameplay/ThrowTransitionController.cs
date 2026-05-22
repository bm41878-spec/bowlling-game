using System;
using UnityEngine;
using Bowling.Scoring;

namespace BowlingGame
{
    /// <summary>투구 종료 후 처리 결과 — 외부 상태 머신(GameManager)이 이를 보고 다음 상태를 결정.</summary>
    public enum TransitionResult
    {
        /// <summary>같은 프레임의 2구로 진행.</summary>
        ProceedToBall2,
        /// <summary>다음 프레임의 1구로 진행.</summary>
        ProceedToNextFrame,
        /// <summary>게임 종료.</summary>
        GameOver
    }

    /// <summary>
    /// 한 투구가 끝난 후의 후처리 흐름을 조정한다.
    /// <see cref="FrameManager"/> 의 도메인 상태를 읽고
    /// <see cref="PinManager"/> · <see cref="BowlingBall"/> 에 적절한 명령을 내리는 코디네이터.
    /// 본인은 상태 머신을 보유하지 않으며, 결과만 이벤트로 통지한다.
    /// </summary>
    /// <remarks>
    /// 호출 순서 (중요 — 어긋나면 카운트 오류):
    /// <list type="number">
    ///   <item><see cref="PhysicsSettleDetector"/> 의 OnSettled 발행</item>
    ///   <item><see cref="HandlePostThrow"/> 호출 — 이 메서드 내부에서 다음 순서로 처리:
    ///     <list type="number">
    ///       <item>GetNewlyFallenCount() — 핀이 아직 씬에 남아 있어야 함</item>
    ///       <item>RecordThrow(count) — 도메인 상태 갱신</item>
    ///       <item>완료 판정 분기 → 핀 제거 또는 리셋, 공 리셋</item>
    ///       <item>다음 투구가 있다면 SnapshotBeforeThrow() 호출 (게임 종료 시는 호출 안 함)</item>
    ///     </list>
    ///   </item>
    /// </list>
    /// </remarks>
    public class ThrowTransitionController : MonoBehaviour
    {
        [SerializeField] private PinManager pinManager;
        [Tooltip("BallController 분리 전 임시 — BowlingBall 직접 참조.")]
        [SerializeField] private BowlingBall ball;

        private FrameManager frameManager;

        /// <summary>전이 처리 완료 시 결과와 함께 1회 발행.</summary>
        public event Action<TransitionResult> OnTransitionComplete;

        /// <summary>FrameManager 인스턴스 주입. GameManager 가 게임 시작 시 호출한다.</summary>
        public void Initialize(FrameManager frameManager)
        {
            this.frameManager = frameManager ?? throw new ArgumentNullException(nameof(frameManager));
        }

        /// <summary>SettleDetector.OnSettled 핸들러에서 호출.</summary>
        public TransitionResult HandlePostThrow()
        {
            if (frameManager == null)
                throw new InvalidOperationException("Initialize(FrameManager) must be called before HandlePostThrow().");

            // 방어적 가드 — 정상 흐름에서는 RecordThrow 단계에서 예외가 나기 전에 도달하지 않음.
            if (frameManager.IsGameOver())
                return EmitGameOver();

            // ===== 1. 카운트 획득 (핀이 아직 씬에 남아 있는 상태) =====
            int newlyFallen = pinManager.GetNewlyFallenCount();

            // RecordThrow 전 시점의 프레임/투구 번호 캐싱 (로그용 + 스트라이크 판별)
            int currentFrameNo = frameManager.GetCurrentFrameIndex() + 1;
            int throwNumber    = frameManager.GetCurrentThrowNumber();

            // ===== 2. 도메인 상태 갱신 =====
            frameManager.RecordThrow(newlyFallen);

            // ===== 3. 분기 =====

            // 3-A. 1구 완료 + 프레임 미완료 → 2구로 진행
            if (!frameManager.IsFrameComplete())
            {
                ball.ResetToStartPosition();
                pinManager.RemoveFallenPins();          // 쓰러진 핀만 제거, 남은 핀 위치 유지
                Debug.Log($"[Transition] 1구 완료, 핀 {newlyFallen}개 쓰러뜨림 - 2구로 진행");
                pinManager.SnapshotBeforeThrow();        // 다음 투구 직전 스냅샷
                return Emit(TransitionResult.ProceedToBall2);
            }

            // 3-B. 프레임 완료 — 다음 프레임으로 또는 게임 종료
            bool wasStrike = throwNumber == 1 && newlyFallen == 10;

            ball.ResetToStartPosition();
            pinManager.ResetAllPins();                   // 모든 핀 다시 세움
            frameManager.AdvanceToNextFrame();           // 이 시점에 마지막 프레임이었다면 IsGameOver 가 true 가 됨

            if (frameManager.IsGameOver())
                return EmitGameOver();

            int nextFrameNo = frameManager.GetCurrentFrameIndex() + 1;
            if (wasStrike)
                Debug.Log($"[Transition] 스트라이크! 2구 건너뛰고 다음 프레임으로 (프레임 {currentFrameNo} → {nextFrameNo})");
            else
                Debug.Log($"[Transition] 프레임 완료, 다음 프레임으로 진행 (프레임 {currentFrameNo} → {nextFrameNo})");

            pinManager.SnapshotBeforeThrow();            // 다음 프레임 1구 직전 스냅샷
            return Emit(TransitionResult.ProceedToNextFrame);
        }

        private TransitionResult EmitGameOver()
        {
            ball.ResetToStartPosition();
            pinManager.ResetAllPins();
            Debug.Log($"[Transition] 게임 종료, 최종 점수: {frameManager.GetTotalScore()}");
            return Emit(TransitionResult.GameOver);
        }

        private TransitionResult Emit(TransitionResult result)
        {
            OnTransitionComplete?.Invoke(result);
            return result;
        }
    }
}
