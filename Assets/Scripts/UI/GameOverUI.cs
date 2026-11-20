using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace BowlingGame
{
    /// <summary>
    /// Gameover_scene 의 Canvas 에 부착되는 결과 화면 UI — <b>버튼만 담당</b>한다.
    /// 메인메뉴 버튼 / Quit 버튼 클릭 처리.
    /// </summary>
    /// <remarks>
    /// 2026-11-19 변경 (사용자 지정): 큰 총점 숫자 / "최고 점수" / "신기록!" 표시를 모두 제거했다.
    /// 결과 화면에는 <b>최종 스코어보드</b>만 뜨며 그건 <see cref="FinalScoreboardUI"/> 담당이다.
    /// 최고점/신기록 <b>수집</b>은 그대로다 — <c>HighScoreService.Record</c> 와
    /// <see cref="GameResultHolder.LastBestScore"/>/<see cref="GameResultHolder.IsNewRecord"/> 는 추후 리더보드용으로 살아 있다.
    /// </remarks>
    public class GameOverUI : MonoBehaviour
    {
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

            var holder = GameResultHolder.Instance;
            if (!holder.HasResult)
                Debug.LogWarning("[GameOverUI] GameResultHolder.HasResult == false — 결과 없이 직접 진입한 것으로 보임.");
            else
                Debug.Log($"[GameOverUI] 초기화 완료 — {holder.LastModeName} {holder.LastScore}점 (최고 {holder.LastBestScore}{(holder.IsNewRecord ? ", 신기록" : "")}) — 표시는 스코어보드가 담당");

#if UNITY_WEBGL && !UNITY_EDITOR
            // 브라우저에선 페이지를 게임이 닫을 수 없으므로 종료 버튼 숨김.
            quitButton.gameObject.SetActive(false);
#endif
        }

        private void OnDestroy()
        {
            if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
            if (quitButton     != null) quitButton.onClick.RemoveListener(OnQuitClicked);
        }

        private bool Validate()
        {
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
