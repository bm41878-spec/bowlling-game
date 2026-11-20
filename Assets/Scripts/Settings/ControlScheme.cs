namespace BowlingGame
{
    /// <summary>
    /// 조준 조작 방식. <see cref="SaveData.controlScheme"/> 인덱스로 선택한다.
    /// </summary>
    /// <remarks>
    /// 입력 횟수와 조준 단계 구성이 통째로 달라진다.
    /// <list type="table">
    ///   <item><term>A</term><description>위치 → 세기 (2번). 기존 기본 방식</description></item>
    ///   <item><term>B</term><description>위치 → 세기 → 커브 (3번)</description></item>
    ///   <item><term>C</term><description>방향만 (1번). 공을 중심으로 호를 그리며 왕복하는 조준선에서 1회 확정,
    ///   세기는 고정값이고 커브는 없다</description></item>
    /// </list>
    ///
    /// ⚠ <b>게임 시작 전에만 선택 가능</b> — <see cref="BallAimer"/> 가 게임 씬 로드 시 1회만 읽는다.
    /// 투구 도중 단계 구성이 바뀌면 상태 머신이 중간 상태에 갇힌다.
    ///
    /// ⚠ 배열 순서는 <see cref="SaveData.controlScheme"/> 에 그대로 저장되므로
    /// <b>순서 변경·중간 삽입 금지</b>. 추가는 반드시 끝에.
    /// </remarks>
    public static class ControlScheme
    {
        /// <summary>A — 위치 + 세기 (2번 입력). 기본값.</summary>
        public const int PositionPower = 0;
        /// <summary>B — 위치 + 세기 + 커브 (3번 입력).</summary>
        public const int PositionPowerSpin = 1;
        /// <summary>C — 방향만 (1번 입력). 호 조준 + 고정 세기.</summary>
        public const int DirectionOnly = 2;

        /// <summary>미설정(=0) 일 때의 기본값. <see cref="SaveData.controlScheme"/> 의 int 기본값과 일치해야 한다.</summary>
        public const int DefaultIndex = PositionPower;

        /// <summary>설정 드롭다운 표시 이름. 인덱스 = <see cref="SaveData.controlScheme"/>.</summary>
        public static readonly string[] Labels =
        {
            "A — 위치 + 세기 (2번 입력)",
            "B — 위치 + 세기 + 커브 (3번 입력)",
            "C — 방향만 (1번 입력)",
        };

        public static int Clamp(int index) =>
            index >= 0 && index < Labels.Length ? index : DefaultIndex;

        /// <summary>세기 확정 후 커브(AimingSpin) 단계로 가는 방식인가.</summary>
        public static bool UsesSpinStage(int index) => Clamp(index) == PositionPowerSpin;

        /// <summary>조준 1회로 끝나는 방식인가 (세기·커브 단계 없음).</summary>
        public static bool IsSingleInput(int index) => Clamp(index) == DirectionOnly;
    }
}
