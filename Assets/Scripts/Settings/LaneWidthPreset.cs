using UnityEngine;

namespace BowlingGame
{
    /// <summary>
    /// 레인 가로 폭 프리셋. <see cref="SaveData.laneWidthPreset"/> 인덱스로 선택한다.
    /// </summary>
    /// <remarks>
    /// <b>적용 시점은 게임 씬 로드 1회뿐이다</b> (<see cref="LaneWidthSetter"/> 의 Awake).
    /// 투구 도중 레인이 넓어지면 굴러가던 공·쓰러지던 핀이 순간이동하므로, 설정에서 값을 바꿔도
    /// 진행 중인 게임에는 반영하지 않고 다음 게임부터 적용한다.
    ///
    /// 구현 방식 — <c>Lane_Root</c> 의 localScale 을 건드리지 <b>않는다</b>. 부모를 x 축으로만 비균등
    /// 스케일하면 그 아래에서 회전하는 강체(쓰러지는 핀)가 찌그러진다. 대신 레인 지오메트리 자식들의
    /// <c>localPosition.x</c> / <c>localScale.x</c> 와 핀의 <c>localPosition.x</c> 를 개별로 곱한다.
    ///
    /// 씬이 로드될 때마다 원본(저작) 값에서 출발하므로 배율이 누적되지 않는다.
    /// <b>같은 씬에서 두 번 호출하면 폭이 제곱으로 늘어난다</b> — 호출 지점을 늘리지 말 것.
    /// </remarks>
    public static class LaneWidthPreset
    {
        /// <summary>레인 폭 프리셋 1개.</summary>
        public struct Preset
        {
            /// <summary>설정 드롭다운에 표시할 이름.</summary>
            public string Label;
            /// <summary>기본 폭 대비 배율.</summary>
            public float Scale;
        }

        /// <summary>미설정(=0) 일 때의 기본 프리셋 인덱스. SaveData.laneWidthPreset 의 int 기본값과 일치해야 한다.</summary>
        public const int DefaultIndex = 0;

        /// <summary>인덱스 = <see cref="SaveData.laneWidthPreset"/>. 순서 변경·중간 삽입 금지 — 추가는 끝에.</summary>
        public static readonly Preset[] Presets =
        {
            new Preset { Label = "기본 (A)",        Scale = 1.0f },
            new Preset { Label = "넓게 ×1.2 (B)",   Scale = 1.2f },
            new Preset { Label = "가장 넓게 ×1.5 (C)", Scale = 1.5f },
        };

        /// <summary>폭 배율의 영향을 받지 않는 자식 — 스폰 지점과 핀 루트는 따로 처리한다.</summary>
        private const string PinRootName   = "PinSpawnRoot";
        private const string BallSpawnName = "BallSpawnPoint";

        public static int Clamp(int index) =>
            index >= 0 && index < Presets.Length ? index : DefaultIndex;

        /// <summary>드롭다운 표시용 이름 목록.</summary>
        public static string[] Labels
        {
            get
            {
                var labels = new string[Presets.Length];
                for (int i = 0; i < Presets.Length; i++) labels[i] = Presets[i].Label;
                return labels;
            }
        }

        /// <summary>
        /// 저장된 프리셋을 현재 씬의 레인에 적용한다. 게임 씬이 아니면 <c>Lane_Root</c> 가 없으므로 조용히 무시.
        /// </summary>
        public static void Apply(int index)
        {
            float scale = Presets[Clamp(index)].Scale;

            var laneRoot = GameObject.Find("Lane_Root");
            if (laneRoot == null) return;   // 게임 씬 아님

            // 배율 1 이어도 아래 상수 동기화는 수행 — 기본값 보장.
            for (int i = 0; i < laneRoot.transform.childCount; i++)
            {
                var child = laneRoot.transform.GetChild(i);
                if (child.name == PinRootName || child.name == BallSpawnName) continue;

                var lp = child.localPosition;
                lp.x *= scale;
                child.localPosition = lp;

                var ls = child.localScale;
                ls.x *= scale;
                child.localScale = ls;
            }

            // 핀은 간격만 벌린다 — localScale 을 건드리면 핀이 옆으로 늘어난다.
            var pinRoot = laneRoot.transform.Find(PinRootName);
            if (pinRoot != null)
            {
                for (int i = 0; i < pinRoot.childCount; i++)
                {
                    var pin = pinRoot.GetChild(i);
                    var lp = pin.localPosition;
                    lp.x *= scale;
                    pin.localPosition = lp;
                }
            }

            // 폭에 연동된 런타임 상수 동기화 — 하나라도 빠지면 조준 범위나 거터 판정이 레인과 어긋난다.
            var aimer = Object.FindFirstObjectByType<BallAimer>(FindObjectsInactive.Include);
            if (aimer != null) aimer.ApplyWidthScale(scale);

            var ball = Object.FindFirstObjectByType<BowlingBall>(FindObjectsInactive.Include);
            if (ball != null) ball.ApplyWidthScale(scale);

            var guide = Object.FindFirstObjectByType<AimGuide>(FindObjectsInactive.Include);
            if (guide != null) guide.ApplyWidthScale(scale);

            Physics.SyncTransforms();
            Debug.Log($"[LaneWidth] 적용: {Presets[Clamp(index)].Label} (×{scale:F2})");
        }
    }
}
