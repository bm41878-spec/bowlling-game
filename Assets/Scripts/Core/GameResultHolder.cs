using UnityEngine;

namespace BowlingGame
{
    /// <summary>
    /// 게임 종료 시점의 결과 (최종 점수 + 모드) 를 다음 씬 (Gameover_scene) 으로 전달하는 싱글턴.
    /// DontDestroyOnLoad 로 씬 전이 사이 상태를 유지한다. <see cref="GameModeSelector"/> 와 동일 패턴.
    /// </summary>
    /// <remarks>
    /// 사용 흐름:
    ///   GameManager.OnEnterGameOver → GameResultHolder.Instance.SetResult(score, modeName)
    ///   → SceneManager.LoadScene("Gameover_scene")
    ///   → GameOverUI.Start() 가 Instance.LastScore 를 읽어 표시
    /// 인스턴스가 씬에 없으면 첫 접근 시 자동 생성된다 (Gameover_scene 단독 Play 대비).
    /// </remarks>
    public class GameResultHolder : MonoBehaviour
    {
        private static GameResultHolder instance;

        /// <summary>지연 생성 싱글턴. 씬 어디에도 없으면 자동 GameObject 생성.</summary>
        public static GameResultHolder Instance
        {
            get
            {
                if (instance != null) return instance;
                var existing = FindFirstObjectByType<GameResultHolder>();
                if (existing != null) { instance = existing; return instance; }
                var go = new GameObject("GameResultHolder");
                instance = go.AddComponent<GameResultHolder>();
                return instance;
            }
        }

        /// <summary>마지막 게임의 최종 점수. 결과 없으면 0.</summary>
        public int LastScore { get; private set; }

        /// <summary>마지막 게임의 모드명 (예: "쇼트 모드"). 결과 없으면 빈 문자열.</summary>
        public string LastModeName { get; private set; } = string.Empty;

        /// <summary>결과가 한 번이라도 기록되었는지 (Gameover_scene 직접 Play 검증용).</summary>
        public bool HasResult { get; private set; }

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        /// <summary>게임 종료 시 GameManager 가 호출.</summary>
        public void SetResult(int score, string modeName)
        {
            LastScore = score;
            LastModeName = modeName ?? string.Empty;
            HasResult = true;
            Debug.Log($"[ResultHolder] 결과 저장: {LastModeName} {LastScore}점");
        }
    }
}
