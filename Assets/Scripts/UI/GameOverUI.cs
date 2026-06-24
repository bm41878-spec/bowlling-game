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

        [SerializeField, Tooltip("Canvas > best_score — 해당 모드 최고 점수 (선택). 미할당 시 표시 생략")]
        private TMP_Text bestScoreText;

        [SerializeField, Tooltip("Canvas > new_record — 신기록 강조 (선택). 신기록 아닐 때 비활성")]
        private TMP_Text newRecordText;

        [Header("Buttons")]
        [SerializeField, Tooltip("메인메뉴 — SceneManager.LoadScene(\"mainmenu\")")]
        private Button mainMenuButton;

        [SerializeField, Tooltip("종료 — Application.Quit() (Editor 에서는 Play 모드 종료)")]
        private Button quitButton;

        private const string MAIN_MENU_SCENE_NAME = "mainmenu";
        private const string BEST_FORMAT          = "최고 점수: {0}";   // 최고 점수 표시 포맷 (단일 출처)
        private const string NEW_RECORD_TEXT      = "신기록!";          // 신기록 강조 문구

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

            ApplyBestScore(holder);

#if UNITY_WEBGL && !UNITY_EDITOR
            // 브라우저에선 페이지를 게임이 닫을 수 없으므로 종료 버튼 숨김.
            quitButton.gameObject.SetActive(false);
#endif
        }

        /// <summary>최고 점수 + 신기록 강조 표시 (선택 필드, 미할당 시 생략).</summary>
        private void ApplyBestScore(GameResultHolder holder)
        {
            if (bestScoreText != null)
                bestScoreText.text = string.Format(BEST_FORMAT, holder.LastBestScore);

            if (newRecordText != null)
            {
                newRecordText.text = NEW_RECORD_TEXT;
                newRecordText.gameObject.SetActive(holder.IsNewRecord);
            }

            if (holder.IsNewRecord)
                Debug.Log($"[GameOverUI] 신기록! 최고 점수 {holder.LastBestScore} (모드: {holder.LastModeName})");
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
#elif UNITY_WEBGL
            Debug.LogWarning("[GameOverUI] WebGL 빌드에선 Application.Quit 호출 불가 — 버튼은 숨겨져 있어야 함");
#else
            Application.Quit();
#endif
        }
    }
}
