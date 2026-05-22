using System;
using System.Collections.Generic;
using UnityEngine;
using BowlingGame;

namespace Bowling.Scoring
{
    /// <summary>
    /// 한 게임 동안의 프레임 진행과 점수를 관리하는 MonoBehaviour.
    /// 씬에 배치하여 [SerializeField] 로 외부에서 참조하며, Initialize() 로 의존성을 주입한다.
    /// </summary>
    /// <remarks>
    /// 책임 범위:
    /// <list type="bullet">
    ///   <item>프레임 데이터 보관 / 투구 결과 기록 / 완료 판정 / 점수 확정 (독립 프레임 방식)</item>
    ///   <item>UI · GameManager 를 모름 — 이벤트만 발행 (단방향 의존)</item>
    ///   <item>점수 계산은 <see cref="ScoreCalculator"/> 에 위임 — 표시 규칙(X, /, -)은 외부 책임</item>
    /// </list>
    /// 비정상 호출 정책: 잘못된 상태에서의 호출은 경고 로그 후 무시 (fail-safe).
    /// 단, 입력 범위(pinsKnockedDown ∈ [0,10]) 위반은 <see cref="ArgumentOutOfRangeException"/>.
    /// </remarks>
    public class FrameManager : MonoBehaviour
    {
        private BowlingRuleConfig ruleConfig;
        private List<Frame> frames;

        private int currentFrameIndex;     // 0-based
        private int currentThrowNumber;    // 1 또는 2 (다음에 기록할 투구 번호)
        private bool isGameOver;
        private bool isInitialized;

        // 보조 상태: 1구 스트라이크 또는 2구 기록 완료 후 true. AdvanceToNextFrame 에서 false 로 리셋.
        private bool currentFrameComplete;

        /// <summary>Initialize() 호출 직후 발행. 페이로드 없음.</summary>
        public event Action OnGameInitialized;

        /// <summary>새 프레임이 시작될 때 발행. (frameIndex 0-base)</summary>
        public event Action<int> OnFrameStarted;

        /// <summary>투구 결과 기록 직후 발행. (frameIndex, throwNumber 1|2, 현재 Frame)</summary>
        public event Action<int, int, Frame> OnThrowRecorded;

        /// <summary>프레임 완료 직후 발행. (frameIndex, 완료된 Frame)</summary>
        public event Action<int, Frame> OnFrameCompleted;

        /// <summary>마지막 프레임 완료 직후 발행. 페이로드 없음.</summary>
        public event Action OnGameOver;

        /// <summary>
        /// 게임 시작 시 호출. 재호출 가능 — 게임 재시작 지원.
        /// 발행 순서: <see cref="OnGameInitialized"/> → <see cref="OnFrameStarted"/>(0).
        /// </summary>
        public void Initialize(BowlingRuleConfig config)
        {
            if (config == null)
            {
                Debug.LogError("[FrameManager] Initialize 실패 — config 가 null 입니다.");
                return;
            }
            if (config.FrameCount < 1)
            {
                Debug.LogError($"[FrameManager] Initialize 실패 — frameCount 가 1 미만입니다 ({config.FrameCount}).");
                return;
            }

            ruleConfig = config;

            frames = new List<Frame>(config.FrameCount);
            for (int i = 0; i < config.FrameCount; i++)
                frames.Add(new Frame());

            currentFrameIndex = 0;
            currentThrowNumber = 1;
            currentFrameComplete = false;
            isGameOver = false;
            isInitialized = true;

            Debug.Log($"[FrameManager] 초기화 완료 (모드: {ruleConfig.ModeName}, 총 {ruleConfig.FrameCount}프레임)");

            OnGameInitialized?.Invoke();
            OnFrameStarted?.Invoke(0);
        }

        /// <summary>
        /// 현재 프레임의 다음 투구로 결과를 기록한다.
        /// 발행 순서: <see cref="OnThrowRecorded"/> → (프레임 완료 시) <see cref="OnFrameCompleted"/> → (마지막 프레임이면) <see cref="OnGameOver"/>.
        /// </summary>
        public void RecordThrow(int pinsKnockedDown)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[FrameManager] RecordThrow 호출 무시 — 아직 초기화되지 않았습니다.");
                return;
            }
            if (isGameOver)
            {
                Debug.LogWarning("[FrameManager] RecordThrow 호출 무시 — 게임이 이미 종료되었습니다.");
                return;
            }
            if (currentFrameComplete)
            {
                Debug.LogWarning("[FrameManager] RecordThrow 호출 무시 — 현재 프레임이 이미 완료되었습니다. AdvanceToNextFrame() 선행 필요.");
                return;
            }
            if (pinsKnockedDown < 0 || pinsKnockedDown > 10)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(pinsKnockedDown), pinsKnockedDown, "pinsKnockedDown must be within [0,10].");
            }

            var frame = frames[currentFrameIndex];
            int displayFrame = currentFrameIndex + 1;
            int recordedThrowNumber = currentThrowNumber; // 이벤트 페이로드용: 4단계 변경 전 값

            // 1) 핀 수 기록 + FrameType / FrameScore 갱신
            if (currentThrowNumber == 1)
            {
                frame.Ball1 = pinsKnockedDown;

                if (pinsKnockedDown == 10)
                {
                    frame.FrameType = FrameType.Strike;
                    frame.FrameScore = ScoreCalculator.CalculateFrameScore(10, 0);
                    Debug.Log($"[FrameManager] 프레임 {displayFrame} / 1구: {pinsKnockedDown}핀 → STRIKE (프레임 점수 {frame.FrameScore})");
                }
                else
                {
                    frame.FrameType = FrameType.Normal; // 임시
                    frame.FrameScore = 0;               // 임시 — 2구 완료 시 확정
                    Debug.Log($"[FrameManager] 프레임 {displayFrame} / 1구: {pinsKnockedDown}핀");
                }
            }
            else // currentThrowNumber == 2
            {
                frame.Ball2 = pinsKnockedDown;
                frame.FrameType = ScoreCalculator.DetermineFrameType(frame.Ball1, frame.Ball2);
                frame.FrameScore = ScoreCalculator.CalculateFrameScore(frame.Ball1, frame.Ball2);
                string typeLabel = frame.FrameType.ToString().ToUpperInvariant();
                Debug.Log($"[FrameManager] 프레임 {displayFrame} / 2구: {pinsKnockedDown}핀 → {typeLabel} (프레임 점수 {frame.FrameScore})");
            }

            // 2) currentThrowNumber 진행 (1구 비-스트라이크에서만 2로 전이)
            if (currentThrowNumber == 1 && pinsKnockedDown != 10)
                currentThrowNumber = 2;

            // 3) OnThrowRecorded — throwNumber 는 변경 전 값(recordedThrowNumber) 전달
            OnThrowRecorded?.Invoke(currentFrameIndex, recordedThrowNumber, frame);

            // 4) 프레임 완료 판정: 1구 스트라이크 OR 2구 기록 완료
            bool frameJustCompleted =
                (recordedThrowNumber == 1 && pinsKnockedDown == 10) ||
                (recordedThrowNumber == 2);

            if (frameJustCompleted)
            {
                currentFrameComplete = true;
                Debug.Log($"[FrameManager] 프레임 {displayFrame} 완료 (누적 {GetTotalScore()}점)");
                OnFrameCompleted?.Invoke(currentFrameIndex, frame);

                // 마지막 프레임이면 게임 종료
                if (currentFrameIndex == ruleConfig.FrameCount - 1)
                {
                    isGameOver = true;
                    Debug.Log($"[FrameManager] 게임 종료 (최종 점수 {GetTotalScore()}점)");
                    OnGameOver?.Invoke();
                }
            }
        }

        /// <summary>
        /// 다음 프레임으로 이동한다. 호출 시점은 외부(GameManager)가 결정한다.
        /// 마지막 프레임 완료 후에는 호출하지 않는다(<see cref="OnGameOver"/>는 RecordThrow 에서 이미 발행됨).
        /// 발행 순서: <see cref="OnFrameStarted"/>(newIndex).
        /// </summary>
        public void AdvanceToNextFrame()
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[FrameManager] AdvanceToNextFrame 호출 무시 — 아직 초기화되지 않았습니다.");
                return;
            }
            if (isGameOver)
            {
                Debug.LogWarning("[FrameManager] AdvanceToNextFrame 호출 무시 — 게임이 이미 종료되었습니다.");
                return;
            }
            if (!currentFrameComplete)
            {
                Debug.LogWarning("[FrameManager] AdvanceToNextFrame 호출 무시 — 현재 프레임이 완료되지 않았습니다.");
                return;
            }

            currentFrameIndex++;
            currentThrowNumber = 1;
            currentFrameComplete = false;

            Debug.Log($"[FrameManager] 프레임 {currentFrameIndex + 1} 시작");
            OnFrameStarted?.Invoke(currentFrameIndex);
        }

        // -------- 상태 조회 --------

        /// <summary>현재 프레임이 더 이상 투구를 받지 않는 상태인지 (1구 스트라이크 또는 2구까지 기록됨).</summary>
        public bool IsFrameComplete() => currentFrameComplete;

        /// <summary>마지막 프레임 완료 후 true.</summary>
        public bool IsGameOver() => isGameOver;

        /// <summary>현재 프레임 인덱스 (0-base).</summary>
        public int GetCurrentFrameIndex() => currentFrameIndex;

        /// <summary>현재 프레임 내에서 다음에 기록될 투구 번호 (1 또는 2).</summary>
        public int GetCurrentThrowNumber() => currentThrowNumber;

        /// <summary>모든 frame.FrameScore 의 합. 미완료 프레임은 0 이므로 영향 없음.</summary>
        public int GetTotalScore()
        {
            if (frames == null) return 0;
            return ScoreCalculator.CalculateTotalScore(frames);
        }

        /// <summary>지정 인덱스(0-base)의 Frame 반환.</summary>
        public Frame GetFrame(int index)
        {
            if (frames == null || index < 0 || index >= frames.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return frames[index];
        }

        /// <summary>이 게임의 총 프레임 수 (UI 활용용). 초기화 전에는 0.</summary>
        public int GetFrameCount() => ruleConfig != null ? ruleConfig.FrameCount : 0;
    }
}
