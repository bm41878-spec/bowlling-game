using System;
using UnityEngine;

namespace BowlingGame
{
    public class Pin : MonoBehaviour
    {
        [SerializeField] float fallThreshold = 45f;
        public int pinId;

        public event Action<Pin> OnPinFallen;

        private bool _fallen;
        private Rigidbody _rb;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        void Update()
        {
            if (!_fallen && IsFallen())
            {
                _fallen = true;
                OnPinFallen?.Invoke(this);
            }
        }

        public bool IsFallen() => Vector3.Angle(transform.up, Vector3.up) > fallThreshold;

        public bool IsSleeping() => _rb != null && _rb.IsSleeping();
    }
}
