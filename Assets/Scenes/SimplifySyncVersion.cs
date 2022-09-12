using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SimplifySyncVersion : MonoBehaviour
{
    // Class Variables and Constants
    private readonly string deviceName = "Xsens DOT";
    private string deviceId;
    private readonly string batteryServiceUuid = "{15173000-4947-11e9-8646-d663bd873d93}"; // xsens dot battery service
    private readonly string batteryCharacteristicsUuid = "15173001-4947-11e9-8646-d663bd873d93";
    private Dictionary<string, Dictionary<string, string>> devices = new Dictionary<string, Dictionary<string, string>>();
    private Dictionary<string, string> characteristicNames = new Dictionary<string, string>();

    //private Dictionary<string, Dictionary<string, string>> devices;


    private string uiConsoleMessages = "";
    private string lastError;
    private bool isScanningDevices = false;
    private bool isTargetDeviceFound = false;
    private bool isScanningServices = false;
    private bool isScanningCharacteristics = false;
    private bool isSubscribed = false;
    private bool hasFoundService = false;
    private bool hasFoundCharacteristic = false;
    private int frameCount = 0;

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
        //Debug.LogWarning($"Frame {frameCount}");
        frameCount++;
        // Updating the status box
        if (isTargetDeviceFound) 
        {
            ScanStatusTextBox.text = $"{deviceName} Found";
        }
        if (isScanningDevices && !isTargetDeviceFound)
        {
            ScanStatusTextBox.text = $"Searching for {deviceName}";
        }
        if (isScanningServices)
        {
            ScanStatusTextBox.text = $"Searching for the Battery Service";
        }
        if (isScanningCharacteristics)
        {
            ScanStatusTextBox.text = $"Searching for the Battery Characteristic";
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
            Thread.Sleep(1000);
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
                    if (res.uuid.Equals(batteryServiceUuid))
                    {
                        PrintToUiConsole($"Selected Service - Battery -> {batteryServiceUuid}");
                        hasFoundService = true;
                    }
                }
                else if (status == BleApi.ScanStatus.FINISHED)
                {
                    isScanningServices = false;
                    PrintToUiConsole("Service Scan Completed");
                }
            } while (status == BleApi.ScanStatus.AVAILABLE);
        }

        if (hasFoundService)
        {
            hasFoundService = false;
            StartCharacteristicsScanHandler();
        }

        
        if (isScanningCharacteristics)
        {
            BleApi.Characteristic res = new BleApi.Characteristic();
            do
            {
                status = BleApi.PollCharacteristic(out res, false);
                if (status == BleApi.ScanStatus.AVAILABLE)
                {
                    //string name = res.userDescription != "no description available" ? res.userDescription : res.uuid;
                    //characteristicNames[name] = res.uuid;
                    //PrintToUiConsole($"Characteristic found -> {name}");
                    //PrintToUiConsole($"Characteristic found -> {res.userDescription}");
                    PrintToUiConsole($"Characteristic found -> {res.uuid}");
                    PrintToUiConsole($"Characteristic found -> {res.userDescription}");
                }
                else if (status == BleApi.ScanStatus.FINISHED)
                {
                    isScanningCharacteristics = false;
                    PrintToUiConsole("Characteristics Scan Completed");
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
        if (!isScanningServices)
        {
            PrintToUiConsole("Now Scanning for Services");
            PrintToUiConsole("Waiting before scanning services");
            Thread.Sleep(1000);
            BleApi.ScanServices(deviceId);
            isScanningServices = true;
        }
    }

    
    public void StartCharacteristicsScanHandler()
    {
        if (!isScanningCharacteristics)
        {
            // start new scan
            PrintToUiConsole("Now Scanning for Characteristics");
            PrintToUiConsole("Waiting before scanning for characteristics");
            PrintToUiConsole($"Device ID  -> {deviceId}");
            PrintToUiConsole($"Service ID -> {batteryServiceUuid}");
            Thread.Sleep(3000);
            BleApi.ScanCharacteristics(deviceId, batteryServiceUuid);
            isScanningCharacteristics = true;


        }
    }
    

    public void ClearUiConsoleHandler()
    {
        uiConsoleMessages = "";
        PrintToUiConsole("UI console cleared");
    }

    void PrintToUiConsole(string newMessage)
    {
        uiConsoleMessages = $"[{DateTime.Now.ToLongTimeString()}][Frame: {frameCount}] {newMessage}\n{uiConsoleMessages}";

        // Try-Catch-Finally in case there are thread issues
        try {
            UiConsoleText.text = uiConsoleMessages;
        }
        catch (Exception e) {
            Debug.Log($"Error in printing to the UI Console -> {e}");
        }
        finally {
            Debug.Log($"[Frame: {frameCount}] {newMessage}");
        }
    }
}
