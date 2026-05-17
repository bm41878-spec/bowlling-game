// TODO: Phase 1 — 셋업 단계에서 ScriptableObject 에셋 생성 및 모드별 값 설정
using UnityEngine;

namespace BowlingGame
{
    [CreateAssetMenu(fileName = "BowlingRule", menuName = "Bowling/Rule Config")]
    public class BowlingRuleConfig : ScriptableObject
    {
        public string modeName;       // "캐주얼", "표준", "하드"
        public int frameCount;        // 3, 5, 10
        public int pinCount = 10;
        public float ballSpeed;       // 좌우 이동 속도
        public float powerGaugeSpeed; // 게이지 변동 속도
    }
}
