using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SimplifySyncVersion : MonoBehaviour
{
    // Class Variables and Constants
    private string uiConsoleMessages = "";

    // GUI Elements
    public Button StartScanButton, StopScanButton;
    public TextMeshProUGUI UiConsoleText;


    // Start is called before the first frame update
    void Start()
    {
        StartScanButton.enabled = true;
        StopScanButton.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void QuitApplicationHandler()
    {
        PrintToUiConsole("Quitting the application");
        StartScanButton.enabled = false;
        StopScanButton.enabled = false;
        Application.Quit();
    }

    public void StartScanHandler()
    {
        PrintToUiConsole("Starting the scanning chain process");
        StartScanButton.enabled = false;
        StopScanButton.enabled = true;
    }

    public void StopScanHandler()
    {
        PrintToUiConsole("Aborting the scanning chain process");
        StartScanButton.enabled = true;
        StopScanButton.enabled = false;
    }

    void PrintToUiConsole(string newMessage)
    {
        uiConsoleMessages = $"[{DateTime.Now.ToLongTimeString()}] {newMessage}\n{uiConsoleMessages}";

        // Try-Catch-Finally in case there are thread issues
        try {
            UiConsoleText.text = uiConsoleMessages;
        }
        catch (Exception e) {
            Debug.Log($"Error in printing to the UI Console -> {e}");
        }
        finally {
            Debug.Log(newMessage);
        }
    }
}
