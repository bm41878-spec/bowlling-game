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

        private void Start()      { /* 단계 2 */ }
        private void OnDestroy()  { /* 단계 2 */ }

        private void HandleGameInitialized() { /* 단계 4 */ }
        private void HandleGameOver()        { /* 단계 4 */ }

        private void OnRestartClicked()      { /* 단계 4 */ }
        private void OnMainMenuClicked()     { /* 단계 4 */ }
    }
}
