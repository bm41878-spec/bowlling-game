// TODO: Phase 5 — BowlingRuleConfig 주입 구조 완성 후 상태 전이 구현
using System.Collections.Generic;
using UnityEngine;

namespace BowlingGame
{
    public class GameManager : MonoBehaviour
    {
        public BowlingRuleConfig ruleConfig;

        private GameState currentState;
        private int currentFrame;
        private List<Frame> frames;
        private ScoreCalculator scoreCalculator;

        private void Start()
        {
            // TODO: frames 초기화, scoreCalculator 생성, Ready 상태 진입
            throw new System.NotImplementedException();
        }

        public void OnThrowComplete()
        {
            if (IsFrameComplete())
            {
                if (currentFrame >= ruleConfig.frameCount - 1)
                    TransitionTo(GameState.GameOver);
                else
                {
                    currentFrame++;
                    TransitionTo(GameState.AimingPosition);
                }
            }
            else
            {
                TransitionTo(GameState.AimingPower);
            }
        }

        private bool IsFrameComplete()
        {
            // TODO: 현재 프레임의 투구 완료 여부 판정 (스트라이크, 2구, 마지막 프레임 3구)
            throw new System.NotImplementedException();
        }

        private void TransitionTo(GameState next)
        {
            // TODO: 현재 상태 Exit → 다음 상태 Enter
            currentState = next;
            throw new System.NotImplementedException();
        }
    }
}
