using UnityEngine;
using TMPro;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;

public class UDPReceiver : MonoBehaviour
{
    [Header("Network Settings")]
    [SerializeField] private int port = 5052;
    [SerializeField] private bool enableDebugLogs = false;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI gestureText;
    [SerializeField] private TextMeshProUGUI confidenceText;

    private UdpClient _client;
    private CancellationTokenSource _cancellationTokenSource;
    private bool _isInitialized;

    private volatile string _currentGesture;
    private volatile float _currentConfidence;
    
    private async void Start()
    {
        try
        {
            await InitializeUDPListener();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to initialize UDP listener: {ex.Message}");
        }
    }

    private async Task InitializeUDPListener()
    {
        if (_isInitialized) return;

        try
        {
            _client = new UdpClient(port);
            _cancellationTokenSource = new CancellationTokenSource();
            _isInitialized = true;

            await ListenForDataAsync(_cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            Debug.LogError($"UDP initialization error: {ex.Message}");
            CleanupResources();
        }
    }

    private async Task ListenForDataAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                UdpReceiveResult result = await _client.ReceiveAsync();
                ProcessReceivedData(result.Buffer);
            }
            catch (Exception ex) when (
                ex is not ObjectDisposedException && 
                ex is not OperationCanceledException)
            {
                Debug.LogError($"Error receiving UDP data: {ex.Message}");
                await Task.Delay(1000, cancellationToken);
            }
        }
    }

    private void ProcessReceivedData(byte[] data)
    {
        try
        {
            string message = System.Text.Encoding.UTF8.GetString(data);
            string[] handsData = message.Split('|');

            if (handsData.Length >= 2)
            {
                _currentGesture = handsData[0];
                
                // Handle different number formats (both . and , as decimal separators)
                string confidenceStr = handsData[1].Replace(',', '.');
                if (float.TryParse(confidenceStr, 
                    NumberStyles.Float, 
                    CultureInfo.InvariantCulture, 
                    out float confidence))
                {
                    // Ensure confidence is within reasonable bounds (0-100)
                    _currentConfidence = Mathf.Clamp(confidence, 0f, 100f);
                }

                if (enableDebugLogs)
                {
                    Debug.Log($"Gesture: {_currentGesture}, Confidence: {_currentConfidence:F1}%");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error processing data: {ex.Message}");
        }
    }

    private void Update()
    {
        if (gestureText != null && confidenceText != null)
        {
            gestureText.SetText($"Gesture: {_currentGesture}");
            confidenceText.SetText($"Confidence: {_currentConfidence:F1}%");
        }
        
    }

    private void OnDisable()
    {
        CleanupResources();
    }

    private void OnApplicationQuit()
    {
        CleanupResources();
    }

    private void CleanupResources()
    {
        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }

        if (_client != null)
        {
            _client.Close();
            _client.Dispose();
            _client = null;
        }

        _isInitialized = false;
    }
}