using UnityEngine;
using TMPro;
using Bowling.Scoring;

namespace BowlingGame
{
    /// <summary>
    /// 점수판 UI 의 데이터 수집·라우터 — FrameManager 이벤트를 구독하여
    /// 화면 표시는 <see cref="ScoreboardLayoutRenderer"/> 구현체(현 CardLayoutRenderer)에 위임한다.
    /// </summary>
    /// <remarks>
    /// 책임 분리:
    /// <list type="bullet">
    ///   <item>본 클래스 — FrameManager 이벤트 구독, 누적 점수 / 현재 프레임 라벨 / 총점 갱신 호출만</item>
    ///   <item>레이아웃 — <see cref="ScoreboardLayoutRenderer"/> 구현체가 카드/행/표 표시</item>
    /// </list>
    /// 누적 점수는 <see cref="FrameManager.GetTotalScore"/> 사용 — 미완료 프레임은 FrameScore=0 이므로
    /// OnFrameCompleted 시점의 GetTotalScore() 가 정확히 "그 프레임까지의 누적" 과 일치한다 (독립 프레임 점수 방식의 특성).
    /// 표시 규칙(X / / / 숫자) 은 <see cref="FrameCardUI"/> 의 상수에 캡슐화 — 본 클래스는 모름.
    /// </remarks>
    public class ScoreboardUI : MonoBehaviour
    {
        private const string LogPrefix = "[Scoreboard]";

        [Header("Domain")]
        [SerializeField, Tooltip("Game.unity 의 GameManager(ⓑ) 에 부착된 FrameManager 인스턴스를 드래그-드롭.")]
        private FrameManager frameManager;

        [Header("Layout Renderer")]
        [SerializeField, Tooltip("CardLayoutRenderer (옵션 B) 또는 추후 TableLayoutRenderer (옵션 C). 인스펙터에서 교체 가능.")]
        private ScoreboardLayoutRenderer layout;

        [Header("Side Labels (optional)")]
        [SerializeField, Tooltip("우측 큰 총점 표시 (선택 — 미할당 시 무시)")]
        private TMP_Text totalScoreText;

        [SerializeField, Tooltip("현재 진행 상태 표시 (예: '프레임 3 / 1구'). 선택")]
        private TMP_Text currentFrameLabel;

        void Start()
        {
            if (!Validate())
            {
                enabled = false;
                return;
            }

            frameManager.OnGameInitialized += HandleGameInitialized;
            frameManager.OnFrameStarted    += HandleFrameStarted;
            frameManager.OnThrowRecorded   += HandleThrowRecorded;
            frameManager.OnFrameCompleted  += HandleFrameCompleted;
            frameManager.OnGameOver        += HandleGameOver;

            Debug.Log($"{LogPrefix} 초기화 완료 — FrameManager 이벤트 구독 시작");
        }

        void OnDestroy()
        {
            if (frameManager == null) return;
            frameManager.OnGameInitialized -= HandleGameInitialized;
            frameManager.OnFrameStarted    -= HandleFrameStarted;
            frameManager.OnThrowRecorded   -= HandleThrowRecorded;
            frameManager.OnFrameCompleted  -= HandleFrameCompleted;
            frameManager.OnGameOver        -= HandleGameOver;
        }

        private bool Validate()
        {
            if (frameManager == null) { Debug.LogError($"{LogPrefix} frameManager 미할당"); return false; }
            if (layout == null)       { Debug.LogError($"{LogPrefix} layout 미할당");       return false; }
            return true;
        }

        // ---------- 이벤트 핸들러 ----------

        private void HandleGameInitialized()
        {
            int frameCount = frameManager.GetFrameCount();
            layout.Initialize(frameCount);
            if (totalScoreText != null)    totalScoreText.text = "0";
            if (currentFrameLabel != null) currentFrameLabel.text = "";
            Debug.Log($"{LogPrefix} 게임 초기화 — {frameCount}프레임 레이아웃 생성");
        }

        private void HandleFrameStarted(int frameIndex)
        {
            layout.SetActiveFrame(frameIndex);
            UpdateCurrentFrameLabel(frameIndex, 1);
        }

        private void HandleThrowRecorded(int frameIndex, int throwNumber, Frame frame)
        {
            layout.UpdateThrow(frameIndex, throwNumber, frame);

            // 1구 후 스트라이크 아니면 → 2구 대기 라벨로 전환
            if (throwNumber == 1 && !frame.IsStrike())
                UpdateCurrentFrameLabel(frameIndex, 2);
            // 스트라이크 1구 / 일반 2구 직후 → 곧 OnFrameCompleted → OnFrameStarted 가 다음 라벨 결정
        }

        private void HandleFrameCompleted(int frameIndex, Frame frame)
        {
            // OnFrameCompleted 시점의 GetTotalScore() == frames[0..frameIndex] 의 합 (미완료는 0)
            int cumulative = frameManager.GetTotalScore();
            layout.UpdateFrameComplete(frameIndex, frame, cumulative);
            if (totalScoreText != null) totalScoreText.text = cumulative.ToString();
            Debug.Log($"{LogPrefix} 프레임 {frameIndex + 1} 완료 — 누적 {cumulative}");
        }

        private void HandleGameOver()
        {
            int final = frameManager.GetTotalScore();
            layout.SetGameOver(final);
            if (totalScoreText != null)    totalScoreText.text = final.ToString();
            if (currentFrameLabel != null) currentFrameLabel.text = "게임 종료";
            Debug.Log($"{LogPrefix} 게임 종료 — 최종 점수 {final}");
        }

        // ---------- 라벨 헬퍼 ----------

        private void UpdateCurrentFrameLabel(int frameIndex, int throwNumber)
        {
            if (currentFrameLabel == null) return;
            currentFrameLabel.text = $"프레임 {frameIndex + 1} / {throwNumber}구";
        }
    }
}
