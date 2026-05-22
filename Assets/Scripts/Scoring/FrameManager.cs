using System;
using System.Collections.Generic;
using UnityEngine;
using BowlingGame;

namespace Bowling.Scoring
{
    /// <summary>
    /// 한 게임 동안의 프레임 진행과 점수를 관리하는 순수 도메인 객체.
    /// </summary>
    /// <remarks>
    /// 책임 범위:
    /// <list type="bullet">
    ///   <item>프레임 데이터 보관 / 투구 결과 기록 / 완료 판정</item>
    ///   <item>독립 프레임 방식으로 프레임 완료 즉시 점수 확정</item>
    ///   <item>외부에 이벤트로만 결과 통지 (UI/씬 직접 호출 없음)</item>
    /// </list>
    /// 책임 외(<see cref="GameManager"/> 등 상위가 담당):
    /// <list type="bullet">
    ///   <item>상태 머신 / 씬 전이 / 입력 처리 / UI 갱신</item>
    ///   <item>다음 프레임 이동 시점 결정 — 자동 이동하지 않으며, 외부에서 <see cref="AdvanceToNextFrame"/> 호출 필요</item>
    /// </list>
    /// 거부 정책 (fail-fast, <see cref="InvalidOperationException"/>):
    /// <list type="bullet">
    ///   <item>게임 종료 후 RecordThrow / AdvanceToNextFrame</item>
    ///   <item>프레임 미완료 상태에서 AdvanceToNextFrame</item>
    ///   <item>이미 완료된 프레임에 RecordThrow (반드시 AdvanceToNextFrame 선행)</item>
    /// </list>
    /// </remarks>
    public class FrameManager
    {
        private readonly BowlingRuleConfig config;
        private readonly List<Frame> frames;

        private int currentFrameIndex;     // 0-base. config.FrameCount 도달 시 게임 종료.
        private int currentThrowNumber;    // 1 또는 2 — 현재 프레임 내에서 "다음에 기록할 투구 번호".
        private bool currentFrameComplete; // 현재 프레임이 더 이상 투구를 받지 않는 상태인지.

        /// <summary>프레임 완료 시 발생. (frameIndex 0-base, 완료된 Frame 인스턴스)</summary>
        public event Action<int, Frame> OnFrameCompleted;

        /// <summary>마지막 프레임 완료 후 발생.</summary>
        public event Action OnGameOver;

        public FrameManager(BowlingRuleConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (config.FrameCount < 1)
                throw new ArgumentException("config.FrameCount must be >= 1.", nameof(config));

            this.config = config;
            frames = new List<Frame>(config.FrameCount);
            for (int i = 0; i < config.FrameCount; i++)
                frames.Add(new Frame());

            currentFrameIndex = 0;
            currentThrowNumber = 1;
            currentFrameComplete = false;
        }

        /// <summary>모든 프레임이 종료되었는지.</summary>
        public bool IsGameOver() => currentFrameIndex >= config.FrameCount;

        /// <summary>현재 프레임이 더 이상 투구를 받지 않는 상태인지.</summary>
        public bool IsFrameComplete() => IsGameOver() || currentFrameComplete;

        /// <summary>현재 프레임 인덱스(0-base). 게임 종료 후에는 FrameCount 와 같다.</summary>
        public int GetCurrentFrameIndex() => currentFrameIndex;

        /// <summary>현재 프레임 내에서 다음에 기록될 투구 번호 (1 또는 2).</summary>
        public int GetCurrentThrowNumber() => currentThrowNumber;

        /// <summary>현재까지 확정된 모든 프레임 점수의 합.</summary>
        public int GetTotalScore() => ScoreCalculator.CalculateTotalScore(frames);

        /// <summary>지정 인덱스(0-base)의 Frame 반환.</summary>
        public Frame GetFrame(int index)
        {
            if (index < 0 || index >= frames.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return frames[index];
        }

        /// <summary>
        /// 현재 프레임의 다음 투구로 결과를 기록한다.
        /// 1구가 스트라이크이거나 2구가 기록되면 프레임 완료 처리 + 점수 확정.
        /// </summary>
        public void RecordThrow(int pinsKnockedDown)
        {
            if (pinsKnockedDown < 0 || pinsKnockedDown > 10)
                throw new ArgumentOutOfRangeException(
                    nameof(pinsKnockedDown), pinsKnockedDown, "pinsKnockedDown must be within [0,10].");
            if (IsGameOver())
                throw new InvalidOperationException("Game is already over.");
            if (currentFrameComplete)
                throw new InvalidOperationException(
                    "Current frame is already complete — call AdvanceToNextFrame() first.");

            var frame = frames[currentFrameIndex];
            int displayFrame = currentFrameIndex + 1;
            int displayThrow = currentThrowNumber;

            if (currentThrowNumber == 1)
            {
                frame.Ball1 = pinsKnockedDown;

                if (pinsKnockedDown == 10)
                {
                    // 스트라이크: 1구만으로 프레임 완료
                    frame.FrameType = FrameType.Strike;
                    frame.FrameScore = ScoreCalculator.CalculateFrameScore(pinsKnockedDown, 0);
                    Debug.Log($"[프레임 {displayFrame} / 투구 {displayThrow}] 핀 {pinsKnockedDown}개 쓰러뜨림 → STRIKE, 프레임 점수 {frame.FrameScore}");
                    CompleteCurrentFrame(frame);
                }
                else
                {
                    Debug.Log($"[프레임 {displayFrame} / 투구 {displayThrow}] 핀 {pinsKnockedDown}개 쓰러뜨림");
                    currentThrowNumber = 2;
                }
            }
            else // 2구
            {
                frame.Ball2 = pinsKnockedDown;
                frame.FrameType = ScoreCalculator.DetermineFrameType(frame.Ball1, frame.Ball2);
                frame.FrameScore = ScoreCalculator.CalculateFrameScore(frame.Ball1, frame.Ball2);
                string typeLabel = frame.FrameType.ToString().ToUpperInvariant();
                Debug.Log($"[프레임 {displayFrame} / 투구 {displayThrow}] 핀 {pinsKnockedDown}개 쓰러뜨림 → {typeLabel}, 프레임 점수 {frame.FrameScore}");
                CompleteCurrentFrame(frame);
            }
        }

        /// <summary>
        /// 다음 프레임으로 이동한다. 호출 시점은 외부(GameManager)가 결정한다.
        /// 마지막 프레임이었다면 게임 종료 처리 + <see cref="OnGameOver"/> 발생.
        /// </summary>
        public void AdvanceToNextFrame()
        {
            if (IsGameOver())
                throw new InvalidOperationException("Game is already over.");
            if (!currentFrameComplete)
                throw new InvalidOperationException("Current frame is not complete yet.");

            currentFrameIndex++;
            currentThrowNumber = 1;
            currentFrameComplete = false;

            if (currentFrameIndex >= config.FrameCount)
            {
                Debug.Log($"[게임 종료] 최종 점수 {GetTotalScore()}");
                OnGameOver?.Invoke();
            }
        }

        private void CompleteCurrentFrame(Frame frame)
        {
            currentFrameComplete = true;
            int total = GetTotalScore();
            Debug.Log($"[프레임 {currentFrameIndex + 1} 완료] 판정 {frame.FrameType.ToString().ToUpperInvariant()}, 누적 총점 {total}");
            OnFrameCompleted?.Invoke(currentFrameIndex, frame);
        }
    }
}
