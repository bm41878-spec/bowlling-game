using UnityEngine;

namespace BowlingGame
{
    /// <summary>
    /// 게임 씬 로드 시 저장된 레인 폭 프리셋을 1회 적용한다. <c>Lane_Root</c> 에 부착.
    /// </summary>
    /// <remarks>
    /// <see cref="DefaultExecutionOrder"/> 가 음수인 이유 — <see cref="BallAimer"/> 의 조준 범위와
    /// <see cref="BowlingBall"/> 의 거터 임계가 폭에 연동되므로, 그 컴포넌트들이 값을 쓰기 전에
    /// 레인이 먼저 늘어나 있어야 한다.
    ///
    /// 적용은 씬당 정확히 한 번이다. <see cref="LaneWidthPreset.Apply"/> 는 현재 좌표에 배율을 곱하는
    /// 방식이라 두 번 호출하면 폭이 제곱으로 늘어난다 — 다른 곳에서 추가로 호출하지 말 것.
    /// </remarks>
    [DefaultExecutionOrder(-100)]
    public class LaneWidthSetter : MonoBehaviour
    {
        void Awake()
        {
            LaneWidthPreset.Apply(SaveSystem.Load().laneWidthPreset);
        }
    }
}
