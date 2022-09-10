using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JoelAppScript : MonoBehaviour
{
    // Class Variables and Constants
    private readonly string deviceName = "Xsens DOT";
    private string deviceId;
    private readonly string batteryServiceUuid = "{15173000-4947-11e9-8646-d663bd873d93}"; // xsens dot battery service
    private readonly string batteryCharacteristicsUuid = "15173001-4947-11e9-8646-d663bd873d93";
    private Dictionary<string, Dictionary<string, string>> devices = new Dictionary<string, Dictionary<string, string>>();
    private Dictionary<string, string> characteristicNames = new Dictionary<string, string>();

    // GUI Elements
    public Button StartScanButton, KillProcessButton, clearUiButton;
    public TextMeshProUGUI UiConsoleText, ScanStatusTextBox;

    // GUI Related
    private string uiConsoleMessages;
    private ConcurrentQueue<string> uiConsoleMessagesList = new ConcurrentQueue<string>();

    void Start()
    {
        ScanStatusTextBox.text = "";
        UiConsoleText.text = "";
        KillProcessButton.enabled = true;
        StartScanButton.enabled = true;
        clearUiButton.enabled = true;
    }


    void Update()
    {
        // Update the UI from concurrent queue
        PrintToConsoleBurst();

    }

    public void StartProcessHandle()
    {
        uiConsoleMessagesList.Enqueue("Starting Process");
    }

    public void KillProcessHandle()
    {
        uiConsoleMessagesList.Enqueue("Stopping Process");
    }

    public void ClearUiConsoleHandle()
    {
        uiConsoleMessages = "";
    }

    public void QuitApplicationButton() 
    {
        Application.Quit();
    }

    void PrintToConsoleBurst()
    {
        while (!uiConsoleMessagesList.IsEmpty)
        {
            string msg;
            uiConsoleMessagesList.TryDequeue(out msg);
            uiConsoleMessages = $"[{DateTime.Now.ToLongTimeString()}] {msg}\n{uiConsoleMessages}";
            Debug.Log(msg);
            UiConsoleText.text = uiConsoleMessages;
        }
    }
}
