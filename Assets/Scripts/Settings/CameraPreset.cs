using UnityEngine;

namespace BowlingGame
{
    /// <summary>
    /// 게임 씬 카메라의 원근 압축 프리셋. <see cref="SaveData.cameraPreset"/> 인덱스로 선택한다.
    /// </summary>
    /// <remarks>
    /// FOV 를 좁히면 피사체가 확대될 뿐 근경/원경 크기 <b>비율</b>은 변하지 않는다 — 비율을 바꾸는 것은
    /// 카메라 거리뿐이다. 따라서 두 값을 함께 움직인다: 카메라를 뒤로 빼서 원근을 압축하고,
    /// 멀어진 만큼 FOV 를 좁혀 피사체 크기를 되돌린다 (망원 렌즈와 같은 조합).
    ///
    /// ⚠️ 프리셋을 새로 추가하거나 <see cref="Preset.FieldOfView"/> 를 바꿀 때는 거리 4개
    /// (<see cref="Preset.Position"/> 의 z, <see cref="Preset.FollowOffsetZ"/>,
    /// <see cref="Preset.StopOffsetFromHeadPin"/>) 를 같은 배율로 재계산해야 한다.
    /// 배율 = tan(기준FOV/2) / tan(새FOV/2). 하나만 바꾸면 투구 중 핀이 화면을 꽉 채우거나 반대로 멀어진다.
    /// </remarks>
    public static class CameraPreset
    {
        /// <summary>카메라 시점 프리셋 1개의 값 묶음.</summary>
        public struct Preset
        {
            /// <summary>설정 드롭다운에 표시할 이름.</summary>
            public string Label;
            /// <summary>세로 FOV (도).</summary>
            public float FieldOfView;
            /// <summary>조준 단계의 카메라 위치 (CameraFollow 의 복귀 지점).</summary>
            public Vector3 Position;
            /// <summary>카메라 피치 (도, 아래로 내려다보는 각).</summary>
            public float PitchDegrees;
            /// <summary>투구 추적 중 공 뒤로 유지할 거리.</summary>
            public float FollowOffsetZ;
            /// <summary>추적 카메라가 헤드핀 앞 몇 유닛에서 멈출지.</summary>
            public float StopOffsetFromHeadPin;
            /// <summary>추적 중 카메라 높이.</summary>
            public float StopHeight;
        }

        /// <summary>미설정(=0) 일 때 쓰이는 기본 프리셋 인덱스. SaveData.cameraPreset 의 int 기본값과 일치해야 한다.</summary>
        public const int DefaultIndex = 0;

        /// <summary>인덱스 = <see cref="SaveData.cameraPreset"/>. 순서를 바꾸면 기존 save.json 의 선택이 어긋난다 — 추가는 반드시 끝에.</summary>
        public static readonly Preset[] Presets =
        {
            // A — 기본. 원근을 적당히 압축하면서 레인이 화면을 채우는 균형점.
            new Preset
            {
                Label                 = "기본 (A)",
                FieldOfView           = 30f,
                Position              = new Vector3(0f, 2.1f, -3.2f),
                PitchDegrees          = 18.5f,
                FollowOffsetZ         = 3.66f,
                StopOffsetFromHeadPin = 5.17f,
                StopHeight            = 2.4f,
            },
            // B — 망원 강조. 근경/원경 크기 차이가 더 줄지만 레인이 평면처럼 보이고 시점이 더 내려다보는 느낌.
            new Preset
            {
                Label                 = "망원 강조 (B)",
                FieldOfView           = 25f,
                Position              = new Vector3(0f, 2.6f, -4.85f),
                PitchDegrees          = 17.4f,
                FollowOffsetZ         = 4.43f,
                StopOffsetFromHeadPin = 6.25f,
                StopHeight            = 3.0f,
            },
        };

        /// <summary>저장값을 유효 인덱스로 보정한다 (범위를 벗어나면 기본값).</summary>
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
        /// 로드된 씬에서 <see cref="CameraFollow"/> 를 찾아 프리셋을 적용한다.
        /// 게임 씬이 아니면(설정/메인메뉴 단독) 대상이 없으므로 조용히 무시한다 — 호출부에서 씬 판별 불필요.
        /// </summary>
        public static void Apply(int index)
        {
            var follow = Object.FindFirstObjectByType<CameraFollow>(FindObjectsInactive.Include);
            if (follow == null) return;   // 게임 씬 미로드 — 다음 Game 씬 진입 시 CameraFollow.Start 가 자체 적용
            follow.ApplyPreset(Presets[Clamp(index)]);
        }
    }
}
