using UnityEngine;
using Bowling.Scoring;

namespace BowlingGame
{
    /// <summary>
    /// 모드별 게임 룰을 코드 수정 없이 에디터에서 관리하기 위한 ScriptableObject.
    /// 새 모드 추가 시 새 에셋만 만들면 되며, 모드별 분기 로직은 코드에 두지 않는다.
    /// </summary>
    /// <remarks>
    /// 에셋 생성 가이드:
    /// <list type="number">
    ///   <item>Project 창에서 <c>Assets/Configs/</c> 폴더로 이동.</item>
    ///   <item>우클릭 → Create → Bowling → Rule Config 로 에셋 생성.</item>
    ///   <item>다음 두 에셋을 만든다 (저장 위치: <c>Assets/Configs/</c>):
    ///     <list type="bullet">
    ///       <item><c>ShortModeRule.asset</c> — modeName="쇼트 모드", frameCount=5</item>
    ///       <item><c>FullModeRule.asset</c>  — modeName="풀 모드",   frameCount=10</item>
    ///     </list>
    ///   </item>
    ///   <item><c>ballSpeed</c>, <c>powerGaugeSpeed</c> 는 튜닝 대상 — 아래 기본값(1.2 / 1.5)에서 시작.</item>
    /// </list>
    /// </remarks>
    [CreateAssetMenu(fileName = "BowlingRule", menuName = "Bowling/Rule Config")]
    public class BowlingRuleConfig : ScriptableObject
    {
        [Header("모드 식별")]
        [SerializeField, Tooltip("UI 표시 및 식별용 모드 이름 (예: 쇼트 모드 / 풀 모드)")]
        private string modeName = "쇼트 모드";

        [Header("규칙")]
        [SerializeField, Tooltip("이 모드의 총 프레임 수 (쇼트=5, 풀=10)")]
        private int frameCount = 5;

        [SerializeField, Tooltip("프레임당 핀 개수 (표준 10)")]
        private int pinCount = 10;

        [Header("조작감 (튜닝 대상)")]
        [SerializeField, Tooltip("위치 결정 단계에서 공이 좌우로 왕복하는 속도. 임시값.")]
        private float ballSpeed = 1.2f;

        [SerializeField, Tooltip("세기 게이지가 변동하는 속도. 임시값.")]
        private float powerGaugeSpeed = 1.5f;

        /// <summary>UI/식별용 모드 이름.</summary>
        public string ModeName => modeName;

        /// <summary>총 프레임 수.</summary>
        public int FrameCount => frameCount;

        /// <summary>프레임당 핀 개수.</summary>
        public int PinCount => pinCount;

        /// <summary>위치 결정 단계 좌우 왕복 속도.</summary>
        public float BallSpeed => ballSpeed;

        /// <summary>세기 게이지 변동 속도.</summary>
        public float PowerGaugeSpeed => powerGaugeSpeed;

        /// <summary>
        /// 이 룰에서의 만점(모든 프레임 스트라이크 가정).
        /// 프레임당 점수(10 + STRIKE_BONUS)는 <see cref="ScoringConstants"/> 에서 단일 관리.
        /// </summary>
        public int GetPerfectScore() => (10 + ScoringConstants.STRIKE_BONUS) * frameCount;

        private void OnValidate()
        {
            if (frameCount < 1) frameCount = 1;
            if (ballSpeed <= 0f) ballSpeed = 0.01f;
            if (powerGaugeSpeed <= 0f) powerGaugeSpeed = 0.01f;
        }
    }
}
