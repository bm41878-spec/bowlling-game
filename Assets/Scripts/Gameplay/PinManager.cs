using System.Collections.Generic;
using UnityEngine;

namespace BowlingGame
{
    /// <summary>
    /// 10개 Pin 인스턴스의 그룹 상태(서있음/쓰러짐/정지)와 라이프사이클(스냅샷, 제거, 리셋)을 관리.
    /// 개별 핀의 판정/리셋은 <see cref="Pin"/> 에 위임하고, 본 클래스는 집합 연산만 담당한다.
    /// </summary>
    /// <remarks>
    /// "이번 투구에서 새로 쓰러진 핀" 추적 패턴:
    /// <list type="number">
    ///   <item>투구 직전 <see cref="SnapshotBeforeThrow"/> 호출 — 그 시점에 서있던 핀들을 내부에 저장</item>
    ///   <item>투구 종료 후 <see cref="GetNewlyFallenCount"/> 호출 — 스냅샷 핀 중 쓰러진 수 반환</item>
    /// </list>
    /// 1구에서 일부 핀이 비활성화(<see cref="RemoveFallenPins"/>)된 상태에서 2구를 굴려도
    /// 1구에서 쓰러진 핀은 스냅샷에 없으므로 중복 카운트되지 않는다.
    /// </remarks>
    public class PinManager : MonoBehaviour
    {
        [Tooltip("인스펙터에서 10개 핀을 직접 할당하거나, 비워두면 Awake 에서 자식 오브젝트로부터 자동 수집한다.")]
        [SerializeField] private List<Pin> pins = new List<Pin>();

        // 직전 투구 시작 시점에 '서있던' 핀들의 스냅샷.
        private List<Pin> preThrowStandingPins = new List<Pin>();

        void Awake()
        {
            // 인스펙터에 할당이 없으면 자식에서 자동 수집 (비활성 포함)
            if (pins == null || pins.Count == 0)
                pins = new List<Pin>(GetComponentsInChildren<Pin>(includeInactive: true));

            pins.RemoveAll(p => p == null);
            pins.Sort((a, b) => a.pinId.CompareTo(b.pinId));
        }

        // ---------- 상태 조회 ----------

        /// <summary>활성 상태이며 쓰러지지 않은 핀들.</summary>
        public List<Pin> GetStandingPins()
        {
            var result = new List<Pin>(pins.Count);
            foreach (var p in pins)
                if (p != null && p.gameObject.activeSelf && !p.IsFallen())
                    result.Add(p);
            return result;
        }

        /// <summary>활성 상태이며 쓰러진 핀들.</summary>
        public List<Pin> GetFallenPins()
        {
            var result = new List<Pin>(pins.Count);
            foreach (var p in pins)
                if (p != null && p.gameObject.activeSelf && p.IsFallen())
                    result.Add(p);
            return result;
        }

        public int CountStanding() => GetStandingPins().Count;
        public int CountFallen()   => GetFallenPins().Count;

        // ---------- 이번 투구에서 새로 쓰러진 핀 추적 ----------

        /// <summary>투구 시작 직전에 호출. 현재 서있는 핀들을 내부에 저장한다.</summary>
        public void SnapshotBeforeThrow()
        {
            preThrowStandingPins = GetStandingPins();
            Debug.Log($"[PinManager] 투구 전 서있는 핀 {preThrowStandingPins.Count}개: [{JoinIds(preThrowStandingPins)}]");
        }

        /// <summary>
        /// 스냅샷 시점에 서있던 핀 중 현재 쓰러졌거나 비활성화된 핀 수를 반환.
        /// (RecordThrow 에 전달할 값)
        /// </summary>
        public int GetNewlyFallenCount()
        {
            var newlyFallen = new List<Pin>();
            foreach (var p in preThrowStandingPins)
            {
                if (p == null) continue;
                // 비활성화 = 외부에서 제거된 케이스, 쓰러짐 = 물리로 넘어진 케이스 — 둘 다 카운트
                if (!p.gameObject.activeSelf || p.IsFallen())
                    newlyFallen.Add(p);
            }
            Debug.Log($"[PinManager] 이번 투구에서 쓰러진 핀 {newlyFallen.Count}개: [{JoinIds(newlyFallen)}]");
            return newlyFallen.Count;
        }

        // ---------- 라이프사이클 ----------

        /// <summary>현재 쓰러진 핀들을 비활성화 (1구 → 2구 전환).</summary>
        public void RemoveFallenPins()
        {
            foreach (var p in pins)
            {
                if (p == null) continue;
                if (p.gameObject.activeSelf && p.IsFallen())
                    p.SetActive(false);
            }
        }

        /// <summary>모든 핀을 활성화하고 초기 위치/회전으로 복원. 프레임 시작 시 호출.</summary>
        public void ResetAllPins()
        {
            foreach (var p in pins)
            {
                if (p == null) continue;
                p.SetActive(true);
                p.ResetToInitialState();
            }
            preThrowStandingPins.Clear();
            Debug.Log("[PinManager] 전체 핀 리셋 완료");
        }

        /// <summary>활성 핀들이 모두 정지 상태인지 (투구 종료 판정용).</summary>
        public bool AreAllPinsAtRest()
        {
            foreach (var p in pins)
            {
                if (p == null) continue;
                if (!p.gameObject.activeSelf) continue;
                if (!p.IsAtRest()) return false;
            }
            return true;
        }

        // ---------- helpers ----------

        private static string JoinIds(List<Pin> list)
        {
            if (list == null || list.Count == 0) return "";
            var ids = new int[list.Count];
            for (int i = 0; i < list.Count; i++) ids[i] = list[i].pinId;
            return string.Join(",", ids);
        }
    }
}
