// TODO: Phase 8 — SaveSystem에서 직렬화/역직렬화 대상으로 사용
using System;

namespace BowlingGame
{
    [Serializable]
    public class GameRecord
    {
        public string modeName;
        public int frameCount;
        public int score;
        public string playedAt; // ISO 8601 (예: "2026-05-17T12:00:00Z")
    }
}
