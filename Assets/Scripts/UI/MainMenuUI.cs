using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace BowlingGame
{
    /// <summary>
    /// 메인메뉴 화면 컨트롤러. 쇼트/풀 모드 버튼 두 개를 처리한다.
    /// 버튼 클릭 → GameModeSelector 에 모드 캐시 → 게임 씬으로 전이.
    /// </summary>
    /// <remarks>
    /// 동작:
    ///   쇼트 모드 버튼 → SelectMode(shortModeRule) → SceneManager.LoadScene(gameSceneName)
    ///   풀  모드 버튼 → SelectMode(fullModeRule)  → SceneManager.LoadScene(gameSceneName)
    /// 모드 전달은 본 컴포넌트와 무관하게 GameModeSelector(DontDestroyOnLoad) 가 담당.
    /// gameSceneName 은 Build Settings 에 등록된 씬 이름이어야 한다.
    /// </remarks>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Rule Configs")]
        [SerializeField, Tooltip("Assets/Configs/ShortModeRule.asset")]
        private BowlingRuleConfig shortModeRule;

        [SerializeField, Tooltip("Assets/Configs/FullModeRule.asset")]
        private BowlingRuleConfig fullModeRule;

        [Header("Buttons")]
        [SerializeField, Tooltip("쇼트 모드 시작 버튼")]
        private Button shortButton;

        [SerializeField, Tooltip("풀 모드 시작 버튼")]
        private Button fullButton;

        [Header("Scene")]
        [SerializeField, Tooltip("게임 씬 이름. Build Settings 에 등록되어 있어야 함.")]
        private string gameSceneName = "Game";

        private void Start()
        {
            if (!Validate())
            {
                enabled = false;
                return;
            }

            shortButton.onClick.AddListener(OnShortClicked);
            fullButton.onClick.AddListener(OnFullClicked);

            Debug.Log("[MainMenu] 초기화 완료 — 버튼 콜백 등록");
        }

        private void OnDestroy()
        {
            if (shortButton != null) shortButton.onClick.RemoveListener(OnShortClicked);
            if (fullButton  != null) fullButton.onClick.RemoveListener(OnFullClicked);
        }

        private bool Validate()
        {
            if (shortModeRule == null) { Debug.LogError("[MainMenu] shortModeRule 미할당"); return false; }
            if (fullModeRule  == null) { Debug.LogError("[MainMenu] fullModeRule 미할당");  return false; }
            if (shortButton   == null) { Debug.LogError("[MainMenu] shortButton 미할당");   return false; }
            if (fullButton    == null) { Debug.LogError("[MainMenu] fullButton 미할당");    return false; }
            if (string.IsNullOrEmpty(gameSceneName))
            { Debug.LogError("[MainMenu] gameSceneName 비어있음"); return false; }
            return true;
        }

        private void OnShortClicked() => StartGameWith(shortModeRule);
        private void OnFullClicked()  => StartGameWith(fullModeRule);

        private void StartGameWith(BowlingRuleConfig rule)
        {
            var selector = GameModeSelector.Instance;
            if (selector == null)
            {
                Debug.LogError("[MainMenu] GameModeSelector.Instance 가 null — 씬에 GameModeSelector 추가 필요");
                return;
            }

            selector.SelectMode(rule);
            Debug.Log($"[MainMenu] 씬 전이 → {gameSceneName} (모드: {rule.ModeName})");
            SceneManager.LoadScene(gameSceneName);
        }
    }
}
