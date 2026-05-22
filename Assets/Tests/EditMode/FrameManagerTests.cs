using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Bowling.Scoring;
using BowlingGame;

namespace Bowling.EditMode.Tests
{
    [TestFixture]
    public class FrameManagerTests
    {
        // ---------- 헬퍼: 테스트용 BowlingRuleConfig 생성 ----------

        private static BowlingRuleConfig MakeRule(int frameCount)
        {
            var rule = ScriptableObject.CreateInstance<BowlingRuleConfig>();
            var fc = typeof(BowlingRuleConfig).GetField(
                "frameCount",
                BindingFlags.NonPublic | BindingFlags.Instance);
            fc.SetValue(rule, frameCount);
            return rule;
        }

        // MonoBehaviour 전환 후 임시 헬퍼.
        // ※ 일부 기존 어서션(InvalidOperationException 기대 등)은 새 명세에서 경고+무시로 바뀌어
        //   런타임에 실패할 수 있음 — 단계 5 에서 전체 테스트 리팩토링 예정.
        private readonly List<GameObject> _spawnedHosts = new List<GameObject>();

        [TearDown]
        public void DestroySpawnedHosts()
        {
            for (int i = 0; i < _spawnedHosts.Count; i++)
                if (_spawnedHosts[i] != null) UnityEngine.Object.DestroyImmediate(_spawnedHosts[i]);
            _spawnedHosts.Clear();
        }

        private FrameManager MakeManager(int frameCount)
        {
            var host = new GameObject("TestFrameManagerHost");
            _spawnedHosts.Add(host);
            var fm = host.AddComponent<FrameManager>();
            fm.Initialize(MakeRule(frameCount));
            return fm;
        }

        // 프레임 완료까지 투구하고 다음 프레임으로 진행 (편의 헬퍼)
        private static void PlayFrame(FrameManager fm, int ball1, int ball2)
        {
            fm.RecordThrow(ball1);
            if (ball1 < 10) fm.RecordThrow(ball2);
            if (!fm.IsGameOver()) fm.AdvanceToNextFrame();
        }

        // ---------- 통합 시나리오 ----------

        [Test]
        public void AllStrikes_ShortMode_TotalScore75()
        {
            // Arrange
            var fm = MakeManager(5);

            // Act
            for (int i = 0; i < 5; i++)
            {
                fm.RecordThrow(10);
                if (!fm.IsGameOver()) fm.AdvanceToNextFrame();
            }

            // Assert
            Assert.AreEqual(75, fm.GetTotalScore());
            Assert.IsTrue(fm.IsGameOver());
        }

        [Test]
        public void AllStrikes_FullMode_TotalScore150()
        {
            var fm = MakeManager(10);

            for (int i = 0; i < 10; i++)
            {
                fm.RecordThrow(10);
                if (!fm.IsGameOver()) fm.AdvanceToNextFrame();
            }

            Assert.AreEqual(150, fm.GetTotalScore());
            Assert.IsTrue(fm.IsGameOver());
        }

        [Test]
        public void AllGutters_TotalScoreZero()
        {
            var fm = MakeManager(5);

            for (int i = 0; i < 5; i++)
                PlayFrame(fm, 0, 0);

            Assert.AreEqual(0, fm.GetTotalScore());
            Assert.IsTrue(fm.IsGameOver());
        }

        [Test]
        public void ShortModeExample_AccumulatedScores()
        {
            // README §8 예제 시퀀스: (6,4) (10) (3,5) (0,10) (7,2)
            // 누적 총점 기대: 13 → 28 → 36 → 49 → 58
            var fm = MakeManager(5);
            var expectedTotals = new int[] { 13, 28, 36, 49, 58 };

            var throws = new (int b1, int b2)[]
            {
                (6, 4),
                (10, 0),
                (3, 5),
                (0, 10),
                (7, 2),
            };

            for (int i = 0; i < throws.Length; i++)
            {
                fm.RecordThrow(throws[i].b1);
                if (throws[i].b1 < 10) fm.RecordThrow(throws[i].b2);

                Assert.AreEqual(
                    expectedTotals[i],
                    fm.GetTotalScore(),
                    $"frame {i + 1} 완료 후 누적 총점 불일치");

                if (!fm.IsGameOver()) fm.AdvanceToNextFrame();
            }

            Assert.AreEqual(58, fm.GetTotalScore());
            Assert.IsTrue(fm.IsGameOver());
        }

        [Test]
        public void FrameCompleted_ImmediatelyFixesScore()
        {
            // 프레임 완료 시점(= RecordThrow 직후, AdvanceToNextFrame 전)에
            // 해당 Frame.FrameScore 가 이미 확정되어 있어야 한다.
            var fm = MakeManager(5);
            int? scoreAtCompletion = null;

            fm.OnFrameCompleted += (idx, frame) =>
            {
                scoreAtCompletion = frame.FrameScore;
            };

            fm.RecordThrow(6);
            fm.RecordThrow(4); // 스페어 → 즉시 완료

            Assert.IsTrue(scoreAtCompletion.HasValue, "OnFrameCompleted 가 발생하지 않음");
            Assert.AreEqual(13, scoreAtCompletion.Value);
            Assert.AreEqual(13, fm.GetFrame(0).FrameScore);
            Assert.AreEqual(FrameType.Spare, fm.GetFrame(0).FrameType);
        }

        [Test]
        public void GameOver_AfterLastFrame()
        {
            var fm = MakeManager(5);

            for (int i = 0; i < 5; i++)
                PlayFrame(fm, 3, 2);

            Assert.IsTrue(fm.IsGameOver());
            Assert.AreEqual(25, fm.GetTotalScore());
        }

        [Test]
        public void OnGameOver_FiresOnceAfterLastAdvance()
        {
            var fm = MakeManager(3);
            int callCount = 0;
            fm.OnGameOver += () => callCount++;

            for (int i = 0; i < 3; i++)
            {
                fm.RecordThrow(10);
                if (!fm.IsGameOver()) fm.AdvanceToNextFrame();
                else fm.AdvanceToNextFrame(); // 마지막 프레임에서도 advance 호출 → 게임 종료 트리거
            }

            Assert.AreEqual(1, callCount);
            Assert.IsTrue(fm.IsGameOver());
        }

        // ---------- 엣지 케이스 ----------

        [Test]
        public void RecordThrow_AfterGameOver_ThrowsInvalidOperation()
        {
            var fm = MakeManager(2);

            // 2 프레임 모두 스트라이크 → 게임 종료
            fm.RecordThrow(10);
            fm.AdvanceToNextFrame();
            fm.RecordThrow(10);
            fm.AdvanceToNextFrame();

            Assert.IsTrue(fm.IsGameOver());
            Assert.Throws<InvalidOperationException>(() => fm.RecordThrow(5));
        }

        [Test]
        public void AdvanceToNextFrame_AfterGameOver_ThrowsInvalidOperation()
        {
            var fm = MakeManager(1);

            fm.RecordThrow(10);
            fm.AdvanceToNextFrame();

            Assert.IsTrue(fm.IsGameOver());
            Assert.Throws<InvalidOperationException>(() => fm.AdvanceToNextFrame());
        }

        [Test]
        public void AdvanceToNextFrame_BeforeFrameComplete_ThrowsInvalidOperation()
        {
            var fm = MakeManager(5);

            fm.RecordThrow(3); // 1구만 던지고 미완료 상태

            Assert.Throws<InvalidOperationException>(() => fm.AdvanceToNextFrame());
        }

        [Test]
        public void RecordThrow_AfterFrameComplete_ThrowsInvalidOperation()
        {
            var fm = MakeManager(5);

            fm.RecordThrow(10); // 스트라이크 → 프레임 완료, AdvanceToNextFrame 미호출

            Assert.Throws<InvalidOperationException>(() => fm.RecordThrow(5));
        }

        [TestCase(-1)]
        [TestCase(-100)]
        [TestCase(11)]
        [TestCase(99)]
        public void RecordThrow_OutOfRangePins_ThrowsArgumentOutOfRange(int pins)
        {
            var fm = MakeManager(5);
            Assert.Throws<ArgumentOutOfRangeException>(() => fm.RecordThrow(pins));
        }
    }
}
