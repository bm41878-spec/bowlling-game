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

        // 디스플레이 설정 (2026-06-24 추가).
        // screenWidth/screenHeight == 0 은 "미설정" — SaveSystem.NormalizeDisplay 가 현재 모니터 해상도로 보정.
        // uiScale == 0 은 sentinel (UI 스케일 0 은 의미상 불가능) — 미마이그레이션으로 간주하고 NormalizeDisplay 가 디스플레이 전 필드를 기본값으로 일괄 복원.
        // fullScreenMode 는 UnityEngine.FullScreenMode 의 int 캐스팅: 0=ExclusiveFullScreen, 1=FullScreenWindow, 2=MaximizedWindow(mac), 3=Windowed.
        public int screenWidth = 1920;
        public int screenHeight = 1080;
        public int fullScreenMode = 1;
        public float uiScale = 1.0f;

        // 접근성 설정 (2026-06-24 추가).
        // aimingAudioGuide: 볼링공 조준(AimingPosition) 중 PingPong 이 좌/우 끝단에 도달할 때마다
        // 1회 비프음을 스테레오 패닝(좌=-1, 우=+1) 으로 재생 — 시각장애 플레이어 위치 안내용.
        // bool 의 기본값 false 는 "미설정 == 비활성" 의미가 자연스러우므로 마이그레이션 보정 불필요.
        public bool aimingAudioGuide = false;

        // colorblindMode: 0=Off, 1=Protanopia(적), 2=Deuteranopia(녹), 3=Tritanopia(청).
        // 현재는 UI 노출 + 저장만, 실제 셰이더/머티리얼 보정은 추후 업데이트.
        // int 의 기본값 0 이 "Off" 와 일치하므로 마이그레이션 보정 불필요.
        public int colorblindMode = 0;
    }
}
