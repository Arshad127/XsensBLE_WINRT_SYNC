using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;

public class JoelAppScript : MonoBehaviour
{
    // Class Variables and Constants
    private readonly string deviceName = "Xsens DOT";
    private string deviceId;
    private readonly string batteryServiceUuid = "{15173000-4947-11e9-8646-d663bd873d93}"; // xsens dot battery service
    private readonly string batteryCharacteristicsUuid = "15173001-4947-11e9-8646-d663bd873d93";
    //private Dictionary<string, Dictionary<string, string>> discoveredDevices = new Dictionary<string, Dictionary<string, string>>();
    private ConcurrentDictionary<string, string> discoveredDevices = new ConcurrentDictionary<string, string>();

    private Dictionary<string, string> characteristicNames = new Dictionary<string, string>();
    private Thread testingThread, deviceScanningThread, batteryThread, connectionThread;
    private JoelBLE ble;
    private JoelBLE.BLEScan scan;

    private bool deviceFound = false;
    private bool isScanningForDevice = false;
    private bool isConnectedToDevice = false;

    // GUI Elements
    public Button discoverButton, batteryStatusButton, clearUiButton, connectButton;
    public TextMeshProUGUI UiConsoleText, ScanStatusTextBox;

    // GUI Related
    private string uiConsoleMessages;
    private ConcurrentQueue<string> uiConsoleMessagesList = new ConcurrentQueue<string>();

    void Start()
    {
        ble = new JoelBLE();
        ScanStatusTextBox.text = "";
        UiConsoleText.text = "";
        batteryStatusButton.enabled = true;
        discoverButton.enabled = true;
        clearUiButton.enabled = true;
        connectButton.enabled = true;
    }


    void Update()
    {
        // GUI updates
        PrintToConsoleBurst();

    }

    public void SubscribeToBatteryHandler()
    {
        if (deviceId != null && !deviceId.Equals("-1"))
        {
            uiConsoleMessagesList.Enqueue("Connection Attempt");
            batteryThread = new Thread(GetBatteryStatus);
            batteryThread.Start();
        }
        else
        {
            uiConsoleMessagesList.Enqueue($"Cannot subscribe since {deviceName} has not been found");
        }
    }

    public void ConnectHandler()
    {
        uiConsoleMessagesList.Enqueue("Not implemented");
    }

    void ConnectBleDevice()
    {
        uiConsoleMessagesList.Enqueue("Not implemented");
    }

    void GetBatteryStatus()
    {
        bool isSuccessful = ble.SubscribeToCharacteristic(deviceId, batteryServiceUuid, batteryCharacteristicsUuid);
        if (isSuccessful)
        {
            uiConsoleMessagesList.Enqueue("Battery Subscription successful");
        }
        else
        {
            uiConsoleMessagesList.Enqueue("Battery Subscription Failed");
        }
    }

    public void ScanForDeviceHandler()
    {
        if (!isScanningForDevice)
        {
            if (!deviceFound)
            {
                uiConsoleMessagesList.Enqueue($"Scanning for {deviceName}");
                discoveredDevices.Clear();
                isScanningForDevice = true;
                deviceScanningThread = new Thread(ScanBleDevices);
                deviceScanningThread.Start();
            }
            else
            {
                uiConsoleMessagesList.Enqueue($"{deviceName} with ID {deviceId} already found");
            }
        }
        else
        {
            uiConsoleMessagesList.Enqueue("Scanning is in progress");
        }
    }

    void ScanBleDevices()
    {
        scan = JoelBLE.ScanDevices();
        uiConsoleMessagesList.Enqueue("BLE.ScanDevices() started");

        scan.Found = (_discoveredDeviceId, _discoveredDeviceName) =>
        {
            //uiConsoleMessagesList.Enqueue($"Found device with name: {_discoveredDeviceName} and ID: {_discoveredDeviceId}");
            Debug.Log($"Found device with name: {_discoveredDeviceName} and ID: {_discoveredDeviceId}");

            if (!discoveredDevices.ContainsKey(_discoveredDeviceId))
            {
                discoveredDevices.TryAdd(_discoveredDeviceId, _discoveredDeviceName);
            }

            // finding the Xsens DOT
            if (deviceId == null && _discoveredDeviceName.Equals(deviceName))
            {
                deviceId = _discoveredDeviceId;
                deviceFound = true;      
                uiConsoleMessagesList.Enqueue($"{deviceName} found -> ID: {deviceId}");
            }
        };

        scan.Finished = () =>
        {
            isScanningForDevice = false;
            uiConsoleMessagesList.Enqueue("Scanning for device is now complete");

            if (deviceId == null)
            {
                deviceId = "-1";
            }
        };

        while (deviceId == null)
        {
            Thread.Sleep(1000);
        }

        scan.Cancel();
        deviceScanningThread = null;
        isScanningForDevice = false;

        if (deviceId.Equals("-1"))
        {
            uiConsoleMessagesList.Enqueue($"{deviceName} has not been found");
            deviceFound = false;
        }

    }



    public void KillProcessHandle()
    {
        uiConsoleMessagesList.Enqueue("Stopping Process");
        if (deviceScanningThread != null && deviceScanningThread.IsAlive)
        {
            deviceScanningThread.Abort();
            deviceScanningThread = null;
        }
        if (batteryThread != null && batteryThread.IsAlive)
        {
            batteryThread.Abort();
            batteryThread = null;
        }
    }

    public void ClearUiConsoleHandle()
    {
        uiConsoleMessages = "";
        UiConsoleText.text = uiConsoleMessages;
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
