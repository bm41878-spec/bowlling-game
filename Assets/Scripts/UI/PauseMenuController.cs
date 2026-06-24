using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

namespace BowlingGame
{
    /// <summary>
    /// 게임 진행 중 ESC 키로 설정 화면을 띄우는 일시정지 컨트롤러.
    /// settings 씬을 Additive 로 올리고 <see cref="Time.timeScale"/> 을 0 으로 멈춘다.
    /// ESC 를 다시 누르면 설정 씬을 내리고 게임을 재개(toggle)한다.
    /// </summary>
    /// <remarks>
    /// settings 씬은 자체 Camera / AudioListener / EventSystem 을 갖고 있어 Additive 로드 시
    /// Game 씬의 동일 컴포넌트와 중복된다. 로드 직후 settings 쪽 Camera(AudioListener 동반) 와
    /// EventSystem 을 비활성화하고, Game 씬의 EventSystem 하나가 설정 UI 입력까지 처리하도록 한다.
    /// (Screen Space - Overlay Canvas 는 카메라 없이 렌더되므로 카메라 비활성화에도 정상 표시.)
    ///
    /// 이 컴포넌트는 DontDestroyOnLoad 객체에 올리지 말 것 — Game 씬 전용으로 두어야
    /// 메인메뉴/설정 단독 진입 시 ESC 토글이 끼어들지 않는다. Game 씬이 내려갈 때
    /// <see cref="OnDestroy"/> 가 timeScale 을 1 로 복구한다(설정의 "메인메뉴로" 경로 보호).
    /// </remarks>
    public class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private string settingsSceneName = "settings";

        private bool isOpen;

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (!kb.escapeKey.wasPressedThisFrame) return;

            // 리바인딩 진행 중이면 ESC 는 리바인딩 취소 용도 — 일시정지 토글을 양보한다.
            if (isOpen && SettingsUI.IsRebindActive) return;

            if (isOpen) Close();
            else Open();
        }

        private void Open()
        {
            if (isOpen) return;
            isOpen = true;
            Time.timeScale = 0f;
            // 설정의 "뒤로가기" 버튼이 메인메뉴 대신 게임 재개로 동작하도록 핸들러 주입.
            SettingsUI.PauseResumeHandler = Close;
            SceneManager.sceneLoaded += OnSettingsLoaded;
            SceneManager.LoadScene(settingsSceneName, LoadSceneMode.Additive);
        }

        private void OnSettingsLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != settingsSceneName) return;
            SceneManager.sceneLoaded -= OnSettingsLoaded;

            // 중복 Camera(+AudioListener) / EventSystem 비활성화 — Game 씬의 것만 남긴다.
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var cam in root.GetComponentsInChildren<Camera>(true))
                    cam.gameObject.SetActive(false);
                foreach (var es in root.GetComponentsInChildren<EventSystem>(true))
                    es.gameObject.SetActive(false);
            }
        }

        private void Close()
        {
            if (!isOpen) return;
            isOpen = false;
            Time.timeScale = 1f;
            SettingsUI.PauseResumeHandler = null;

            var scene = SceneManager.GetSceneByName(settingsSceneName);
            if (scene.IsValid() && scene.isLoaded)
                SceneManager.UnloadSceneAsync(scene);
        }

        void OnDestroy()
        {
            // 설정의 "메인메뉴로" 버튼 등으로 Game 씬이 단독 로드에 의해 내려갈 때
            // timeScale 이 0 으로 남아 다음 씬이 얼어붙는 것을 방지.
            SceneManager.sceneLoaded -= OnSettingsLoaded;
            if (isOpen)
            {
                Time.timeScale = 1f;
                SettingsUI.PauseResumeHandler = null;
            }
        }
    }
}
