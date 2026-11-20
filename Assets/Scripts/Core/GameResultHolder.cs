using System.Collections.Generic;
using UnityEngine;
using Bowling.Scoring;

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

        /// <summary>해당 모드의 최고 점수 (이번 게임 반영 후). 결과 없으면 0.</summary>
        public int LastBestScore { get; private set; }

        /// <summary>이번 게임이 해당 모드의 신기록이었는지.</summary>
        /// <remarks>
        /// 2026-11-19 부터 결과 화면에 <b>표시하지 않는다</b> (사용자 지정). 수집은 계속한다 — 추후 리더보드용.
        /// <see cref="HighScoreService.Record"/> 호출도 그대로 유지된다.
        /// </remarks>
        public bool IsNewRecord { get; private set; }

        /// <summary>
        /// 마지막 게임의 프레임별 결과 (0-base 순서). 결과 없으면 빈 목록.
        /// 결과 화면이 최종 스코어보드를 그리는 데 쓴다 — Gameover_scene 에는 FrameManager 가 없으므로
        /// 이벤트로 받을 수 없고 이 스냅샷이 유일한 출처다.
        /// </summary>
        public IReadOnlyList<Frame> LastFrames { get; private set; } = EmptyFrames;

        private static readonly Frame[] EmptyFrames = new Frame[0];

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

        /// <summary>게임 종료 시 GameManager 가 호출. 최고점/신기록 + 프레임별 결과까지 함께 전달.</summary>
        /// <param name="score">이번 게임 최종 점수.</param>
        /// <param name="modeName">모드명.</param>
        /// <param name="bestScore">해당 모드의 최고 점수 (이번 게임 반영 후).</param>
        /// <param name="isNewRecord">이번 게임이 신기록이었는지.</param>
        /// <param name="frames">프레임별 결과 (<see cref="FrameManager.GetFrames"/>). 목록만 복사하고 Frame 인스턴스는 공유한다.</param>
        public void SetResult(int score, string modeName, int bestScore, bool isNewRecord, IReadOnlyList<Frame> frames)
        {
            LastScore = score;
            LastModeName = modeName ?? string.Empty;
            LastBestScore = bestScore;
            IsNewRecord = isNewRecord;
            // 목록 자체는 복사 — FrameManager 가 재시작 때 같은 List 를 재사용하더라도 스냅샷이 흔들리지 않게.
            LastFrames = frames != null ? (IReadOnlyList<Frame>)new List<Frame>(frames) : EmptyFrames;
            HasResult = true;
            Debug.Log($"[ResultHolder] 결과 저장: {LastModeName} {LastScore}점 / {LastFrames.Count}프레임 (최고 {LastBestScore}{(IsNewRecord ? ", 신기록!" : "")})");
        }
    }
}
