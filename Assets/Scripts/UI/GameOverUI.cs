using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace BowlingGame
{
    /// <summary>
    /// Gameover_scene 의 Canvas 에 부착되는 결과 화면 UI.
    /// <see cref="GameResultHolder"/> 의 마지막 점수를 읽어 <c>gameover_score</c> TMP_Text 에 표시하고
    /// 메인메뉴 버튼 / Quit 버튼 클릭을 처리한다.
    /// </summary>
    /// <remarks>
    /// ResultUI (Game.unity 오버레이 패널) 와의 구조 차이:
    ///   - 이쪽은 별도 씬. 점수는 DontDestroyOnLoad 싱글턴 (<see cref="GameResultHolder"/>) 으로 전달
    ///   - FrameManager 의존 없음 — 씬이 갈리면서 FrameManager 인스턴스도 사라지므로 사후 조회 불가
    /// 표시 규칙은 본 클래스에 캡슐화.
    /// </remarks>
    public class GameOverUI : MonoBehaviour
    {
        [Header("Text Targets")]
        [SerializeField, Tooltip("Canvas > gameover_score — 최종 점수 표시")]
        private TMP_Text gameOverScoreText;

        [Header("Buttons")]
        [SerializeField, Tooltip("메인메뉴 — SceneManager.LoadScene(\"mainmenu\")")]
        private Button mainMenuButton;

        [SerializeField, Tooltip("종료 — Application.Quit() (Editor 에서는 Play 모드 종료)")]
        private Button quitButton;

        private const string MAIN_MENU_SCENE_NAME = "mainmenu";

        private void Start()
        {
            if (!Validate())
            {
                enabled = false;
                return;
            }

            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            quitButton.onClick.AddListener(OnQuitClicked);

            // 마지막 게임의 점수 표시.
            var holder = GameResultHolder.Instance;
            if (!holder.HasResult)
            {
                // Gameover_scene 단독 Play / 결과 없는 직접 진입 — 기본 0 으로 표시 + 경고.
                Debug.LogWarning("[GameOverUI] GameResultHolder.HasResult == false — 결과 없이 직접 진입한 것으로 보임. 점수 0 표시.");
                gameOverScoreText.text = "0";
            }
            else
            {
                gameOverScoreText.text = holder.LastScore.ToString();
                Debug.Log($"[GameOverUI] 초기화 완료 — 점수 {holder.LastScore} 표시 (모드: {holder.LastModeName})");
            }
        }

        private void OnDestroy()
        {
            if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
            if (quitButton     != null) quitButton.onClick.RemoveListener(OnQuitClicked);
        }

        private bool Validate()
        {
            if (gameOverScoreText == null)
            { Debug.LogError("[GameOverUI] gameOverScoreText 참조 누락"); return false; }
            if (mainMenuButton == null)
            { Debug.LogError("[GameOverUI] mainMenuButton 참조 누락"); return false; }
            if (quitButton == null)
            { Debug.LogError("[GameOverUI] quitButton 참조 누락"); return false; }
            return true;
        }

        private void OnMainMenuClicked()
        {
            Debug.Log($"[GameOverUI] 메인메뉴 버튼 클릭 — SceneManager.LoadScene(\"{MAIN_MENU_SCENE_NAME}\")");
            SceneManager.LoadScene(MAIN_MENU_SCENE_NAME);
        }

        private void OnQuitClicked()
        {
            Debug.Log("[GameOverUI] Quit 버튼 클릭 — 애플리케이션 종료");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
