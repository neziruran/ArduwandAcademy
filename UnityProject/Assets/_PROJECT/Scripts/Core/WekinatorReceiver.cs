using System;
using DG.Tweening;
using UnityEngine;
using extOSC;

public class WekinatorReceiver : MonoBehaviour
{
    [SerializeField] private string address = "/wek/outputs"; // OSC address from Wekinator
    [SerializeField] private ParticleSystem trailFx, puffFX;
    private OSCReceiver receiver;

    void Start()
    {
        receiver = GetComponent<OSCReceiver>();

        if (receiver == null)
            gameObject.AddComponent<OSCReceiver>();

        // Bind the OSC address to a handler function
        receiver.Bind(address, OnReceiveMessage);
    }

    private void OnReceiveMessage(OSCMessage message)
    {
        if (message.Values.Count > 0)
        {
            // Get the output value from Wekinator
            float wekinatorOutput = message.Values[0].FloatValue;
            Debug.Log($"Wekinator Output: {wekinatorOutput}");

            // Handle gestures based on Wekinator's output
            HandleGesture(wekinatorOutput);
            EventManager.OnWandPerformed();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            MoveInCircle();
        }
    }

    public void HandleGesture(float output)
    {
        int gesture = Mathf.RoundToInt(output); // Convert to integer for classification

        // Reset positions to center before starting any movement
        ResetEffects();

        switch (gesture)
        {
            case 1:
                Debug.Log("Gesture Detected: Left-Right Wave");
                CreateWave(Vector3.left * 1.5f, Vector3.right * 1.5f);
                break;

            case 2:
                Debug.Log("Gesture Detected: Up-Down Wave");
                CreateWave(Vector3.up * 1.5f, Vector3.down * 1.5f);
                break;

            case 3:
                Debug.Log("Gesture Detected: Circular Motion");
                MoveInCircle(); // Circular motion is already optimized
                break;

            default:
                Debug.Log("Gesture Detected: Unknown");
                break;
        }
    }

    // Helper to reset effects to the initial state
    private void ResetEffects()
    {
        puffFX.transform.position = Vector3.zero;
        trailFx.transform.position = Vector3.zero;
    }

    // Helper to create a wave motion
    private void CreateWave(Vector3 firstPosition, Vector3 secondPosition)
    {
        float moveDuration = 0.25f;
        float resetDuration = 0.1f;

        trailFx.transform.DOMove(firstPosition, moveDuration).OnComplete(() =>
        {
            trailFx.transform.DOMove(secondPosition, moveDuration).OnComplete(() =>
            {
                trailFx.transform.DOMove(Vector3.zero, resetDuration);
                puffFX.Play();
            });
        });
    }

    void MoveInCircle()
    {
        // Create a sequence for the circular motion
        Sequence seq = DOTween.Sequence();

        // Number of points around the circle for smoothness
        int points = 36; // Higher means smoother motion
        float angleStep = 360f / points;

        // Loop through the points on the circle
        for (int i = 0; i <= points; i++)
        {
            float angle = angleStep * i; // Calculate the angle
            float radian = angle * Mathf.Deg2Rad; // Convert angle to radians
            float radius = 1f;
            float duration = .5f;

            // Calculate the position based on the radius and angle
            Vector3 position = Vector3.zero + new Vector3(
                Mathf.Cos(radian) * radius,
                Mathf.Sin(radian) * radius,
                0);

            // Append the movement to the sequence
            seq.Append(trailFx.transform.DOMove(position, duration / points).SetEase(Ease.Linear));
        }

        seq.OnComplete((() => { puffFX.Play(); }));
    }
}