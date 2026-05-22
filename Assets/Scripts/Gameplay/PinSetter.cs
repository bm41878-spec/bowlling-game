using System.Collections.Generic;
using UnityEngine;

namespace BowlingGame
{
    public class PinSetter : MonoBehaviour
    {
        [SerializeField] public GameObject pinPrefab;
        [SerializeField] public Transform pinSpawnRoot;

        public List<Pin> activePins = new List<Pin>();

        private Vector3[] _initialPositions;
        private Quaternion[] _initialRotations;

        void Start()
        {
            activePins.Clear();
            foreach (Transform child in pinSpawnRoot)
            {
                var pin = child.GetComponent<Pin>();
                if (pin != null) activePins.Add(pin);
            }

            _initialPositions = new Vector3[activePins.Count];
            _initialRotations = new Quaternion[activePins.Count];
            for (int i = 0; i < activePins.Count; i++)
            {
                _initialPositions[i] = activePins[i].transform.position;
                _initialRotations[i] = activePins[i].transform.rotation;
            }
        }

        public int GetFallenCount()
        {
            int count = 0;
            foreach (var pin in activePins)
                if (pin != null && pin.IsFallen()) count++;
            return count;
        }

        public bool GetAllPinsSleeping()
        {
            foreach (var pin in activePins)
                if (pin != null && !pin.IsAtRest()) return false;
            return true;
        }

        // 스페어용: 쓰러진 핀만 제거, 서 있는 핀 위치/회전 유지
        public void ResetFallenPins()
        {
            for (int i = activePins.Count - 1; i >= 0; i--)
            {
                if (activePins[i] == null || activePins[i].IsFallen())
                {
                    if (activePins[i] != null) Destroy(activePins[i].gameObject);
                    activePins.RemoveAt(i);
                }
            }
            // TODO: Phase 5 — 쓰러진 핀 위치에 pinPrefab 재소환
        }
    }
}
