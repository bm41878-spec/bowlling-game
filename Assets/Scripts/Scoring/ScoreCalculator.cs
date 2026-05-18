using System;
using System.Collections.Generic;
using System.Linq;

namespace BowlingGame
{
    public class ScoreCalculator
    {
        public int? CalculateFrameScore(List<Frame> frames, int targetIndex, int totalFrameCount)
        {
            if (frames == null || totalFrameCount <= 0)
                throw new ArgumentException("frames가 null이거나 totalFrameCount가 0 이하입니다.");
            if (targetIndex < 0 || targetIndex >= totalFrameCount)
                throw new ArgumentOutOfRangeException(nameof(targetIndex));

            bool isLast = (targetIndex == totalFrameCount - 1);
            Frame f = frames[targetIndex];

            if (isLast)
            {
                if (f.IsComplete(true))
                    return f.GetRollsArray().Sum();
                return null;
            }

            if (f.IsStrike())
            {
                List<int> future = GetFutureRolls(frames, targetIndex, totalFrameCount, 2);
                if (future.Count == 2)
                    return 10 + future[0] + future[1];
                return null;
            }

            if (f.IsSpare())
            {
                List<int> future = GetFutureRolls(frames, targetIndex, totalFrameCount, 1);
                if (future.Count == 1)
                    return 10 + future[0];
                return null;
            }

            if (f.SecondRoll >= 0)
                return f.FirstRoll + f.SecondRoll;
            return null;
        }

        private List<int> GetFutureRolls(List<Frame> frames, int fromFrameIndex, int totalFrameCount, int count)
        {
            var collected = new List<int>(count);
            for (int i = fromFrameIndex + 1; i < totalFrameCount && i < frames.Count; i++)
            {
                int[] rolls = frames[i].GetRollsArray();
                for (int r = 0; r < rolls.Length && collected.Count < count; r++)
                    collected.Add(rolls[r]);
                if (collected.Count >= count) break;
            }
            if (collected.Count < count)
                return new List<int>();
            return collected;
        }

        public List<int?> CalculateCumulativeScores(List<Frame> frames, int totalFrameCount)
        {
            if (frames == null || totalFrameCount <= 0)
                throw new ArgumentException("frames가 null이거나 totalFrameCount가 0 이하입니다.");

            var result = new List<int?>(totalFrameCount);
            int cumulative = 0;
            bool broken = false;
            for (int i = 0; i < totalFrameCount; i++)
            {
                if (broken)
                {
                    result.Add(null);
                    continue;
                }

                int? frameScore = CalculateFrameScore(frames, i, totalFrameCount);
                if (frameScore == null)
                {
                    broken = true;
                    result.Add(null);
                }
                else
                {
                    cumulative += frameScore.Value;
                    result.Add(cumulative);
                }
            }
            return result;
        }

        public int GetPerfectScore(int totalFrameCount) => 30 * totalFrameCount;
    }
}
