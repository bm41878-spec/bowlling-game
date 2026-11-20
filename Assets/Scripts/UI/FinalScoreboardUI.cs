using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Bowling.Scoring;

namespace BowlingGame
{
    /// <summary>
    /// 결과 화면(<c>Gameover_scene</c>)의 최종 스코어보드 — 인게임 <see cref="ScoreboardUI"/> 의 결과 화면 짝.
    /// <see cref="GameResultHolder.LastFrames"/> 스냅샷을 읽어 <see cref="ScoreboardLayoutRenderer"/> 에 한 번에 그린다.
    /// </summary>
    /// <remarks>
    /// 인게임 스코어보드와의 차이:
    /// <list type="bullet">
    ///   <item>데이터 출처 — 인게임은 <c>FrameManager</c> 의 5개 이벤트를 <b>실시간 구독</b>,
    ///         이쪽은 씬이 갈려 FrameManager 가 없으므로 <see cref="GameResultHolder"/> 의 <b>스냅샷을 1회 재생</b></item>
    ///   <item>배치 — 인게임은 좌측 세로 컬럼(<c>FrameRow.prefab</c>), 이쪽은 중앙 가로 크게(<c>FrameCard.prefab</c>).
    ///         배치 차이는 코드가 아니라 <b>컨테이너의 레이아웃 그룹과 프리팹</b>이 만든다 — 같은 CardLayoutRenderer 를 재사용한다</item>
    /// </list>
    /// 누적 점수는 <c>FrameScore</c> 를 앞에서부터 더해 구한다 — 독립 프레임 점수 방식이라
    /// <c>FrameManager.GetTotalScore()</c>(전체 합) 와 같은 규칙이다.
    /// </remarks>
    public class FinalScoreboardUI : MonoBehaviour
    {
        private const string LogPrefix = "[FinalScoreboard]";

        [Header("Layout Renderer")]
        [SerializeField, Tooltip("CardLayoutRenderer — 결과 화면용 가로 컨테이너 + FrameCard.prefab 으로 배선. 추상 타입 유지(§7-23).")]
        private ScoreboardLayoutRenderer layout;

        [Header("Title (optional)")]
        [SerializeField, Tooltip("상단 제목. 미할당 시 생략")]
        private TMP_Text titleLabel;

        // 제목 포맷 (단일 출처). {0} = 모드명.
        private const string TITLE_FORMAT   = "{0} 결과";
        private const string TITLE_FALLBACK = "게임 결과";

        private void Start()
        {
            if (layout == null)
            {
                Debug.LogError($"{LogPrefix} layout 미할당 — 최종 스코어보드를 그릴 수 없음");
                enabled = false;
                return;
            }

            var holder = GameResultHolder.Instance;
            ApplyTitle(holder.LastModeName);

            var frames = holder.LastFrames;
            if (frames == null || frames.Count == 0)
            {
                // Gameover_scene 단독 Play / 결과 없는 직접 진입.
                Debug.LogWarning($"{LogPrefix} 프레임 스냅샷 없음 — 빈 스코어보드로 표시");
                layout.Initialize(0);
                return;
            }

            Render(frames, holder.LastScore);
        }

        private void ApplyTitle(string modeName)
        {
            if (titleLabel == null) return;
            titleLabel.text = string.IsNullOrEmpty(modeName)
                ? TITLE_FALLBACK
                : string.Format(TITLE_FORMAT, modeName);
        }

        /// <summary>스냅샷을 1회 재생 — 프레임마다 완료 상태로 바로 확정 표시한다.</summary>
        private void Render(IReadOnlyList<Frame> frames, int finalScore)
        {
            layout.Initialize(frames.Count);

            int cumulative = 0;
            for (int i = 0; i < frames.Count; i++)
            {
                var frame = frames[i];
                cumulative += frame.FrameScore;
                // UpdateFrameComplete 가 1·2구 표시와 누적 점수를 함께 확정한다 — UpdateThrow 선행 호출 불필요.
                layout.UpdateFrameComplete(i, frame, cumulative);
            }

            // 진행 중 강조 없음 + 종료 배경색으로 마무리.
            layout.SetGameOver(finalScore);

            Debug.Log($"{LogPrefix} 최종 스코어보드 표시 — {frames.Count}프레임, 최종 {finalScore}점 (누적 합 {cumulative})");
        }
    }
}
