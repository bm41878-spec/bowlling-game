// Phase 8 — Application.persistentDataPath/save.json 으로 저장/로드 (SaveSystem 참조)
using System;
using System.Collections.Generic;

namespace BowlingGame
{
    [Serializable]
    public class SaveData
    {
        /// <summary>세이브 데이터 스키마 버전. 포맷 변경 시 마이그레이션 분기에 사용 (현재 1).</summary>
        public int version = 1;

        public List<GameRecord> highScores = new List<GameRecord>();  // 모드별 최고 점수 (HighScoreService 가 모드당 Top N 유지)
        public string selectedBallSkin;
        public string selectedCharacterSkin;
    }
}
