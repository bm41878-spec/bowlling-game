namespace Bowling.Scoring
{
    /// <summary>
    /// 한 프레임의 투구 결과와 점수를 담는 데이터 객체(POCO).
    /// 점수 계산 로직은 ScoreCalculator가 담당하며, 이 클래스는 상태만 보관한다.
    /// 외부에서의 임의 수정을 막기 위해 setter는 internal 로 제한한다.
    /// </summary>
    public class Frame
    {
        /// <summary>첫 번째 투구에서 쓰러뜨린 핀 개수.</summary>
        public int Ball1 { get; internal set; }

        /// <summary>두 번째 투구에서 쓰러뜨린 핀 개수. 스트라이크인 경우 0으로 유지.</summary>
        public int Ball2 { get; internal set; }

        /// <summary>프레임 종류(Normal / Spare / Strike).</summary>
        public FrameType FrameType { get; internal set; }

        /// <summary>이 프레임의 최종 점수(보너스 포함).</summary>
        public int FrameScore { get; internal set; }

        /// <summary>
        /// 초기 상태: 모든 핀 카운트 0, FrameType.Normal, 점수 0.
        /// </summary>
        public Frame()
        {
            Ball1 = 0;
            Ball2 = 0;
            FrameType = FrameType.Normal;
            FrameScore = 0;
        }

        /// <summary>스트라이크 여부.</summary>
        public bool IsStrike() => FrameType == FrameType.Strike;

        /// <summary>스페어 여부.</summary>
        public bool IsSpare() => FrameType == FrameType.Spare;
    }
}
