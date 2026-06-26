using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Bowling.Scoring;

namespace BowlingGame
{
    /// <summary>
    /// 게임 흐름의 최상위 조정자.
    /// 상태 머신은 <see cref="GameStateManager"/> 에 위임하고, 본 클래스는 각 상태 진입 시
    /// 도메인/물리 컴포넌트(FrameManager, PinManager, PhysicsSettleDetector, ThrowTransitionController)
    /// 에 적절한 명령을 보내고 결과 이벤트를 받아 다음 전이를 결정한다.
    /// </summary>
    /// <remarks>
    /// [DefaultExecutionOrder(1000)] 로 다른 컴포넌트의 Start 완료 이후 실행되어
    /// OnStateChanged 구독 누락 없이 첫 상태 전이를 트리거할 수 있도록 한다.
    ///
    /// 순서 보장 체크리스트:
    /// <list type="number">
    ///   <item>BeginGame 시: ResetAllPins → ResetToStartPosition → <b>SnapshotBeforeThrow</b> → ChangeState(AimingPosition)</item>
    ///   <item>Rolling 진입 시: SettleDetector.StartDetection</item>
    ///   <item>OnSettled → ChangeState(Scoring)</item>
    ///   <item>Scoring 진입 시: TransitionController.HandlePostThrow 내부에서
    ///         GetNewlyFallenCount → RecordThrow → 분기(핀/공 리셋) → 다음 투구가 있으면 SnapshotBeforeThrow</item>
    ///   <item>OnTransitionComplete → ChangeState(AimingPosition or GameOver)</item>
    /// </list>
    /// </remarks>
    [DefaultExecutionOrder(1000)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Config")]
        [SerializeField] private BowlingRuleConfig ruleConfig;

        [Header("Components")]
        [SerializeField] private PinManager pinManager;
        [SerializeField] private PhysicsSettleDetector settleDetector;
        [SerializeField] private ThrowTransitionController transitionController;
        [Tooltip("BallController 분리 전 임시 — BowlingBall 직접 참조.")]
        [SerializeField] private BowlingBall ball;
        [Tooltip("씬에 배치된 FrameManager 호스트 GameObject. Initialize() 로 게임 시작 시 주입.")]
        [SerializeField] private FrameManager frameManager;

        private GameStateManager stateManager;

        public FrameManager FrameManager => frameManager;
        public ThrowTransitionController TransitionController => transitionController;
        public GameState CurrentState =>
            stateManager != null ? stateManager.CurrentState : GameState.Ready;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Start()
        {
            // GameModeSelector 가 있으면 인스펙터 ruleConfig 를 선택값으로 덮어쓴다.
            // (없는 경우 = Game.unity 단독 Play — 인스펙터 폴백 그대로 사용)
            var selector = GameModeSelector.Instance;
            if (selector != null && selector.SelectedRule != null)
            {
                ruleConfig = selector.SelectedRule;
                Debug.Log($"[GameManager] ModeSelector 로부터 룰 주입: {ruleConfig.ModeName}");
            }

            if (!ValidateDependencies()) return;

            stateManager = GameStateManager.Instance;
            if (stateManager == null)
            {
                Debug.LogError("[GameManager] GameStateManager.Instance 가 null — 씬에 GameStateManager 추가 필요");
                return;
            }

            // 1) 도메인 초기화 + TransitionController 에 주입
            frameManager.Initialize(ruleConfig);
            transitionController.Initialize(frameManager);

            // 2) 이벤트 배선 (구독 → 트리거 순서)
            stateManager.OnStateChanged                 += OnGameStateChanged;
            settleDetector.OnSettled                    += OnPhysicsSettled;
            transitionController.OnTransitionComplete   += OnTransitionComplete;

            // 3) 게임 시작 (Ready → AimingPosition)
            BeginGame();
        }

        void OnDestroy()
        {
            if (stateManager           != null) stateManager.OnStateChanged               -= OnGameStateChanged;
            if (settleDetector         != null) settleDetector.OnSettled                  -= OnPhysicsSettled;
            if (transitionController   != null) transitionController.OnTransitionComplete -= OnTransitionComplete;
            if (Instance == this) Instance = null;
        }

        // ---------- 게임 시작 / 재시작 ----------

        /// <summary>Ready → AimingPosition: 첫 프레임 준비를 명세 순서대로 수행한다.</summary>
        private void BeginGame()
        {
            Debug.Log($"[GameManager] BeginGame — 모드: {ruleConfig.ModeName}, 총 {ruleConfig.FrameCount} 프레임");

            pinManager.ResetAllPins();
            ball.ResetToStartPosition();
            pinManager.SnapshotBeforeThrow();        // 첫 투구 직전 스냅샷

            stateManager.ChangeState(GameState.AimingPosition);
        }

        /// <summary>
        /// 진행 중인 게임을 처음 상태로 일관되게 되돌린다.
        /// 디버그 단축키(R) / 메뉴 재시작 등에서 호출.
        /// 상태 머신, 도메인(FrameManager), 물리(공·핀), 스냅샷을 한 번에 초기화한다.
        /// </summary>
        public void RestartGame()
        {
            Debug.Log("[GameManager] RestartGame — 진행 상태 전체 초기화");

            // 진행 중인 정지 감지가 있으면 중단 (Rolling 도중 호출된 경우)
            settleDetector.StopDetection();

            // 도메인 상태 리셋 — 같은 FrameManager 인스턴스에 Initialize 재호출.
            // TransitionController 는 인스턴스 참조를 그대로 유지하므로 재주입 불필요.
            frameManager.Initialize(ruleConfig);

            // 물리/스냅샷/상태는 BeginGame 과 동일 시퀀스로 처리
            BeginGame();
        }

        // ---------- 상태 전이 핸들러 ----------

        private void OnGameStateChanged(GameState prev, GameState next)
        {
            Debug.Log($"[GameManager] 상태 전이: {prev} → {next}");

            switch (next)
            {
                case GameState.AimingPosition: OnEnterAimingPosition(); break;
                case GameState.AimingPower:    OnEnterAimingPower();    break;
                case GameState.AimingSpin:     OnEnterAimingSpin();     break;
                case GameState.Rolling:        OnEnterRolling();        break;
                case GameState.Scoring:        OnEnterScoring();        break;
                case GameState.GameOver:       OnEnterGameOver();       break;
                case GameState.Ready:                                    break;
            }
        }

        private void OnEnterAimingPosition()
        {
            // BallAimer 가 OnStateChanged 구독으로 본 상태 진입을 처리.
            // 공/핀 리셋은 ThrowTransitionController 가 이미 수행하고 SnapshotBeforeThrow 도 완료된 상태.
            // (첫 진입은 BeginGame 에서 동일 사전 작업 완료)
        }

        private void OnEnterAimingPower()
        {
            // PowerGaugeUI 가 OnStateChanged 구독으로 본 상태 진입을 처리.
        }

        private void OnEnterAimingSpin()
        {
            // SpinGaugeUI 가 OnStateChanged 구독으로 본 상태 진입을 처리.
        }

        private void OnEnterRolling()
        {
            // BowlingBall.HandleStateChanged 가 ExecuteLaunch 로 힘 적용.
            // SettleDetector 시작 — 정지 감지 시 OnSettled 콜백.
            settleDetector.StartDetection();
        }

        private void OnEnterScoring()
        {
            // 후처리: 카운트 → 기록 → 분기 → 핀/공 리셋 → 다음 투구 SnapshotBeforeThrow.
            // 결과는 OnTransitionComplete 로 통지.
            transitionController.HandlePostThrow();
        }

        private const string GAMEOVER_SCENE_NAME = "Gameover_scene";

        private void OnEnterGameOver()
        {
            settleDetector.StopDetection();
            int score   = frameManager.GetTotalScore();
            int perfect = ruleConfig.GetPerfectScore();
            Debug.Log($"[GameOver] 게임 종료! 모드: {ruleConfig.ModeName}, 최종 점수: {score}점, 퍼펙트 대비: {score}/{perfect}");

            // 최고 점수 기록 (Phase 8 — JSON 영속화). 실패해도 게임 흐름은 막지 않음 (SaveSystem fail-safe).
            var record = HighScoreService.Record(ruleConfig.ModeName, ruleConfig.FrameCount, score);

            // 결과를 다음 씬으로 전달 + Gameover_scene 로드.
            GameResultHolder.Instance.SetResult(score, ruleConfig.ModeName, record.BestScore, record.IsNewRecord);
            SceneManager.LoadScene(GAMEOVER_SCENE_NAME);
        }

        // ---------- 외부 이벤트 핸들러 ----------

        private void OnPhysicsSettled()
        {
            if (stateManager.CurrentState != GameState.Rolling)
            {
                Debug.LogWarning($"[GameManager] OnSettled 발생했으나 현재 상태가 Rolling 이 아님: {stateManager.CurrentState} — 무시");
                return;
            }
            stateManager.ChangeState(GameState.Scoring);
        }

        private void OnTransitionComplete(TransitionResult result)
        {
            switch (result)
            {
                case TransitionResult.ProceedToBall2:
                case TransitionResult.ProceedToNextFrame:
                    stateManager.ChangeState(GameState.AimingPosition);
                    break;
                case TransitionResult.GameOver:
                    stateManager.ChangeState(GameState.GameOver);
                    break;
            }
        }

        // ---------- helpers ----------

        private bool ValidateDependencies()
        {
            bool ok = true;
            if (ruleConfig            == null) { Debug.LogError("[GameManager] ruleConfig 미할당");            ok = false; }
            if (pinManager            == null) { Debug.LogError("[GameManager] pinManager 미할당");            ok = false; }
            if (settleDetector        == null) { Debug.LogError("[GameManager] settleDetector 미할당");        ok = false; }
            if (transitionController  == null) { Debug.LogError("[GameManager] transitionController 미할당");  ok = false; }
            if (ball                  == null) { Debug.LogError("[GameManager] ball 미할당");                  ok = false; }
            if (frameManager          == null) { Debug.LogError("[GameManager] frameManager 미할당");          ok = false; }
            return ok;
        }
    }
}
