using System;
using UnityEngine;
using TMPro;
using Bowling.Scoring;

namespace BowlingGame
{
    /// <summary>
    /// 점수판 UI 갱신 담당. FrameManager 이벤트를 구독하여 TMP_Text 4개를 실시간 갱신.
    /// </summary>
    /// <remarks>
    /// 갱신 트리거:
    ///   OnGameInitialized → 전체 클리어
    ///   OnFrameStarted    → frame_n 갱신, first/sec 클리어
    ///   OnThrowRecorded   → frame_N_first 또는 frame_N_sec 갱신
    ///   OnFrameCompleted  → total_score_n 갱신 (스트라이크 시 sec 도 "-"로 갱신)
    ///   OnGameOver        → 전체 클리어
    /// 표시 규칙은 본 클래스에 캡슐화. FrameManager 는 표시 규칙을 모름.
    /// </remarks>
    public class ScoreboardUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("GameManager 가 들고 있는 동일 FrameManager 인스턴스를 드래그-드롭. 싱글톤 우회 — 명시적 의존성 가시화.")]
        private FrameManager frameManager;

        [Header("TMP Targets (Canvas 하위)")]
        [SerializeField, Tooltip("Canvas > total_score > total_score_n")]
        private TMP_Text totalScoreText;

        [SerializeField, Tooltip("Canvas > current_frame > frame_n")]
        private TMP_Text currentFrameText;

        [SerializeField, Tooltip("Canvas > frame_N_first")]
        private TMP_Text frameFirstText;

        [SerializeField, Tooltip("Canvas > frame_N_sec")]
        private TMP_Text frameSecText;

        // 표시 규칙 단일 출처(single source of truth)
        private const string STRIKE_FIRST_DISPLAY = "<b>X</b>";    // 스트라이크 1구
        private const string STRIKE_SEC_DISPLAY   = "-";           // 스트라이크 2구 (굴리지 않음)
        private const string SPARE_SEC_DISPLAY    = "<b>/</b>";    // 스페어 2구
        private const string EMPTY_DISPLAY        = "";            // 클리어 시

        private void Start()
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

            Debug.Log("[Scoreboard] 초기화 완료 — FrameManager 이벤트 구독 시작");
        }

        private void OnDestroy()
        {
            if (frameManager != null)
            {
                frameManager.OnGameInitialized -= HandleGameInitialized;
                frameManager.OnFrameStarted    -= HandleFrameStarted;
                frameManager.OnThrowRecorded   -= HandleThrowRecorded;
                frameManager.OnFrameCompleted  -= HandleFrameCompleted;
                frameManager.OnGameOver        -= HandleGameOver;
            }
        }

        private bool Validate()
        {
            if (frameManager == null)
            { Debug.LogError("[Scoreboard] frameManager 참조 누락"); return false; }
            if (totalScoreText == null)
            { Debug.LogError("[Scoreboard] totalScoreText 참조 누락"); return false; }
            if (currentFrameText == null)
            { Debug.LogError("[Scoreboard] currentFrameText 참조 누락"); return false; }
            if (frameFirstText == null)
            { Debug.LogError("[Scoreboard] frameFirstText 참조 누락"); return false; }
            if (frameSecText == null)
            { Debug.LogError("[Scoreboard] frameSecText 참조 누락"); return false; }
            return true;
        }

        /// <summary>frame_N_first 표시 문자열 생성</summary>
        private string FormatFirstThrow(Frame frame)
        {
            if (frame.IsStrike())
                return STRIKE_FIRST_DISPLAY;
            return frame.Ball1.ToString();
        }

        /// <summary>frame_N_sec 표시 문자열 생성</summary>
        /// <remarks>
        /// 우선순위: Strike("-") > Spare("/") > 일반(Ball2 숫자)
        /// </remarks>
        private string FormatSecondThrow(Frame frame)
        {
            if (frame.IsStrike())
                return STRIKE_SEC_DISPLAY;
            if (frame.IsSpare())
                return SPARE_SEC_DISPLAY;
            return frame.Ball2.ToString();
        }

        /// <summary>frame_n 표시 (0-base → 1-base 변환)</summary>
        /// <remarks>AI_PROMPT_REFERENCE §6: 페이로드는 0-base, UI/로그는 1-base</remarks>
        private string FormatFrameNumber(int frameIndex)
        {
            return (frameIndex + 1).ToString();
        }

        /// <summary>total_score_n 표시</summary>
        private string FormatTotalScore(int score)
        {
            return score.ToString();
        }

        /// <summary>모든 UI 텍스트를 빈 문자열로 초기화</summary>
        private void ClearAllText()
        {
            totalScoreText.text   = EMPTY_DISPLAY;
            currentFrameText.text = EMPTY_DISPLAY;
            frameFirstText.text   = EMPTY_DISPLAY;
            frameSecText.text     = EMPTY_DISPLAY;
        }

        private void HandleGameInitialized()
        {
            ClearAllText();
            Debug.Log("[Scoreboard] 게임 초기화 — UI 클리어");
        }

        private void HandleFrameStarted(int frameIndex)
        {
            currentFrameText.text = FormatFrameNumber(frameIndex);
            frameFirstText.text   = EMPTY_DISPLAY;
            frameSecText.text     = EMPTY_DISPLAY;
            Debug.Log($"[Scoreboard] 프레임 {frameIndex + 1} 시작 — first/sec 클리어");
        }

        private void HandleThrowRecorded(int frameIndex, int throwNumber, Frame frame)
        {
            if (throwNumber == 1)
            {
                frameFirstText.text = FormatFirstThrow(frame);
            }
            else if (throwNumber == 2)
            {
                frameSecText.text = FormatSecondThrow(frame);
            }
            else
            {
                Debug.LogWarning($"[Scoreboard] 알 수 없는 throwNumber: {throwNumber}");
                return;
            }
            Debug.Log($"[Scoreboard] 프레임 {frameIndex + 1} / {throwNumber}구 표시 갱신");
        }

        private void HandleFrameCompleted(int frameIndex, Frame frame)
        {
            // 스트라이크 케이스: sec 에 "-" 명시
            // (HandleThrowRecorded 는 throwNumber 별로만 처리되므로 sec 가 빈 상태로 남음)
            if (frame.IsStrike())
            {
                frameSecText.text = STRIKE_SEC_DISPLAY;
            }

            // 총점 갱신 (프레임 완료 시점에만)
            int total = frameManager.GetTotalScore();
            totalScoreText.text = FormatTotalScore(total);

            Debug.Log($"[Scoreboard] 프레임 {frameIndex + 1} 완료 — 누적 총점 {total}");
        }

        private void HandleGameOver()
        {
            ClearAllText();
            Debug.Log("[Scoreboard] 게임 종료 — UI 클리어");
        }
    }
}
