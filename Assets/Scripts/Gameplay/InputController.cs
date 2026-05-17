using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BowlingGame
{
    public class InputController : MonoBehaviour
    {
        public static InputController Instance { get; private set; }

        public event Action OnConfirmPressed;

        private InputAction confirmAction;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            confirmAction = new InputAction(type: InputActionType.Button);
            confirmAction.AddBinding("<Keyboard>/space");
            confirmAction.performed += _ => OnConfirmPressed?.Invoke();
            confirmAction.Enable();
        }

        void OnDestroy()
        {
            confirmAction?.Disable();
        }
    }
}
