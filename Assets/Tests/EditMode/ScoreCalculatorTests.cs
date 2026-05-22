using System;
using NUnit.Framework;
using Bowling.Scoring;

namespace Bowling.EditMode.Tests
{
    [TestFixture]
    public class ScoreCalculatorTests
    {
        // ---------- CalculateFrameScore ----------

        [Test]
        public void CalculateFrameScore_AllGutter_ReturnsZero()
        {
            // Arrange
            int ball1 = 0, ball2 = 0;

            // Act
            int score = ScoreCalculator.CalculateFrameScore(ball1, ball2);

            // Assert
            Assert.AreEqual(0, score);
        }

        [Test]
        public void CalculateFrameScore_Strike_Returns15()
        {
            int score = ScoreCalculator.CalculateFrameScore(10, 0);
            Assert.AreEqual(10 + ScoringConstants.STRIKE_BONUS, score);
            Assert.AreEqual(15, score);
        }

        [Test]
        public void CalculateFrameScore_NormalSpare_Returns13()
        {
            int score = ScoreCalculator.CalculateFrameScore(6, 4);
            Assert.AreEqual(10 + ScoringConstants.SPARE_BONUS, score);
            Assert.AreEqual(13, score);
        }

        [Test]
        public void CalculateFrameScore_ZeroTenSpare_Returns13()
        {
            int score = ScoreCalculator.CalculateFrameScore(0, 10);
            Assert.AreEqual(13, score);
        }

        [Test]
        public void CalculateFrameScore_Open_ReturnsSum()
        {
            int score = ScoreCalculator.CalculateFrameScore(3, 5);
            Assert.AreEqual(8, score);
        }

        [TestCase(0, 0, 0)]
        [TestCase(3, 5, 8)]
        [TestCase(9, 0, 9)]
        [TestCase(6, 4, 13)]
        [TestCase(0, 10, 13)]
        [TestCase(10, 0, 15)]
        [TestCase(10, 7, 15)] // strike: ball2 무시 정책 검증
        public void CalculateFrameScore_Parameterized(int ball1, int ball2, int expected)
        {
            Assert.AreEqual(expected, ScoreCalculator.CalculateFrameScore(ball1, ball2));
        }

        // ---------- DetermineFrameType ----------

        [Test]
        public void DetermineFrameType_BallOneTen_ReturnsStrike()
        {
            Assert.AreEqual(FrameType.Strike, ScoreCalculator.DetermineFrameType(10, 0));
        }

        [Test]
        public void DetermineFrameType_SumTenNotStrike_ReturnsSpare()
        {
            Assert.AreEqual(FrameType.Spare, ScoreCalculator.DetermineFrameType(6, 4));
        }

        [Test]
        public void DetermineFrameType_SumUnderTen_ReturnsNormal()
        {
            Assert.AreEqual(FrameType.Normal, ScoreCalculator.DetermineFrameType(3, 5));
        }

        [TestCase(10, 0, FrameType.Strike)]
        [TestCase(0, 10, FrameType.Spare)]
        [TestCase(6, 4, FrameType.Spare)]
        [TestCase(3, 5, FrameType.Normal)]
        [TestCase(0, 0, FrameType.Normal)]
        public void DetermineFrameType_Parameterized(int ball1, int ball2, FrameType expected)
        {
            Assert.AreEqual(expected, ScoreCalculator.DetermineFrameType(ball1, ball2));
        }

        // ---------- 입력 검증 ----------

        [TestCase(-1, 0)]
        [TestCase(11, 0)]
        [TestCase(0, -1)]
        [TestCase(0, 11)]
        public void CalculateFrameScore_OutOfRange_ThrowsArgumentOutOfRange(int ball1, int ball2)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => ScoreCalculator.CalculateFrameScore(ball1, ball2));
        }

        [Test]
        public void CalculateFrameScore_SumOverTenNonStrike_ThrowsArgument()
        {
            Assert.Throws<ArgumentException>(
                () => ScoreCalculator.CalculateFrameScore(7, 5));
        }

        // ---------- CalculateTotalScore ----------

        [Test]
        public void CalculateTotalScore_NullList_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => ScoreCalculator.CalculateTotalScore(null));
        }

        [Test]
        public void CalculateTotalScore_EmptyList_ReturnsZero()
        {
            Assert.AreEqual(0, ScoreCalculator.CalculateTotalScore(new System.Collections.Generic.List<Frame>()));
        }
    }
}
