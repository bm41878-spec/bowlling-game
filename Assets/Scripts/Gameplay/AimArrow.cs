using UnityEngine;

namespace BowlingGame
{
    /// <summary>
    /// 조작 방식 C(방향만) 전용 조준 화살표. 공을 중심으로 호를 그리며 왕복하는 블록 화살표를
    /// 레인 위에 그린다. 메시는 런타임에 생성하므로 별도 아트 에셋이 필요 없다.
    /// </summary>
    /// <remarks>
    /// 배치 규칙 — 화살표는 <b>공 뒤쪽</b>(핀 반대편)에 서서 공을 관통하는 방향을 가리킨다.
    /// 즉 공이 호의 중심이고, 화살표는 반지름 <see cref="orbitRadius"/> 위를 돈다.
    /// 조준 각도가 θ 면 화살표 위치는 <c>공 − dir(θ) × orbitRadius</c>, 방향은 <c>dir(θ)</c>.
    ///
    /// A/B 방식에서는 아무것도 하지 않는다 (<see cref="BallAimer.SingleInputAiming"/> 이 false).
    /// </remarks>
    [DefaultExecutionOrder(50)]   // BallAimer 가 각도를 갱신한 뒤에 따라 그린다
    public class AimArrow : MonoBehaviour
    {
        [Header("References (비우면 자동 탐색)")]
        [SerializeField, Tooltip("조준 방향 출처. 비우면 같은 GameObject → 씬 전체 순서로 자동 탐색.")]
        private BallAimer aimer;

        [Header("배치")]
        [SerializeField, Tooltip("공(호의 중심)에서 화살표 꼬리까지의 거리. 클수록 화살표가 공 뒤로 멀어진다. 카메라 프리셋 A 기준 화면 아래 끝이 z≈0 이라 0.55 를 넘으면 화살표가 화면 밖으로 나간다.")]
        private float orbitRadius = 0.5f;
        [SerializeField, Tooltip("화살표를 그릴 높이(y). ⚠ 레인 윗면이 y=0.05 이므로 그보다 높아야 보인다.")]
        private float arrowY = 0.055f;

        [Header("형태")]
        [SerializeField, Tooltip("몸통 절반 폭.")] private float shaftHalfWidth = 0.055f;
        [SerializeField, Tooltip("머리 절반 폭 (몸통보다 넓어야 화살표로 보인다).")] private float headHalfWidth = 0.135f;
        [SerializeField, Tooltip("몸통 길이.")] private float shaftLength = 0.24f;
        [SerializeField, Tooltip("머리 길이.")] private float headLength = 0.18f;
        [SerializeField, Tooltip("화살표 색.")] private Color arrowColor = new Color(1f, 0.85f, 0.1f, 0.95f);
        [SerializeField, Tooltip("화살표 머티리얼. 비우면 Sprites/Default 런타임 폴백 — 빌드 스트리핑 위험이 있으니 에셋 배선 권장.")]
        private Material arrowMaterial;

        private Transform arrowTransform;
        private MeshRenderer arrowRenderer;
        private GameStateManager stateManager;
        private bool isVisible;

        void Start()
        {
            if (aimer == null) aimer = GetComponent<BallAimer>();
            if (aimer == null) aimer = FindFirstObjectByType<BallAimer>();

            stateManager = GameStateManager.Instance;
            if (stateManager != null) stateManager.OnStateChanged += HandleStateChanged;

            CreateArrowMesh();
            SetVisible(false);
        }

        void OnDestroy()
        {
            if (stateManager != null) stateManager.OnStateChanged -= HandleStateChanged;
            // 부모가 없으므로 직접 정리한다 (씬 전이 시 잔존 방지).
            if (arrowTransform != null) Destroy(arrowTransform.gameObject);
        }

        private void HandleStateChanged(GameState prev, GameState next)
        {
            // C 방식의 조준 단계에서만 보인다. 확정하는 순간(Rolling) 즉시 사라진다.
            bool show = next == GameState.AimingPosition
                     && aimer != null && aimer.SingleInputAiming;
            SetVisible(show);
        }

        private void SetVisible(bool visible)
        {
            isVisible = visible;
            if (arrowRenderer != null) arrowRenderer.enabled = visible;
        }

        void LateUpdate()
        {
            if (!isVisible || aimer == null || arrowTransform == null) return;

            Vector3 dir = aimer.CurrentAimDirection;
            Vector3 center = aimer.transform.position;       // 공 = 호의 중심

            // 공 뒤쪽 호 위에 서서, 공을 관통하는 방향을 가리킨다.
            Vector3 pos = center - dir * orbitRadius;
            pos.y = arrowY;

            arrowTransform.SetPositionAndRotation(pos, Quaternion.LookRotation(dir, Vector3.up));
        }

        /// <summary>
        /// 블록 화살표(몸통 + 삼각 머리) 메시를 생성한다. +Z 를 향하고 XZ 평면에 눕는다.
        /// 양면 삼각형으로 만들어 컬링 방향에 상관없이 위에서 보인다.
        /// </summary>
        private void CreateArrowMesh()
        {
            // 부모를 두지 않는다 — 공(BowlingBall) 의 localScale 이 10 이라 자식으로 붙이면
            // 화살표가 10배로 커진다. 위치·회전은 매 프레임 월드 좌표로 직접 세팅하므로 부모가 불필요.
            var go = new GameObject("AimArrowMesh");
            arrowTransform = go.transform;

            float w = shaftHalfWidth;
            float W = headHalfWidth;
            float L = shaftLength;
            float H = headLength;

            // 꼬리(0) 기준으로 +Z 로 뻗는다.
            var verts = new[]
            {
                new Vector3(-w, 0f, 0f),      // 0 꼬리 좌
                new Vector3( w, 0f, 0f),      // 1 꼬리 우
                new Vector3(-w, 0f, L),       // 2 몸통 좌
                new Vector3( w, 0f, L),       // 3 몸통 우
                new Vector3(-W, 0f, L),       // 4 머리 좌 바깥
                new Vector3( W, 0f, L),       // 5 머리 우 바깥
                new Vector3( 0f, 0f, L + H),  // 6 촉
            };

            // 앞뒤 양면 — 컬링 설정과 무관하게 위에서 항상 보이도록.
            var tris = new[]
            {
                0, 2, 3,  0, 3, 1,   // 몸통
                4, 6, 5,             // 머리
                3, 2, 0,  1, 3, 0,   // 몸통 뒷면
                5, 6, 4,             // 머리 뒷면
            };

            var mesh = new Mesh { name = "AimArrow" };
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            arrowRenderer = go.AddComponent<MeshRenderer>();
            arrowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            arrowRenderer.receiveShadows = false;

            if (arrowMaterial != null)
            {
                arrowRenderer.material = arrowMaterial;
            }
            else
            {
                var shader = Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    arrowRenderer.material = new Material(shader) { color = arrowColor };
                }
                else
                {
                    Debug.LogWarning("[AimArrow] Sprites/Default 셰이더 미발견 — 머티리얼을 인스펙터에 배선할 것");
                }
            }

            if (arrowMaterial == null && arrowRenderer.material != null)
                arrowRenderer.material.color = arrowColor;
        }
    }
}
