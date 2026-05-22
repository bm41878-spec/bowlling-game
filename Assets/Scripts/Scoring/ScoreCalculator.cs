using System;
using System.Collections.Generic;

namespace Bowling.Scoring
{
    /// <summary>
    /// 프레임 점수 계산 유틸리티. 상태 없음(순수 함수).
    /// 외부 의존성 없음 — UnityEngine 미참조로 EditMode 유닛 테스트에서 단독 실행 가능.
    /// </summary>
    /// <remarks>
    /// 입력 검증 정책:
    /// <list type="bullet">
    ///   <item>ball1 또는 ball2 가 [0,10] 범위를 벗어나면 <see cref="ArgumentOutOfRangeException"/>.</item>
    ///   <item>스트라이크가 아닌데 ball1 + ball2 > 10 이면 <see cref="ArgumentException"/>.</item>
    ///   <item>스트라이크(ball1==10)일 때 ball2 는 무시되며 별도 검증/예외 없음.</item>
    /// </list>
    /// 인스턴스화는 가능하지만, 모든 메서드는 정적이므로 일반적으로 클래스명을 통해 호출한다.
    /// </remarks>
    public class ScoreCalculator
    {
        /// <summary>
        /// 한 프레임의 점수를 계산한다.
        /// 판정 우선순위: 스트라이크 → 스페어 → 일반.
        /// </summary>
        /// <param name="ball1">첫 번째 투구로 쓰러진 핀 수 [0,10].</param>
        /// <param name="ball2">두 번째 투구로 쓰러진 핀 수 [0,10]. 스트라이크 시 무시.</param>
        /// <returns>보너스가 포함된 프레임 최종 점수.</returns>
        public static int CalculateFrameScore(int ball1, int ball2)
        {
            ValidateInputs(ball1, ball2);

            if (ball1 == 10)
                return 10 + ScoringConstants.STRIKE_BONUS;
            if (ball1 + ball2 == 10)
                return 10 + ScoringConstants.SPARE_BONUS;
            return ball1 + ball2;
        }

        /// <summary>
        /// 투구 결과로부터 <see cref="FrameType"/> 을 판정한다.
        /// </summary>
        /// <param name="ball1">첫 번째 투구로 쓰러진 핀 수 [0,10].</param>
        /// <param name="ball2">두 번째 투구로 쓰러진 핀 수 [0,10]. 스트라이크 시 무시.</param>
        public static FrameType DetermineFrameType(int ball1, int ball2)
        {
            ValidateInputs(ball1, ball2);

            if (ball1 == 10) return FrameType.Strike;
            if (ball1 + ball2 == 10) return FrameType.Spare;
            return FrameType.Normal;
        }

        /// <summary>
        /// 모든 프레임의 <see cref="Frame.FrameScore"/> 를 단순 합산한 총점.
        /// 독립 프레임 방식이므로 이후 프레임이 이전 프레임 점수에 영향을 주지 않는다.
        /// </summary>
        /// <param name="frames">계산 대상 프레임 목록. null 이면 <see cref="ArgumentNullException"/>.</param>
        public static int CalculateTotalScore(List<Frame> frames)
        {
            if (frames == null)
                throw new ArgumentNullException(nameof(frames));

            int total = 0;
            for (int i = 0; i < frames.Count; i++)
            {
                if (frames[i] == null) continue;
                total += frames[i].FrameScore;
            }
            return total;
        }

        private static void ValidateInputs(int ball1, int ball2)
        {
            if (ball1 < 0 || ball1 > 10)
                throw new ArgumentOutOfRangeException(
                    nameof(ball1), ball1, "ball1 must be within [0,10].");

            // 스트라이크인 경우 ball2 는 정책상 무시 — 검증하지 않는다.
            if (ball1 == 10) return;

            if (ball2 < 0 || ball2 > 10)
                throw new ArgumentOutOfRangeException(
                    nameof(ball2), ball2, "ball2 must be within [0,10].");

            if (ball1 + ball2 > 10)
                throw new ArgumentException(
                    $"ball1 + ball2 cannot exceed 10 when not a strike (got {ball1} + {ball2}).");
        }
    }
}
