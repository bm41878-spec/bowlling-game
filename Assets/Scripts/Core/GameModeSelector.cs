using UnityEngine;

namespace BowlingGame
{
    /// <summary>
    /// 메인메뉴에서 선택한 모드(BowlingRuleConfig)를 게임 씬으로 전달하는 싱글턴.
    /// DontDestroyOnLoad 로 씬 전이 사이 상태를 유지한다.
    /// </summary>
    /// <remarks>
    /// 사용 흐름:
    ///   MainMenuUI.OnShortClicked → GameModeSelector.Instance.SelectMode(shortRule)
    ///   → SceneManager.LoadScene("Game")
    ///   → GameManager.Start() 가 Instance.SelectedRule 을 우선 사용
    /// 인스펙터 ruleConfig 폴백 유지 — Game.unity 단독 Play 호환.
    /// SelectedRule 은 게임 종료 후 메인메뉴 복귀 시점에도 유지된다 (재선택 전까지).
    /// </remarks>
    public class GameModeSelector : MonoBehaviour
    {
        public static GameModeSelector Instance { get; private set; }

        public BowlingRuleConfig SelectedRule { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>선택한 모드를 캐시. 실제 씬 전이는 호출자(MainMenuUI) 가 SceneManager.LoadScene 으로 수행.</summary>
        public void SelectMode(BowlingRuleConfig rule)
        {
            if (rule == null)
            {
                Debug.LogError("[ModeSelector] SelectMode 호출 시 rule == null — 무시");
                return;
            }
            SelectedRule = rule;
            Debug.Log($"[ModeSelector] 모드 선택: {rule.ModeName} ({rule.FrameCount}프레임)");
        }
    }
}
