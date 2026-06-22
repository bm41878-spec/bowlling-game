using UnityEngine;
using Bowling.Scoring;

namespace BowlingGame
{
    /// <summary>
    /// 점수판 레이아웃 렌더링의 추상 베이스 — 구체 구현체 (CardLayoutRenderer / TableLayoutRenderer 등)
    /// 가 상속한다. <see cref="ScoreboardUI"/> 가 데이터 수집 + 본 추상 API 호출만 담당하고,
    /// 화면 표시 방식의 차이는 모두 본 추상의 구현체에 격리된다.
    /// </summary>
    /// <remarks>
    /// 라이프사이클:
    /// <list type="number">
    ///   <item><see cref="Initialize"/> — 게임 시작 시 1회 (frameCount 만큼 카드/행 생성)</item>
    ///   <item>매 투구마다 <see cref="UpdateThrow"/> (현재 진행 중 1·2구 표시)</item>
    ///   <item>프레임 완료 직후 <see cref="UpdateFrameComplete"/> (누적 점수 확정 표시)</item>
    ///   <item>프레임 전이 시 <see cref="SetActiveFrame"/> (현재 강조 위치 이동)</item>
    ///   <item>게임 종료 시 <see cref="SetGameOver"/> (모든 강조 해제 + 최종 점수 알림)</item>
    /// </list>
    /// 호출자는 <see cref="ClearAll"/> 로 재시작 진입을 표시 — Initialize 직전에 호출 안전.
    /// </remarks>
    public abstract class ScoreboardLayoutRenderer : MonoBehaviour
    {
        /// <summary>게임 시작 시 1회 — frameCount 만큼 카드/행을 생성하고 빈 상태로 초기화.</summary>
        public abstract void Initialize(int frameCount);

        /// <summary>매 투구 직후 — 해당 프레임의 1·2구 표시를 현재 시점 frame 으로 갱신.</summary>
        /// <param name="frameIndex">0-base</param>
        /// <param name="throwNumber">방금 기록된 투구 (1 또는 2)</param>
        public abstract void UpdateThrow(int frameIndex, int throwNumber, Frame frame);

        /// <summary>프레임 완료 직후 — 누적 점수 확정. cumulativeScore 는 frames[0..=frameIndex] 의 합.</summary>
        public abstract void UpdateFrameComplete(int frameIndex, Frame frame, int cumulativeScore);

        /// <summary>현재 진행 중 프레임 강조 위치 이동. -1 이면 강조 해제.</summary>
        public abstract void SetActiveFrame(int frameIndex);

        /// <summary>게임 종료 — 모든 강조 해제 + 최종 점수 표시 (구현체에서 활용 여부 자유).</summary>
        public abstract void SetGameOver(int finalScore);

        /// <summary>전체 표시 초기화 (생성된 카드/행은 유지, 텍스트만 빈 상태로). 재시작 직전 호출 가능.</summary>
        public abstract void ClearAll();
    }
}
