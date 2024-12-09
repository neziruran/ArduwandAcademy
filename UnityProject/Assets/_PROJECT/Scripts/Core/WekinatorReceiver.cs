using System;
using UnityEngine;
using extOSC;

public class WekinatorReceiver : MonoBehaviour
{
    [SerializeField] private string address = "/wek/outputs"; // OSC address from Wekinator
    private OSCReceiver receiver;
    private LevelManager levelManager;

    void Start()
    {
        levelManager = FindObjectOfType<LevelManager>();
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

    public void HandleGesture(float output)
    {
        int gesture = Mathf.RoundToInt(output); // Convert to integer for classification

        switch (gesture)
        {
            case 1:
                Debug.Log("Gesture Detected: Left-Right Wave");
                break;
            case 2:
                Debug.Log("Gesture Detected: Up-Down Wave");
                break;
            case 3:
                Debug.Log("Gesture Detected: Circular Motion");
                break;
            default:
                Debug.Log("Gesture Detected: Unknown");
                break;
        }
    }
}
