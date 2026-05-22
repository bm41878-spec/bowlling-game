using System;
using System.Collections;
using UnityEngine;

namespace BowlingGame
{
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        public GameState CurrentState { get; private set; }

        public event Action<GameState, GameState> OnStateChanged;

        private InputController inputController;

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
            inputController = InputController.Instance;
            // 첫 상태 전이는 GameManager 가 BeginGame() 에서 트리거한다 — 여기서는 Ready 상태로 대기.
        }

        public void ChangeState(GameState newState)
        {
            var previous = CurrentState;
            ExitState(previous);
            CurrentState = newState;
            OnStateChanged?.Invoke(previous, newState);
            EnterState(newState);
        }

        private void EnterState(GameState state)
        {
            switch (state)
            {
                case GameState.Ready:
                    Debug.Log("[State] Ready — 스페이스바를 눌러 시작");
                    if (inputController != null)
                        inputController.OnConfirmPressed += OnReadyConfirm;
                    break;
                case GameState.AimingPosition:
                    Debug.Log("[State] AimingPosition 시작");
                    break;
                case GameState.AimingPower:
                    Debug.Log("[State] AimingPower 시작");
                    break;
                case GameState.Rolling:
                    Debug.Log("[State] Rolling 시작");
                    break;
                case GameState.Scoring:
                    Debug.Log("[State] Scoring 시작");
                    StartCoroutine(TempScoringDelay());
                    break;
                case GameState.GameOver:
                    Debug.Log("[State] GameOver");
                    break;
            }
        }

        private void ExitState(GameState state)
        {
            Debug.Log("[State Exit] " + state);
            if (state == GameState.Ready && inputController != null)
                inputController.OnConfirmPressed -= OnReadyConfirm;
        }

        private void OnReadyConfirm()
        {
            ChangeState(GameState.AimingPosition);
        }

        private IEnumerator TempScoringDelay()
        {
            Debug.Log("[임시] Scoring 2초 후 다음 투구로 전환 (Phase 4에서 교체)");
            yield return new WaitForSeconds(2f);
            ChangeState(GameState.AimingPosition);
        }
    }
}
