using UnityEngine;
using UnityEngine.InputSystem;

public class BowlingBallLauncher : MonoBehaviour
{
    [Header("Launch Settings")]
    public float launchForce = 10f;

    private Rigidbody rb;
    [HideInInspector] public bool launched = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
    }

    void Update()
    {
        if (!launched && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Launch();
        }
    }

    void Launch()
    {
        launched = true;
        rb.isKinematic = false;
        rb.AddForce(Vector3.forward * launchForce, ForceMode.Impulse);
    }
}
