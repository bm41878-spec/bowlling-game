// TODO: Phase 4 — 전통 볼링 점수 계산 구현, 섹션 9 테스트 케이스로 검증
using System.Collections.Generic;

namespace BowlingGame
{
    public class ScoreCalculator
    {
        // 지정 프레임의 확정 점수를 반환. 보너스 투구가 아직 없으면 null.
        public int? CalculateFrameScore(List<Frame> frames, int targetIndex, int frameCount)
        {
            // TODO: isLast 판정 후 일반/마지막 프레임 분기 처리
            throw new System.NotImplementedException();
        }

        // 모든 프레임의 누적 점수 배열을 반환. 미확정 프레임은 null.
        public int?[] CalculateCumulativeScores(List<Frame> frames, int frameCount)
        {
            // TODO: CalculateFrameScore를 순서대로 호출해 누적합 계산
            throw new System.NotImplementedException();
        }
    }
}
