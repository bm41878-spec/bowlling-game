namespace Bowling.Scoring
{
    /// <summary>
    /// 점수 계산에 사용되는 보너스 상수 모음.
    /// 밸런스 조정은 반드시 이 클래스 한 곳에서만 수행한다.
    /// </summary>
    public static class ScoringConstants
    {
        /// <summary>스페어 보너스. 최종 점수 = 10 + SPARE_BONUS.</summary>
        public const int SPARE_BONUS = 3;

        /// <summary>스트라이크 보너스. 최종 점수 = 10 + STRIKE_BONUS.</summary>
        public const int STRIKE_BONUS = 5;
    }
}
