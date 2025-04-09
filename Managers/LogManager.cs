using System.Collections.Generic;
using UnityEngine;

public class LogManager : BaseManager
{
    private bool isLogWindowVisible = false;
    private Vector2 logScrollPosition;
    private List<string> logMessages = new List<string>();
    private const int maxLogMessages = 100;
    private bool shouldScrollToBottom = false;

    private GUIStyle logWindowStyle;
    private GUIStyle logTextStyle;

    public override void Initialize()
    {
        // Subscribe to log message received
        Application.logMessageReceived += HandleLog;
        
        // Initialize log styles
        InitializeLogStyles();
        
        Debug.Log("LogManager initialized");
    }

    void OnDestroy()
    {
        // Unsubscribe from log message received
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        string timeStamp = System.DateTime.Now.ToString("HH:mm:ss");
        string logEntry = $"[{timeStamp}] {type}: {logString}";
        
        // Add stack trace for errors and exceptions
        if (type == LogType.Error || type == LogType.Exception)
        {
            logEntry += $"\nStack Trace: {stackTrace}";
        }

        // Add to log messages list
        logMessages.Add(logEntry);

        // Keep only the last maxLogMessages entries
        if (logMessages.Count > maxLogMessages)
        {
            logMessages.RemoveAt(0);
        }

        // Set flag to scroll to bottom on next GUI update
        shouldScrollToBottom = true;
    }

    public void ToggleLogWindow()
    {
        isLogWindowVisible = !isLogWindowVisible;
        
        if (isLogWindowVisible)
        {
            shouldScrollToBottom = true; // Auto-scroll when opening window
        }
    }

    private void InitializeLogStyles()
    {
        logWindowStyle = new GUIStyle();
        logWindowStyle.normal.background = Texture2D.grayTexture;
        logWindowStyle.normal.textColor = Color.white;
        logWindowStyle.fontSize = 14;
        logWindowStyle.padding = new RectOffset(10, 10, 10, 10);

        logTextStyle = new GUIStyle();
        logTextStyle.fontSize = 12;
        logTextStyle.normal.textColor = Color.white;
        logTextStyle.wordWrap = true;
        logTextStyle.richText = true;
    }

    void OnGUI()
    {
        if (!isLogWindowVisible) return;

        // Calculate window dimensions
        float windowWidth = 400;
        float windowHeight = 300;
        float windowX = Screen.width - windowWidth - 10;
        float windowY = Screen.height - windowHeight - 10;

        // Create semi-transparent background
        GUI.color = new Color(0, 0, 0, 0.7f);
        GUI.Box(new Rect(windowX, windowY, windowWidth, windowHeight), "");
        
        // Reset color for content
        GUI.color = Color.white;

        // Title
        GUI.Label(new Rect(windowX + 10, windowY + 5, windowWidth - 20, 20), "Log Messages", logTextStyle);

        // Calculate content height
        float totalContentHeight = 0;
        for (int i = 0; i < logMessages.Count; i++)
        {
            float messageHeight = logTextStyle.CalcHeight(new GUIContent(logMessages[i]), windowWidth - 40);
            totalContentHeight += messageHeight + 5; // 5 pixels spacing
        }

        // Scrollable log content area
        Rect viewRect = new Rect(windowX + 10, windowY + 30, windowWidth - 20, windowHeight - 40);
        Rect contentRect = new Rect(0, 0, viewRect.width - 20, totalContentHeight);

        // Begin scroll view
        if (shouldScrollToBottom)
        {
            // Set scroll position to bottom
            logScrollPosition = new Vector2(0, totalContentHeight);
            shouldScrollToBottom = false;
        }
        logScrollPosition = GUI.BeginScrollView(viewRect, logScrollPosition, contentRect);

        // Display log messages
        float currentY = 0;
        for (int i = 0; i < logMessages.Count; i++)
        {
            string message = logMessages[i];
            float messageHeight = logTextStyle.CalcHeight(new GUIContent(message), viewRect.width - 20);
            GUI.Label(new Rect(0, currentY, contentRect.width, messageHeight), message, logTextStyle);
            currentY += messageHeight + 5;
        }

        GUI.EndScrollView();

        // Close button (top right corner)
        if (GUI.Button(new Rect(windowX + windowWidth - 60, windowY + 5, 50, 20), "Close"))
        {
            isLogWindowVisible = false;
        }
    }

    public void ClearLogs()
    {
        logMessages.Clear();
        shouldScrollToBottom = true;
    }

    public bool IsLogWindowVisible() => isLogWindowVisible;
    
    public void SetLogWindowVisible(bool visible)
    {
        isLogWindowVisible = visible;
        if (isLogWindowVisible)
        {
            shouldScrollToBottom = true;
        }
    }
} 