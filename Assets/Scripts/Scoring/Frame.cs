// TODO: Phase 4 — ScoreCalculator와 함께 구현, 유닛 테스트 필수
namespace BowlingGame
{
    public class Frame
    {
        public int firstRoll = -1;
        public int secondRoll = -1;
        public int thirdRoll = -1;   // 마지막 프레임 전용
        public int? confirmedScore;  // null = 보너스 대기 중

        public bool IsStrike() => firstRoll == 10;
        public bool IsSpare() => firstRoll + secondRoll == 10 && firstRoll < 10;
    }
}
