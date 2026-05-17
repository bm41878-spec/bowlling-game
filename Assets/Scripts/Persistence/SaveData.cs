// TODO: Phase 8 — Application.persistentDataPath/save.json 으로 저장/로드
using System;
using System.Collections.Generic;

namespace BowlingGame
{
    [Serializable]
    public class SaveData
    {
        public List<GameRecord> highScores;      // 모드별 최고 점수
        public string selectedBallSkin;
        public string selectedCharacterSkin;
    }
}
