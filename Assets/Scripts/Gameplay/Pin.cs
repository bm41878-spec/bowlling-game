using System;
using UnityEngine;

namespace BowlingGame
{
    /// <summary>
    /// 한 핀의 식별 / 쓰러짐 판정 / 초기 상태 복원을 담당하는 컴포넌트.
    /// 핀 그룹 관리(일괄 수집, 스냅샷, 일괄 리셋)는 <see cref="PinManager"/> 의 책임이며,
    /// 점수 계산은 Bowling.Scoring 도메인 책임이다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Pin : MonoBehaviour
    {
        [Header("식별")]
        [Range(1, 10)]
        [Tooltip("표준 볼링 핀 번호 (1~10). 인스펙터에서 핀별로 지정.")]
        public int pinId = 1;

        [Header("판정 임계값")]
        [Tooltip("쓰러짐 판정 각도(도). transform.up 과 월드 Up 의 사잇각이 이 값을 넘으면 쓰러진 것으로 본다.")]
        [SerializeField] private float fallThreshold = 45f;

        [Tooltip("정지 판정용 속도 임계값(제곱). 0.0025 ≈ |v|<0.05 m/s.")]
        [SerializeField] private float restVelocityEpsilonSqr = 0.0025f;

        /// <summary>핀이 처음 쓰러진 순간 한 번 발생한다(반복 발화 없음).</summary>
        public event Action<Pin> OnPinFallen;

        private Rigidbody _rb;
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;

        // DebugResetController 가 리플렉션으로 접근하는 필드 — 이름 유지 필요.
        private bool _fallen;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
        }

        void Update()
        {
            if (_fallen) return;
            if (!IsFallen()) return;

            _fallen = true;
            float angle = Vector3.Angle(transform.up, Vector3.up);
            Debug.Log($"[Pin {pinId}] 쓰러짐 감지 (각도: {angle:F1}°)");
            OnPinFallen?.Invoke(this);
        }

        /// <summary>각도 임계 기준 쓰러짐 여부.</summary>
        public bool IsFallen() =>
            Vector3.Angle(transform.up, Vector3.up) > fallThreshold;

        /// <summary>
        /// 물리적 정지 상태 여부.
        /// <c>Rigidbody.IsSleeping()</c> 이거나,
        /// linearVelocity·angularVelocity 가 둘 다 임계값(제곱) 이하면 true.
        /// </summary>
        public bool IsAtRest()
        {
            if (_rb == null) return true;
            if (_rb.IsSleeping()) return true;
            return _rb.linearVelocity.sqrMagnitude < restVelocityEpsilonSqr
                && _rb.angularVelocity.sqrMagnitude < restVelocityEpsilonSqr;
        }

        /// <summary>
        /// Awake 시 캐싱한 초기 position/rotation 으로 복원하고 물리 상태를 정지시킨다.
        /// 쓰러짐 플래그도 해제되어 다음 라운드에 다시 감지될 수 있다.
        /// </summary>
        public void ResetToInitialState()
        {
            // kinematic 상태에서 velocity 설정 시 Unity 경고 발생 — 동적 바디일 때만 처리
            if (_rb != null && !_rb.isKinematic)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }

            transform.position = _initialPosition;
            transform.rotation = _initialRotation;

            if (_rb != null && !_rb.isKinematic) _rb.Sleep();
            _fallen = false;
        }

        /// <summary>
        /// 1구 → 2구 전환 시 핀의 가시성을 제어한다.
        /// 기본 구현은 GameObject 활성/비활성. 추후 스윕 애니메이션 등으로 교체 시
        /// 호출부 변경 없이 이 메서드 내부만 수정한다.
        /// </summary>
        public void SetActive(bool active)
        {
            if (gameObject.activeSelf == active) return;
            gameObject.SetActive(active);
        }
    }
}
