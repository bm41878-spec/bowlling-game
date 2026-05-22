using System;
using System.Collections;
using UnityEngine;

namespace BowlingGame
{
    /// <summary>
    /// 공과 모든 활성 핀의 물리 시뮬레이션이 완전히 정지한 시점을 감지한다.
    /// Rolling → Scoring 상태 전이 트리거로 사용된다.
    /// </summary>
    /// <remarks>
    /// 코루틴 기반 폴링. 매 프레임마다 정지 조건을 평가하고
    /// <c>settleTime</c> 초 이상 연속 만족 시 <see cref="OnSettled"/> 발행.
    /// <c>maxWaitTime</c> 초 경과 시 강제 정지 판정(타임아웃 안전장치).
    /// </remarks>
    public class PhysicsSettleDetector : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("공의 Rigidbody. 인스펙터에서 직접 할당.")]
        private Rigidbody ballRigidbody;

        [SerializeField, Tooltip("핀 그룹 정지 판정을 위임할 PinManager.")]
        private PinManager pinManager;

        [Header("정지 판정 임계값")]
        [SerializeField, Tooltip("공 linearVelocity.sqrMagnitude 임계값")]
        private float velocityEpsilon = 0.01f;

        [SerializeField, Tooltip("공 angularVelocity.sqrMagnitude 임계값")]
        private float angularEpsilon = 0.01f;

        [Header("안정화/타임아웃")]
        [SerializeField, Tooltip("정지 조건이 연속 유지되어야 하는 최소 시간(초)")]
        private float settleTime = 0.5f;

        [SerializeField, Tooltip("이 시간 경과 시 강제 정지 판정(초)")]
        private float maxWaitTime = 10f;

        /// <summary>정지 확정 또는 타임아웃 발생 시 1회 발행.</summary>
        public event Action OnSettled;

        private Coroutine detectionRoutine;

        /// <summary>투구 직후 호출. 이미 감지 중이면 재시작한다.</summary>
        public void StartDetection()
        {
            if (ballRigidbody == null || pinManager == null)
                Debug.LogError("[SettleDetector] ballRigidbody 또는 pinManager 미할당 — 타임아웃까지 대기 후 강제 정지됩니다.");

            if (detectionRoutine != null) StopCoroutine(detectionRoutine);

            Debug.Log("[SettleDetector] 정지 감지 시작");
            detectionRoutine = StartCoroutine(DetectLoop());
        }

        /// <summary>외부에서 강제 중단 (예: 게임 종료 / 리셋). OnSettled 는 발행하지 않는다.</summary>
        public void StopDetection()
        {
            if (detectionRoutine != null)
            {
                StopCoroutine(detectionRoutine);
                detectionRoutine = null;
            }
        }

        private IEnumerator DetectLoop()
        {
            float stableTime = 0f;
            float startTime = Time.time;

            while (true)
            {
                if (IsAllStopped())
                    stableTime += Time.deltaTime;
                else
                    stableTime = 0f;

                if (stableTime >= settleTime)
                {
                    float elapsed = Time.time - startTime;
                    Debug.Log($"[SettleDetector] 정지 확정 (소요 시간: {elapsed:F1}초)");
                    detectionRoutine = null;
                    OnSettled?.Invoke();
                    yield break;
                }

                if (Time.time - startTime >= maxWaitTime)
                {
                    Debug.LogWarning($"[SettleDetector] 타임아웃 - 강제 정지 판정 ({maxWaitTime:F0}초 경과)");
                    detectionRoutine = null;
                    OnSettled?.Invoke();
                    yield break;
                }

                yield return null;
            }
        }

        private bool IsAllStopped()
        {
            if (ballRigidbody == null || pinManager == null) return false;
            if (ballRigidbody.linearVelocity.sqrMagnitude >= velocityEpsilon) return false;
            if (ballRigidbody.angularVelocity.sqrMagnitude >= angularEpsilon) return false;
            return pinManager.AreAllPinsAtRest();
        }
    }
}
