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
    [SerializeField] private bool enableDebugPanel = false;
    [SerializeField] private bool hasConnected = false;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI gestureText;
    [SerializeField] private TextMeshProUGUI confidenceText;
    [SerializeField] private TextMeshProUGUI connectionStatusText;
    [SerializeField] private Canvas debugPanel;

    private UdpClient _client;
    private CancellationTokenSource _cancellationTokenSource;
    private bool _isInitialized;
    private float _connectionCheckTimer;
    private const float CONNECTION_CHECK_INTERVAL = 2f;
    private const float INITIAL_WAIT_TIME = 5f; // Time to show "Waiting..." status
    private float _initialWaitTimer;
    private DateTime _lastPacketTime;

    private volatile string _currentGesture;
    private volatile float _currentConfidence;
    
    private void Awake()
    {
        if (debugPanel != null)
        {
            debugPanel.gameObject.SetActive(enableDebugPanel);
        }
    }

    private async void Start()
    {
        _initialWaitTimer = 0f;
        UpdateConnectionStatus(ConnectionState.Waiting);
        
        try
        {
            await InitializeUDPListener();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to initialize UDP listener: {ex.Message}");
            UpdateConnectionStatus(ConnectionState.Disconnected);
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
            _lastPacketTime = DateTime.Now;

            await ListenForDataAsync(_cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            Debug.LogError($"UDP initialization error: {ex.Message}");
            CleanupResources();
            UpdateConnectionStatus(ConnectionState.Disconnected);
        }
    }

    private async Task ListenForDataAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                UdpReceiveResult result = await _client.ReceiveAsync();
                _lastPacketTime = DateTime.Now;
                UpdateConnectionStatus(ConnectionState.Connected);
                ProcessReceivedData(result.Buffer);
            }
            catch (Exception ex) when (
                ex is not ObjectDisposedException && 
                ex is not OperationCanceledException)
            {
                Debug.LogError($"Error receiving UDP data: {ex.Message}");
                if (_initialWaitTimer >= INITIAL_WAIT_TIME)
                {
                    UpdateConnectionStatus(ConnectionState.Disconnected);
                }
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
                
                string confidenceStr = handsData[1].Replace(',', '.');
                if (float.TryParse(confidenceStr, 
                    NumberStyles.Float, 
                    CultureInfo.InvariantCulture, 
                    out float confidence))
                {
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
        // Update UI elements
        if (gestureText != null && confidenceText != null)
        {
            gestureText.SetText($"Gesture: {_currentGesture}");
            confidenceText.SetText($"Confidence: {_currentConfidence:F1}%");
        }

        // Handle initial waiting period
        if (_initialWaitTimer < INITIAL_WAIT_TIME)
        {
            _initialWaitTimer += Time.deltaTime;
            if (_initialWaitTimer >= INITIAL_WAIT_TIME && connectionStatusText.text == "Waiting...")
            {
                UpdateConnectionStatus(ConnectionState.Disconnected);
            }
        }

        // Check connection status periodically
        _connectionCheckTimer += Time.deltaTime;
        if (_connectionCheckTimer >= CONNECTION_CHECK_INTERVAL)
        {
            _connectionCheckTimer = 0f;
            CheckConnectionTimeout();
        }
    }

    private void CheckConnectionTimeout()
    {
        if (!_isInitialized) return;

        // Consider connection lost if no packets received in the last 5 seconds
        TimeSpan timeSinceLastPacket = DateTime.Now - _lastPacketTime;
        if (timeSinceLastPacket.TotalSeconds > 5 && _initialWaitTimer >= INITIAL_WAIT_TIME)
        {
            UpdateConnectionStatus(ConnectionState.Disconnected);
        }
    }

    private enum ConnectionState
    {
        Connected,
        Disconnected,
        Waiting
    }

    private void UpdateConnectionStatus(ConnectionState state)
    {
        hasConnected = state == ConnectionState.Connected;

        if (connectionStatusText != null)
        {
            switch (state)
            {
                case ConnectionState.Connected:
                    connectionStatusText.text = "Connected";
                    connectionStatusText.color = Color.green;
                    break;
                case ConnectionState.Disconnected:
                    connectionStatusText.text = "Not Connected";
                    connectionStatusText.color = Color.red;
                    break;
                case ConnectionState.Waiting:
                    connectionStatusText.text = "Waiting...";
                    connectionStatusText.color = Color.white;
                    break;
            }
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
        UpdateConnectionStatus(ConnectionState.Disconnected);
    }
}