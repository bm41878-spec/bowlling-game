using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Bowling.Scoring;

namespace BowlingGame
{
    /// <summary>
    /// 점수판의 한 프레임 카드 — frame 번호 / 1·2구 결과 / 누적 점수 표시.
    /// CardLayoutRenderer 가 prefab 으로 N개 인스턴스화하여 사용.
    /// </summary>
    public class FrameCardUI : MonoBehaviour
    {
        // 표시 규칙 상수 (단일 출처 — 도메인에 표시 문자열 도입 금지 패턴).
        public const string STRIKE_FIRST = "X";
        public const string STRIKE_SEC   = "-";
        public const string SPARE_SEC    = "/";
        public const string EMPTY        = "";

        [Header("표시 요소")]
        [SerializeField] private TMP_Text frameNumberLabel;
        [SerializeField] private TMP_Text throwsLabel;     // "X -" / "7 /" / "5 4"
        [SerializeField] private TMP_Text scoreLabel;      // 누적 점수 "15", "28"
        [SerializeField] private Image background;
        [SerializeField] private Image highlight;          // 활성 시 노출되는 강조 테두리/오버레이 (옵션, 없어도 됨)

        [Header("색상")]
        [SerializeField] private Color normalBgColor   = new Color(0.12f, 0.12f, 0.15f, 1f);
        [SerializeField] private Color activeBgColor   = new Color(0.20f, 0.30f, 0.45f, 1f);
        [SerializeField] private Color gameOverBgColor = new Color(0.18f, 0.18f, 0.22f, 1f);

        // ---------- 공개 API ----------

        public void SetFrameNumber(int oneBaseFrameNumber)
        {
            if (frameNumberLabel != null) frameNumberLabel.text = oneBaseFrameNumber.ToString();
        }

        /// <summary>1·2구 결과 텍스트 갱신. throwNumber: 1 또는 2 — 어디까지 굴렸는지 결정.</summary>
        public void SetThrows(Frame frame, int throwNumber)
        {
            if (throwsLabel == null) return;
            throwsLabel.text = FormatThrows(frame, throwNumber);
        }

        public void SetCumulativeScore(int score)
        {
            if (scoreLabel != null) scoreLabel.text = score.ToString();
        }

        public void SetActive(bool isActive)
        {
            if (background != null)
                background.color = isActive ? activeBgColor : normalBgColor;
            if (highlight != null)
                highlight.gameObject.SetActive(isActive);
        }

        public void SetGameOver()
        {
            if (background != null) background.color = gameOverBgColor;
            if (highlight != null) highlight.gameObject.SetActive(false);
        }

        public void Clear()
        {
            if (throwsLabel != null) throwsLabel.text = EMPTY;
            if (scoreLabel != null)  scoreLabel.text  = EMPTY;
            if (background != null)  background.color = normalBgColor;
            if (highlight != null)   highlight.gameObject.SetActive(false);
        }

        // ---------- 헬퍼 (순수 함수) ----------

        /// <summary>throwNumber 까지 굴린 결과를 단일 문자열로 — "X -" / "7 /" / "5 4" / "7 ?".</summary>
        public static string FormatThrows(Frame frame, int throwNumber)
        {
            if (frame == null) return EMPTY;

            string first  = FormatFirstThrow(frame, throwNumber);
            string second = FormatSecondThrow(frame, throwNumber);
            return $"{first}  {second}";
        }

        private static string FormatFirstThrow(Frame frame, int throwNumber)
        {
            if (throwNumber < 1) return EMPTY;
            if (frame.IsStrike()) return STRIKE_FIRST;
            return frame.Ball1.ToString();
        }

        private static string FormatSecondThrow(Frame frame, int throwNumber)
        {
            // 스트라이크 시 2구 미실행 — 1구 직후 즉시 "-" 표시
            if (frame.IsStrike()) return STRIKE_SEC;
            // 아직 2구 안 던졌으면 빈 칸
            if (throwNumber < 2) return EMPTY;
            if (frame.IsSpare()) return SPARE_SEC;
            return frame.Ball2.ToString();
        }
    }
}
