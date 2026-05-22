namespace Bowling.Scoring
{
    /// <summary>
    /// 한 프레임의 결과 종류.
    /// </summary>
    public enum FrameType
    {
        /// <summary>두 번 굴려 핀이 남은 일반 프레임.</summary>
        Normal,

        /// <summary>두 번째 투구로 10개를 모두 쓰러뜨린 프레임.</summary>
        Spare,

        /// <summary>첫 번째 투구로 10개를 모두 쓰러뜨린 프레임.</summary>
        Strike
    }
}
