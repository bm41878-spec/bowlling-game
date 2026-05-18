using System;
using System.Collections.Generic;

namespace BowlingGame
{
    public class Frame
    {
        public int FrameIndex { get; }
        public int FirstRoll { get; private set; } = -1;
        public int SecondRoll { get; private set; } = -1;
        public int ThirdRoll { get; private set; } = -1;
        public int? ConfirmedScore { get; set; }

        // 이벤트 중복 방지용 내부 플래그
#pragma warning disable CS0414
        private bool _firstFallen = false;
#pragma warning restore CS0414

        public Frame(int frameIndex)
        {
            FrameIndex = frameIndex;
        }

        public bool IsStrike() => FirstRoll == 10;
        public bool IsSpare() => !IsStrike() && FirstRoll + SecondRoll == 10;
        public bool IsOpen() => !IsStrike() && !IsSpare() && FirstRoll >= 0 && SecondRoll >= 0;

        public bool IsComplete(bool isLastFrame)
        {
            if (isLastFrame)
            {
                if (IsStrike() || IsSpare())
                    return ThirdRoll >= 0;
                return SecondRoll >= 0;
            }

            if (IsStrike())
                return FirstRoll >= 0;
            return SecondRoll >= 0;
        }

        public void RecordRoll(int pins)
        {
            if (pins < 0 || pins > 10)
                throw new InvalidOperationException("유효하지 않은 핀 수");

            if (FirstRoll < 0)
            {
                FirstRoll = pins;
                return;
            }

            if (SecondRoll < 0)
            {
                // Strike 후속(마지막 프레임 보너스)이 아니면 합이 10을 넘을 수 없음
                if (FirstRoll != 10 && FirstRoll + pins > 10)
                    throw new InvalidOperationException("유효하지 않은 핀 수");
                SecondRoll = pins;
                return;
            }

            if (ThirdRoll < 0)
            {
                // 마지막 프레임에서 Strike 또는 Spare인 경우에만 허용
                if (!IsStrike() && !IsSpare())
                    throw new InvalidOperationException("유효하지 않은 핀 수");
                ThirdRoll = pins;
                return;
            }

            throw new InvalidOperationException("유효하지 않은 핀 수");
        }

        public int[] GetRollsArray()
        {
            var list = new List<int>(3);
            if (FirstRoll >= 0) list.Add(FirstRoll);
            if (SecondRoll >= 0) list.Add(SecondRoll);
            if (ThirdRoll >= 0) list.Add(ThirdRoll);
            return list.ToArray();
        }
    }
}
