using UnityEngine;

namespace BowlingGame
{
    /// <summary>
    /// 조준 중 공의 예상 경로를 레인 위에 선(직구)/곡선(커브)으로 그려주는 시각 가이드.
    /// 접근성 설정 <c>SaveData.aimGuideEnabled</c> 가 ON 일 때만 표시된다.
    /// </summary>
    /// <remarks>
    /// 표시 상태별 동작:
    /// <list type="bullet">
    ///   <item>AimingPosition — 왕복 중인 공 위치를 따라 직선 (매 프레임 갱신)</item>
    ///   <item>AimingPower — 확정 위치에서 직선 (커브볼 조작 OFF 인 2단계 모드는 여기까지)</item>
    ///   <item>AimingSpin — 스핀 마커의 실시간 값 + 확정 세기로 곡선 미리보기 (마커 따라 실시간 굴절)</item>
    /// </list>
    /// 경로 예측은 <see cref="BowlingBall.PredictLateralOffset"/> 에 위임 — 커브 물리 상수의 단일 출처 유지.
    /// 예측선이 거터 영역(|x| &gt; gutterX)에 닿으면 그 지점에서 잘라 표시 — 거터행 여부를 시각적으로 알 수 있다.
    /// LineRenderer 는 Start 에서 자식 GameObject 로 자동 생성. lineMaterial 미배선 시 Sprites/Default 런타임
    /// 폴백 (Editor 에선 동작하나 빌드에서 셰이더 스트리핑 가능성 — 머티리얼 에셋 배선 권장).
    /// 설정 캐싱 정책: AimingPosition 진입 시 1회 (BallAimer.audioGuideEnabled 와 동일 — 변경은 다음 조준부터 반영).
    /// </remarks>
    public class AimGuide : MonoBehaviour
    {
        private const string LogPrefix = "[AimGuide]";

        [Header("References (비우면 자동 탐색)")]
        [SerializeField, Tooltip("예측 대상 공. 비우면 같은 GameObject → 씬 전체 순서로 자동 탐색.")]
        private BowlingBall ball;
        [SerializeField, Tooltip("실시간 회전값 출처 (AimingSpin 중 곡선 갱신). 비우면 자동 탐색 (비활성 포함).")]
        private SpinGaugeUI spinGauge;
        [SerializeField, Tooltip("확정 세기 출처 (커브 크기가 공 속도에 반비례). 비우면 자동 탐색 (비활성 포함).")]
        private PowerGaugeUI powerGauge;

        [Header("Line 형태")]
        [SerializeField, Tooltip("가이드 선 머티리얼. 비우면 Sprites/Default 런타임 폴백 — 빌드 스트리핑 위험이 있으니 에셋 배선 권장 (예: URP/Unlit 흰색).")]
        private Material lineMaterial;
        [SerializeField, Tooltip("가이드 종점 z (핀 데크). 스폰 z≈0.5 + 레인 길이 ≈7.8u 기준.")]
        private float guideEndZ = 8.3f;
        [SerializeField, Tooltip("경로 샘플 수. 곡선의 부드러움 (직선일 땐 2개면 충분하지만 통일 사용).")]
        [Range(2, 64)] private int samples = 24;
        [SerializeField, Tooltip("선을 그릴 높이(y). 레인 윗면에서 살짝 띄워 z-fighting 방지.")]
        private float lineY = 0.03f;
        [SerializeField, Tooltip("선 시작 폭 (공 쪽).")]
        private float widthStart = 0.08f;
        [SerializeField, Tooltip("선 끝 폭 (핀 쪽). 시작보다 좁혀 원근감.")]
        private float widthEnd = 0.03f;
        [SerializeField, Tooltip("거터 판정 x 임계. BowlingBall.IsInGutter 의 0.533 과 동일하게 유지할 것.")]
        private float gutterX = 0.533f;

        private GameStateManager stateManager;
        private LineRenderer line;
        private Vector3[] points;
        // AimingPosition 진입 시 SaveSystem.Load 로 1회 캐싱 — 매 프레임 I/O 방지.
        private bool guideEnabled;
        private bool isVisible;

        void Start()
        {
            if (ball == null) ball = GetComponent<BowlingBall>();
            if (ball == null) ball = FindFirstObjectByType<BowlingBall>();
            if (ball == null)
            {
                Debug.LogError($"{LogPrefix} BowlingBall 미발견 — 가이드 비활성");
                enabled = false;
                return;
            }

            // 두 게이지 모두 평상시 SetActive(false) 상태이므로 비활성 포함 탐색.
            if (spinGauge == null)  spinGauge  = FindFirstObjectByType<SpinGaugeUI>(FindObjectsInactive.Include);
            if (powerGauge == null) powerGauge = FindFirstObjectByType<PowerGaugeUI>(FindObjectsInactive.Include);

            stateManager = GameStateManager.Instance;
            if (stateManager == null)
            {
                Debug.LogError($"{LogPrefix} GameStateManager 미발견 — 가이드 비활성");
                enabled = false;
                return;
            }
            stateManager.OnStateChanged += HandleStateChanged;

            points = new Vector3[samples];
            CreateLineRenderer();
            SetVisible(false);

            Debug.Log($"{LogPrefix} 초기화 완료 (spinGauge={(spinGauge != null)}, powerGauge={(powerGauge != null)})");
        }

        void OnDestroy()
        {
            if (stateManager != null) stateManager.OnStateChanged -= HandleStateChanged;
        }

        void Update()
        {
            if (!isVisible) return;
            UpdateGuide();
        }

        private void HandleStateChanged(GameState prev, GameState next)
        {
            // 조준 사이클 시작 시점에 접근성 설정 1회 캐싱 — 이후 Power/Spin 단계까지 유지.
            if (next == GameState.AimingPosition)
                guideEnabled = SaveSystem.Load().aimGuideEnabled;

            bool aiming = next == GameState.AimingPosition
                       || next == GameState.AimingPower
                       || next == GameState.AimingSpin;
            SetVisible(guideEnabled && aiming);
        }

        private void SetVisible(bool visible)
        {
            isVisible = visible;
            if (line != null) line.enabled = visible;
        }

        // 매 프레임 경로 재계산 — AimingPosition 은 공이, AimingSpin 은 마커가 움직이므로 상시 갱신 필요.
        // 비용: samples 회의 곱셈 몇 개 + SetPosition — 부담 없음.
        private void UpdateGuide()
        {
            Vector3 start = ball.transform.position;

            // 회전값: AimingSpin 진행 중에만 마커의 실시간 값. 그 외(위치/세기 단계, 2단계 모드)는 직구.
            float spin = 0f;
            float power = 0f;
            if (stateManager.CurrentState == GameState.AimingSpin)
            {
                spin  = spinGauge  != null ? spinGauge.CurrentShapedSpin    : 0f;
                power = powerGauge != null ? powerGauge.ConfirmedNormalized : 0.5f;
            }

            float totalDz = Mathf.Max(guideEndZ - start.z, 0.5f);
            points[0] = new Vector3(start.x, lineY, start.z);
            int used = 1;

            for (int i = 1; i < samples; i++)
            {
                float dz = totalDz * i / (samples - 1);
                float x = start.x + ball.PredictLateralOffset(spin, power, dz);

                // 거터 진입 — 경계에 클램프한 마지막 점을 찍고 가이드 종료 (거터행이 한눈에 보이도록).
                bool gutter = Mathf.Abs(x) > gutterX;
                if (gutter) x = Mathf.Sign(x) * gutterX;

                points[used++] = new Vector3(x, lineY, start.z + dz);
                if (gutter) break;
            }

            line.positionCount = used;
            for (int i = 0; i < used; i++)
                line.SetPosition(i, points[i]);
        }

        private void CreateLineRenderer()
        {
            var go = new GameObject("AimGuideLine");
            go.transform.SetParent(transform, worldPositionStays: false);
            line = go.AddComponent<LineRenderer>();

            line.useWorldSpace = true;
            line.positionCount = 0;
            line.startWidth = widthStart;
            line.endWidth = widthEnd;
            line.numCapVertices = 4;
            line.numCornerVertices = 2;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            // 공 쪽 진하게 → 핀 쪽 옅게 (Sprites/Default 등 버텍스 컬러 지원 셰이더에서만 그라데이션 표시).
            line.startColor = new Color(1f, 1f, 1f, 0.9f);
            line.endColor   = new Color(1f, 1f, 1f, 0.25f);

            if (lineMaterial != null)
            {
                line.material = lineMaterial;
            }
            else
            {
                var shader = Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    line.material = new Material(shader);
                    Debug.LogWarning($"{LogPrefix} lineMaterial 미배선 — Sprites/Default 런타임 폴백 사용. " +
                                     "빌드에서 셰이더가 스트리핑될 수 있으니 머티리얼 에셋 배선 권장.");
                }
                else
                {
                    Debug.LogWarning($"{LogPrefix} lineMaterial 미배선 + Sprites/Default 미발견 — 선이 마젠타로 표시될 수 있음.");
                }
            }
        }
    }
}
