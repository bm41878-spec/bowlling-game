using UnityEngine;
using UnityEngine.InputSystem;

public class BowlingResetManager : MonoBehaviour
{
    [Header("References")]
    public GameObject bowlingBall;
    public GameObject bowlingPinsParent;

    private Vector3 ballInitialPosition;
    private Quaternion ballInitialRotation;

    private Vector3[] pinInitialPositions;
    private Quaternion[] pinInitialRotations;

    void Start()
    {
        // 볼링공 초기 위치 저장
        ballInitialPosition = bowlingBall.transform.position;
        ballInitialRotation = bowlingBall.transform.rotation;

        // 핀 초기 위치 저장
        int pinCount = bowlingPinsParent.transform.childCount;
        pinInitialPositions = new Vector3[pinCount];
        pinInitialRotations = new Quaternion[pinCount];

        for (int i = 0; i < pinCount; i++)
        {
            Transform pin = bowlingPinsParent.transform.GetChild(i);
            pinInitialPositions[i] = pin.position;
            pinInitialRotations[i] = pin.rotation;
        }
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            ResetAll();
        }
    }

    void ResetAll()
    {
        ResetBall();
        ResetPins();
    }

    void ResetBall()
    {
        var rb = bowlingBall.GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        bowlingBall.transform.position = ballInitialPosition;
        bowlingBall.transform.rotation = ballInitialRotation;

        // BowlingBallLauncher의 launched 플래그 초기화
        var launcher = bowlingBall.GetComponent<BowlingBallLauncher>();
        if (launcher != null)
            launcher.launched = false;
    }

    void ResetPins()
    {
        int pinCount = bowlingPinsParent.transform.childCount;
        for (int i = 0; i < pinCount; i++)
        {
            Transform pin = bowlingPinsParent.transform.GetChild(i);
            var rb = pin.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            pin.position = pinInitialPositions[i];
            pin.rotation = pinInitialRotations[i];

            if (rb != null)
                rb.isKinematic = false;
        }
    }
}
