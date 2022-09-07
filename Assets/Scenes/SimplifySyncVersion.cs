using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SimplifySyncVersion : MonoBehaviour
{
    // Class Variables and Constants
    private readonly string deviceName = "Xsens DOT";
    private string deviceId;
    private readonly string batteryServiceUuid = "15173000-4947-11e9-8646-d663bd873d93"; // xsens dot battery service
    private readonly string batteryCharacteristicsUuid = "15173001-4947-11e9-8646-d663bd873d93";
    private Dictionary<string, Dictionary<string, string>> devices = new Dictionary<string, Dictionary<string, string>>();
    //private Dictionary<string, Dictionary<string, string>> devices;


    private string uiConsoleMessages = "";
    private string lastError;
    private bool isScanningDevices = false;
    private bool isTargetDeviceFound = false;
    private bool isScanningServices = false;
    private bool isScanningCharacteristics = false;
    private bool isSubscribed = false;

    // GUI Elements
    public Button StartScanButton, StopScanButton, clearUiButton;
    public TextMeshProUGUI UiConsoleText, ScanStatusTextBox;


    // Start is called before the first frame update
    void Start()
    {
        //StartScanButton.enabled = true;
        //StopScanButton.enabled = false;
        ScanStatusTextBox.text = "";
    }

    // Update is called once per frame
    void Update()
    {
        // Updating the status box
        if (isTargetDeviceFound)
        {
            ScanStatusTextBox.text = $"{deviceName} Found";
        }
        else if (isScanningDevices && !isTargetDeviceFound)
        {
            ScanStatusTextBox.text = $"Searching for {deviceName}";
        }
        else
        {
            ScanStatusTextBox.text = "";
        }

        // If the device is scanning, poll through the found devices
        BleApi.ScanStatus status;
        if (isScanningDevices)
        {
            BleApi.DeviceUpdate res = new BleApi.DeviceUpdate();
            do
            {
                status = BleApi.PollDevice(ref res, false);
                
                if (status == BleApi.ScanStatus.AVAILABLE)
                {

                    if (!devices.ContainsKey(res.id))
                    {
                        devices[res.id] = new Dictionary<string, string>() {
                            { "name", "" },
                            { "isConnectable", "False" }
                        };
                    }

                    if (res.nameUpdated)
                    {
                        devices[res.id]["name"] = res.name;
                    }

                    if (res.isConnectableUpdated)
                    {
                        devices[res.id]["isConnectable"] = res.isConnectable.ToString();
                    }
                        
                    // consider only devices which have a name and which are connectable
                    if (devices[res.id]["name"] != "" && devices[res.id]["isConnectable"] == "True")
                    {
                        if (devices[res.id]["name"].Equals(deviceName))
                        {
                            isTargetDeviceFound = true;
                            deviceId = res.id; // save the ID of the Xsens DOT for future ref
                            PrintToUiConsole($"Selected Device ID -> {res.id}");
                        }
                        // add new device to list
                        //GameObject g = Instantiate(deviceScanResultProto, scanResultRoot);
                        //g.name = res.id;
                        //g.transform.GetChild(0).GetComponent<Text>().text = devices[res.id]["name"];
                        //g.transform.GetChild(1).GetComponent<Text>().text = res.id;

                        PrintToUiConsole("Name: " + devices[res.id]["name"] + " ID: " + res.id);
                    }
                }
                else if (status == BleApi.ScanStatus.FINISHED)
                {
                    isScanningDevices = false;
                    PrintToUiConsole("*** Scanning Complete ***");
                }
  
            } while (status == BleApi.ScanStatus.AVAILABLE);
        }

        if (isScanningDevices && isTargetDeviceFound)
        {
            StopScanHandler();
            StartServiceScanHandler();
        }

        if (isScanningServices)
        {
            BleApi.Service res = new BleApi.Service();
            do
            {
                status = BleApi.PollService(out res, false);
                if (status == BleApi.ScanStatus.AVAILABLE)
                {
                    PrintToUiConsole($"Service found -> {res.uuid}");
                }
                else if (status == BleApi.ScanStatus.FINISHED)
                {
                    isScanningServices = false;
                    PrintToUiConsole("Service Scan Completed");
                }
            } while (status == BleApi.ScanStatus.AVAILABLE);
        }

        {
            // log potential errors
            BleApi.ErrorMessage res = new BleApi.ErrorMessage();
            BleApi.GetError(out res);
            if (lastError != res.msg)
            {
                Debug.LogError(res.msg);
                //errorText.text = res.msg;
                lastError = res.msg;
            }
        }
    }

    public void QuitApplicationHandler()
    {
        PrintToUiConsole("Quitting the application");
        StartScanButton.enabled = false;
        StopScanButton.enabled = false;
        BleApi.Quit();
        Application.Quit();
    }

    public void StartScanHandler()
    {
        if (!isScanningDevices)
        {
            PrintToUiConsole("Starting the scanning chain process");
            
            BleApi.StartDeviceScan();
            isScanningDevices = true;
        }
        else
        {
            PrintToUiConsole("Scanning is already ongoing, cannot start another");
        }
    }

    public void StopScanHandler()
    {
        if (isScanningDevices)
        {
            PrintToUiConsole("Aborting the scanning chain process");

            isScanningDevices = false;
            BleApi.StopDeviceScan();
        }
        else
        {
            PrintToUiConsole("Scanning process is already stopped");
        }

    }

    public void StartServiceScanHandler()
    {
        isScanningServices = true;
        BleApi.ScanServices(deviceId);
    }

    public void ClearUiConsoleHandler()
    {
        uiConsoleMessages = "";
        PrintToUiConsole("UI console cleared");
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
