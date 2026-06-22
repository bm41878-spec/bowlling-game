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

        // 음량(0~1). 기본값은 NewEmpty() 경로에서만 보장되며,
        // 기존 save.json (음량 필드 미존재) 로드 시 JsonUtility 가 0f 로 두므로
        // SaveSystem.Load() 에서 0f 감지 시 기본값으로 보정한다 (자세한 내용은 SaveSystem.NormalizeVolumes).
        public float masterVolume = 1.0f;
        public float sfxVolume    = 1.0f;
        public float bgmVolume    = 0.7f;

        // 의도적 음소거 — 음량 0 (미마이그레이션 처리 분기) 과 구분되어야 한다.
        // bool 의 기본값 false 는 미설정과 동일한 의미라 마이그레이션 보정 불필요.
        public bool isMuted = false;

        // Unity Input System 의 InputAction.SaveBindingOverridesAsJson 결과.
        // null/empty 면 기본 binding 사용. 마이그레이션 보정 불필요 (null = 미설정 = 기본값).
        public string inputOverridesJson = "";
    }
}
