using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Bowling.Scoring;

namespace BowlingGame
{
    /// <summary>
    /// 게임 종료 결과 화면 UI. FrameManager OnGameOver 발생 시 패널을 표시하고
    /// 최종 점수 / 스트라이크 횟수 / 스페어 처리 횟수를 갱신한다.
    /// </summary>
    /// <remarks>
    /// 동작:
    ///   OnGameInitialized → 패널 숨김 (게임 시작/재시작 시점)
    ///   OnGameOver        → 패널 표시 + 3개 수치 갱신
    ///   재시작 버튼       → GameManager.Instance.RestartGame()
    ///   메인메뉴 버튼     → SceneManager.LoadScene("mainmenu")
    /// 표시 규칙은 본 클래스에 캡슐화. FrameManager 는 표시 규칙을 모름.
    /// </remarks>
    public class ResultUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("GameManager 가 들고 있는 동일 FrameManager 인스턴스. ScoreboardUI 와 같은 인스턴스여야 함.")]
        private FrameManager frameManager;

        [Header("Panel")]
        [SerializeField, Tooltip("결과 패널 컨테이너. 본 컴포넌트가 붙은 오브젝트와는 별도 — 본 컴포넌트는 항상 활성 유지, panelRoot 만 SetActive 토글.")]
        private GameObject panelRoot;

        [Header("Text Targets")]
        [SerializeField, Tooltip("Canvas > ResultPanel > final_score")]
        private TMP_Text finalScoreText;

        [SerializeField, Tooltip("Canvas > ResultPanel > strike_count")]
        private TMP_Text strikeCountText;

        [SerializeField, Tooltip("Canvas > ResultPanel > spare_count")]
        private TMP_Text spareCountText;

        [Header("Buttons")]
        [SerializeField, Tooltip("재시작 — GameManager.Instance.RestartGame()")]
        private Button restartButton;

        [SerializeField, Tooltip("메인메뉴 — SceneManager.LoadScene(\"mainmenu\")")]
        private Button mainMenuButton;

        // 표시 규칙 단일 출처(single source of truth)
        private const string COUNT_FORMAT          = "{0}회";      // 스트라이크/스페어 횟수 포맷
        private const string MAIN_MENU_SCENE_NAME  = "mainmenu";    // 메인메뉴 씬 이름 (Build Settings index 0)

        private void Start()
        {
            if (!Validate())
            {
                enabled = false;
                return;
            }

            frameManager.OnGameInitialized += HandleGameInitialized;
            frameManager.OnGameOver        += HandleGameOver;

            restartButton.onClick.AddListener(OnRestartClicked);
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);

            // 초기 상태: 패널 숨김
            HidePanel();

            Debug.Log("[Result] 초기화 완료 — FrameManager 이벤트 + 버튼 콜백 구독");
        }

        private void OnDestroy()
        {
            if (frameManager != null)
            {
                frameManager.OnGameInitialized -= HandleGameInitialized;
                frameManager.OnGameOver        -= HandleGameOver;
            }
            if (restartButton  != null) restartButton.onClick.RemoveListener(OnRestartClicked);
            if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
        }

        private bool Validate()
        {
            if (frameManager == null)
            { Debug.LogError("[Result] frameManager 참조 누락"); return false; }
            if (panelRoot == null)
            { Debug.LogError("[Result] panelRoot 참조 누락"); return false; }
            if (finalScoreText == null)
            { Debug.LogError("[Result] finalScoreText 참조 누락"); return false; }
            if (strikeCountText == null)
            { Debug.LogError("[Result] strikeCountText 참조 누락"); return false; }
            if (spareCountText == null)
            { Debug.LogError("[Result] spareCountText 참조 누락"); return false; }
            if (restartButton == null)
            { Debug.LogError("[Result] restartButton 참조 누락"); return false; }
            if (mainMenuButton == null)
            { Debug.LogError("[Result] mainMenuButton 참조 누락"); return false; }
            return true;
        }

        /// <summary>모든 프레임 중 스트라이크 횟수.</summary>
        private int CountStrikes()
        {
            int count = 0;
            int frameCount = frameManager.GetFrameCount();
            for (int i = 0; i < frameCount; i++)
            {
                if (frameManager.GetFrame(i).IsStrike()) count++;
            }
            return count;
        }

        /// <summary>모든 프레임 중 스페어 처리 횟수 (0/10 스페어 포함, <see cref="Frame.IsSpare"/> 기준).</summary>
        private int CountSpares()
        {
            int count = 0;
            int frameCount = frameManager.GetFrameCount();
            for (int i = 0; i < frameCount; i++)
            {
                if (frameManager.GetFrame(i).IsSpare()) count++;
            }
            return count;
        }

        /// <summary>"N회" 형식 문자열 생성.</summary>
        private string FormatCount(int count) => string.Format(COUNT_FORMAT, count);

        /// <summary>결과 패널 표시.</summary>
        private void ShowPanel() => panelRoot.SetActive(true);

        /// <summary>결과 패널 숨김.</summary>
        private void HidePanel() => panelRoot.SetActive(false);

        private void HandleGameInitialized()
        {
            HidePanel();
            Debug.Log("[Result] 게임 초기화 — 패널 숨김");
        }

        private void HandleGameOver()
        {
            int finalScore = frameManager.GetTotalScore();
            int strikes    = CountStrikes();
            int spares     = CountSpares();

            finalScoreText.text  = finalScore.ToString();
            strikeCountText.text = FormatCount(strikes);
            spareCountText.text  = FormatCount(spares);

            ShowPanel();
            Debug.Log($"[Result] 게임 종료 — 패널 표시 (점수 {finalScore}, 스트라이크 {strikes}회, 스페어 {spares}회)");
        }

        private void OnRestartClicked()
        {
            Debug.Log("[Result] 재시작 버튼 클릭 — GameManager.RestartGame() 호출");
            GameManager.Instance.RestartGame();
        }

        private void OnMainMenuClicked()
        {
            Debug.Log($"[Result] 메인메뉴 버튼 클릭 — SceneManager.LoadScene(\"{MAIN_MENU_SCENE_NAME}\")");
            SceneManager.LoadScene(MAIN_MENU_SCENE_NAME);
        }
    }
}
