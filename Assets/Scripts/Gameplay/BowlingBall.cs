using System;
using UnityEngine;

namespace BowlingGame
{
    public class BowlingBall : MonoBehaviour
    {
        /// <summary>
        /// 현재 투구에서 공이 처음 핀(Pin 컴포넌트 보유)에 닿는 순간 1회 발화.
        /// 다음 투구에서 다시 발화하려면 ResetBall() 경유로 플래그가 리셋되어야 한다.
        /// 사운드/이펙트/카메라쉐이크 등 외부 모듈이 구독한다.
        /// </summary>
        public event Action OnFirstPinContact;

        /// <summary>
        /// 현재 투구에서 공이 처음 거터 영역(|x| &gt; 0.533)에 진입하는 순간 1회 발화하는 <b>저수준 즉시 신호</b>.
        /// Rolling 상태에서만 폴링하며, ResetBall() 에서 플래그가 리셋된다.
        /// 거터 '판정'(거터 진입 + 0핀 동시 충족)은 <see cref="ThrowTransitionController"/> 가
        /// <see cref="EnteredGutterThisThrow"/> 와 쓰러진 핀 수를 결합해 내린다(OnGutterBall).
        /// </summary>
        public event Action OnEnteredGutter;
        [SerializeField] float minForce = 8f;
        [SerializeField] float maxForce = 18f;

        [Tooltip("커브 세기 계수(최대치 = 게이지 끝). Rolling 동안 매 FixedUpdate 마다 측면 힘 = spin × 이 값 × 전진속도 로 가해진다. spin 은 SpinGaugeUI 에서 직구 데드존 + 끝강조 지수가 적용된 값. 0 이면 항상 직구. 레인이 짧아(스폰~핀 ≈ 7.8u) 너무 크면 끝 스핀이 거터로 직행. Play 모드에서 조정.")]
        [SerializeField] float curveStrength = 0.22f;

        [Tooltip("이 전진속도(z) 미만이면 커브 적용 중단 — 거의 멈춘 공이 옆으로 미끄러지는 현상 방지.")]
        [SerializeField] float curveMinForwardSpeed = 0.2f;

        // BallSpawnPoint 의 시작 위치 미발견 시 사용하는 fallback. Awake 에서 한 번 캐싱하여
        // 매 리셋마다 GameObject.Find 호출을 피한다.
        private static readonly Vector3 FallbackSpawnPosition = new Vector3(0f, 0.15f, 0.5f);

        /// <summary>리셋 후 위치 검증 허용 오차 (단위: 유닛). 이 값 이상 벗어나면 재시도.</summary>
        private const float RESET_POSITION_TOLERANCE = 0.05f;
        /// <summary>위치 검증 실패 시 최대 재시도 횟수.</summary>
        private const int MAX_RESET_RETRIES = 3;

        private Rigidbody rb;
        private BallAimer ballAimer;
        private PowerGaugeUI powerGauge;
        private SpinGaugeUI spinGauge;
        /// <summary>이번 투구에 확정된 회전값 [-1(좌)~+1(우)]. 발사 시 캐싱, ResetBall 에서 0 으로 리셋.</summary>
        private float confirmedSpin = 0f;
        private GameStateManager stateManager;
        private InputController inputController;
        private Transform spawnPoint;
        private bool hasLaunched = false;
        /// <summary>최종 폴백(코루틴) 진행 중 여부. 중복 실행 방지.</summary>
        private bool isForceResetting = false;
        /// <summary>이번 투구에서 OnFirstPinContact 가 이미 발화되었는지. ResetBall() 에서 리셋.</summary>
        private bool hasPlayedPinHitThisThrow = false;
        /// <summary>이번 투구에서 OnEnteredGutter 가 이미 발화되었는지. ResetBall() 에서 리셋.</summary>
        private bool hasEnteredGutterThisThrow = false;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            var spawnGo = GameObject.Find("BallSpawnPoint");
            spawnPoint = spawnGo != null ? spawnGo.transform : null;
            if (spawnPoint == null)
                Debug.LogWarning($"[Ball] BallSpawnPoint 미발견 — fallback 위치 {FallbackSpawnPosition} 사용");
        }

        void Start()
        {
            stateManager    = GameStateManager.Instance;
            inputController = InputController.Instance;
            ballAimer       = GetComponent<BallAimer>();
            // PowerGaugeUI / SpinGaugeUI는 비활성 자식까지 포함해 탐색
            powerGauge = FindFirstObjectByType<PowerGaugeUI>(FindObjectsInactive.Include);
            spinGauge  = FindFirstObjectByType<SpinGaugeUI>(FindObjectsInactive.Include);

            stateManager.OnStateChanged += HandleStateChanged;
        }

        private void HandleStateChanged(GameState prev, GameState next)
        {
            // AimingPosition 진입 시 위치 리셋은 GameManager.BeginGame / ThrowTransitionController
            // 가 명시적으로 호출한다 (이중 호출 방지 — 단일 리셋 경로 유지).
            if (next == GameState.Rolling)
                ExecuteLaunch();
        }

        private void ExecuteLaunch()
        {
            if (hasLaunched) return;
            hasLaunched = true;

            Vector3 launchPos = ballAimer.ConfirmedPosition;
            transform.position = launchPos;

            float force = powerGauge.ConfirmedNormalized;
            confirmedSpin = spinGauge != null ? spinGauge.ConfirmedSpin : 0f;
            Launch(launchPos, force);

            Debug.Log($"[Ball] 발사! 위치: {launchPos.x:F2}, 세기: {force * 100:F1}%, 회전: {confirmedSpin:F2}");
            // Scoring 전이는 GameManager 가 PhysicsSettleDetector.OnSettled 를 받아 처리한다.
        }

        public void Launch(Vector3 startPos, float normalizedForce)
        {
            transform.position = startPos;
            float force = Mathf.Lerp(minForce, maxForce, normalizedForce);
            rb.AddForce(Vector3.forward * force, ForceMode.Impulse);
        }

        public bool IsRolling => rb.linearVelocity.magnitude > 0.05f;

        public void ResetBall(Vector3 position)
        {
            // 1) 운동 상태 클리어 — 동적 일 때만. kinematic 상태에서 velocity setter 호출 시 Unity 경고.
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // 2) 물리 시뮬레이션 차단 — CCD / Interpolation / 중력 영향 0.
            rb.isKinematic = true;

            // 3) Transform 위치·회전 갱신 (kinematic 상태에서 안전).
            transform.position = position;
            transform.rotation = Quaternion.identity;

            // 4) Transform → 물리 엔진 명시적 sync.
            //    Unity 6 기본값 autoSyncTransforms=false 환경에서 이 호출이 없으면
            //    Rigidbody.position 이 stale 한 채 dynamic 으로 복귀하여 잘못된 위치에서 시뮬레이션.
            Physics.SyncTransforms();

            // 5) 누적 force / contact buffer / Interpolation 의 previousPosition 등 내부 상태 정리.
            //    이전 버전의 y 드리프트 회귀 원인 — Sleep() 누락으로 잔여 상태가 매 리셋마다 누적됨.
            rb.Sleep();

            // 6) 동적 복귀 — 깨끗한 상태로 시뮬레이션 재개.
            rb.isKinematic = false;
            hasLaunched = false;

            // 사운드/이펙트 1회 발화 플래그 리셋 — 다음 투구에서 재발화 가능 (조건 3: 리스폰 시 재생 가능).
            hasPlayedPinHitThisThrow = false;
            hasEnteredGutterThisThrow = false;

            // 회전값 리셋 — 다음 투구는 새 스핀 게이지 확정 전까지 직구.
            confirmedSpin = 0f;
        }

        /// <summary>
        /// 리셋 후 실제 위치가 목표 위치와 허용 오차 이내인지 검증한다.
        /// </summary>
        /// <param name="targetPosition">의도한 스폰 위치.</param>
        /// <returns>허용 오차 이내이면 true, 아니면 false.</returns>
        private bool ValidateResetPosition(Vector3 targetPosition)
        {
            float distance = Vector3.Distance(transform.position, targetPosition);
            return distance <= RESET_POSITION_TOLERANCE;
        }

        /// <summary>
        /// 모든 재시도 실패 시 실행하는 최종 폴백.
        /// kinematic 상태로 강제 배치한 뒤 1프레임 대기 후 dynamic 복귀하여
        /// 물리 엔진이 안정된 상태에서 시뮬레이션을 재개한다.
        /// </summary>
        private System.Collections.IEnumerator ForceResetCoroutine(Vector3 targetPosition)
        {
            isForceResetting = true;

            // kinematic 으로 완전 고정
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            transform.position = targetPosition;
            transform.rotation = Quaternion.identity;
            Physics.SyncTransforms();
            rb.Sleep();

            Debug.LogWarning($"[Ball] 최종 폴백 — kinematic 강제 배치 후 1프레임 대기 (위치: {targetPosition})");

            // 1프레임 대기 — 물리 엔진이 Transform 변경을 완전히 반영할 시간 확보.
            yield return null;

            // 대기 후에도 위치가 어긋나면 한 번 더 강제 보정
            transform.position = targetPosition;
            Physics.SyncTransforms();
            rb.Sleep();

            // dynamic 복귀
            rb.isKinematic = false;
            hasLaunched = false;
            isForceResetting = false;

            if (ValidateResetPosition(targetPosition))
                Debug.Log($"[Ball] 최종 폴백 성공 — 위치 검증 통과 (오차: {Vector3.Distance(transform.position, targetPosition):F4})");
            else
                Debug.LogError($"[Ball] 최종 폴백 후에도 위치 불일치! 실제: {transform.position}, 목표: {targetPosition}");
        }

        /// <summary>
        /// 스폰 위치로 공을 리셋한다. 리셋 후 위치를 검증하고,
        /// 바닥 관통 등으로 위치가 어긋났을 경우 최대 <see cref="MAX_RESET_RETRIES"/>회 재시도한다.
        /// 모든 재시도 실패 시 코루틴 기반 최종 폴백을 실행한다.
        /// </summary>
        public void ResetToStartPosition()
        {
            // 최종 폴백 코루틴이 진행 중이면 중복 호출 방지
            if (isForceResetting)
            {
                Debug.LogWarning("[Ball] 최종 폴백 코루틴 진행 중 — ResetToStartPosition 호출 무시");
                return;
            }

            Vector3 resetPos = spawnPoint != null ? spawnPoint.position : FallbackSpawnPosition;

            // 첫 시도
            ResetBall(resetPos);

            if (ValidateResetPosition(resetPos))
                return; // 정상 — 추가 작업 불필요

            // 재시도 루프
            for (int attempt = 1; attempt <= MAX_RESET_RETRIES; attempt++)
            {
                Debug.LogWarning($"[Ball] 리셋 위치 검증 실패 (시도 {attempt}/{MAX_RESET_RETRIES}) — " +
                                 $"실제: {transform.position}, 목표: {resetPos}, " +
                                 $"오차: {Vector3.Distance(transform.position, resetPos):F4}");
                ResetBall(resetPos);

                if (ValidateResetPosition(resetPos))
                {
                    Debug.Log($"[Ball] 재시도 {attempt}회 만에 리셋 성공");
                    return;
                }
            }

            // 모든 즉시 재시도 실패 → 코루틴 기반 최종 폴백 (1프레임 대기)
            Debug.LogWarning($"[Ball] {MAX_RESET_RETRIES}회 재시도 모두 실패 — 최종 폴백 코루틴 시작");
            StartCoroutine(ForceResetCoroutine(resetPos));
        }

        // 거터 진입 판정 — |x| > 0.533 (레인 절반 폭 + 약간의 여유).
        // 효과음 후크는 OnEnteredGutter 이벤트(아래 Update 폴링) 로 노출되며, 점수 시스템 연계는 추후 Phase.
        public bool IsInGutter => Mathf.Abs(transform.position.x) > 0.533f;

        /// <summary>
        /// 이번 투구에서 공이 거터 영역에 진입한 적이 있는지. <see cref="ResetBall"/> 에서 리셋된다.
        /// 거터 판정(거터 진입 + 0핀)에 <see cref="ThrowTransitionController"/> 가 사용한다.
        /// </summary>
        public bool EnteredGutterThisThrow => hasEnteredGutterThisThrow;

        void Update()
        {
            // Rolling 상태에서만 거터 진입 폴링. Aiming/Scoring/Ready 등은 무시.
            // 폴링 비용은 transform.position.x 한 번 + 분기 — 매 프레임 부담 없음.
            if (hasEnteredGutterThisThrow) return;
            if (stateManager == null || stateManager.CurrentState != GameState.Rolling) return;
            if (!IsInGutter) return;

            hasEnteredGutterThisThrow = true;
            Debug.Log($"[Ball] 거터 진입 감지 (x={transform.position.x:F2})");
            OnEnteredGutter?.Invoke();
        }

        // 커브 적용 — Rolling 동안 전진속도에 비례한 측면 힘을 가해 공이 호를 그리며 휘게 한다.
        // 측면 힘이 전진속도에 비례하므로 빠른 공일수록 굴절이 균일하게 유지되고, 공이 멈추면 자연히 0 으로 수렴.
        void FixedUpdate()
        {
            if (stateManager == null) return;
            if (stateManager.CurrentState != GameState.Rolling) return;
            if (rb.isKinematic) return;
            if (Mathf.Approximately(confirmedSpin, 0f)) return;

            float forwardSpeed = rb.linearVelocity.z;
            if (forwardSpeed < curveMinForwardSpeed) return;

            rb.AddForce(Vector3.right * confirmedSpin * curveStrength * forwardSpeed, ForceMode.Force);
        }

        void OnCollisionEnter(Collision collision)
        {
            // 핀 충돌음 1회 발화 게이트 (조건 1·2).
            if (hasPlayedPinHitThisThrow) return;
            if (collision.collider.GetComponent<Pin>() == null) return;

            hasPlayedPinHitThisThrow = true;
            Debug.Log($"[Ball] 첫 핀 접촉 (대상: {collision.collider.name})");
            OnFirstPinContact?.Invoke();
        }

        void OnDestroy()
        {
            if (stateManager != null)
                stateManager.OnStateChanged -= HandleStateChanged;
        }
    }
}
