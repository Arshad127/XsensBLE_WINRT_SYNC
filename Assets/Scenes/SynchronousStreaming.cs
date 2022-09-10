using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ThreadPriority = UnityEngine.ThreadPriority;

public class SynchronousStreaming : MonoBehaviour
{
    // Class Variables and Constants
    private readonly string deviceName = "Xsens DOT";
    private string deviceId;
    private readonly string batteryServiceUuid = "{15173000-4947-11e9-8646-d663bd873d93}"; // xsens dot battery service
    private readonly string batteryCharacteristicsUuid = "15173001-4947-11e9-8646-d663bd873d93";
    private Dictionary<string, Dictionary<string, string>> devices = new Dictionary<string, Dictionary<string, string>>();
    private Dictionary<string, string> characteristicNames = new Dictionary<string, string>();

    private string uiConsoleMessages = "";
    private string lastError;
    private bool isScanningDevices = false;
    private bool isTargetDeviceFound = false;
    private bool isScanningServices = false;
    private bool isScanningCharacteristics = false;
    private bool isSubscribed = false;
    private bool hasFoundService = false;
    private bool hasFoundCharacteristic = false;
    private Thread testingThread;
    private ConcurrentQueue<string> uiConsoleMessagesList = new ConcurrentQueue<string>();


    // GUI Elements
    public Button startScanButton, killProcessesButton, clearUiButton;
    public TextMeshProUGUI UiConsoleText, ScanStatusTextBox;


    // Start is called before the first frame update
    void Start()
    {
        //StartScanButton.enabled = true;
        //StopScanButton.enabled = false;
        BleApi.Quit();
        ScanStatusTextBox.text = "Ready";
        lastError = "Ok";
        //bool success = Caching.ClearCache();
    }

    // Update is called once per frame
    void Update()
    {
        /*
        BleApi.ScanStatus status;

        if (isScanningCharacteristics)
        {
            BleApi.Characteristic res = new BleApi.Characteristic();
            while (BleApi.PollCharacteristic(out res, true) != BleApi.ScanStatus.FINISHED)
            {
                PrintToUiConsole($"Characteristic found -> {res.uuid}, {res.userDescription}");
            }

            if (BleApi.PollCharacteristic(out res, true) == BleApi.ScanStatus.FINISHED)
            {
                isScanningCharacteristics = false;
                PrintToUiConsole("Characteristics Scan Completed");
            }
        }
        */

        
        // log potential errors
        BleApi.ErrorMessage res = new BleApi.ErrorMessage();
        BleApi.GetError(out res);
        if (lastError != res.msg)
        {
            uiConsoleMessagesList.Enqueue(res.msg);
            Debug.LogError(res.msg);
            lastError = res.msg;
        }

        // Update the UI from concurrent queue
        PrintToConsoleBurst();

    }

    public void QuitApplicationHandler()
    {
        uiConsoleMessagesList.Enqueue("Quitting the application");
        startScanButton.enabled = false;
        killProcessesButton.enabled = false;
        clearUiButton.enabled = false;
        BleApi.Quit();
        Application.Quit();
    }

    public void StartScanHandler()
    {
        /*
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
        */
        //StartServiceScanHandler();
        //StartCharacteristicsScanHandler();
        testingThread = new Thread(ServiceScanning);
        testingThread.Start();
        uiConsoleMessagesList.Enqueue("Starting the testing thread");
    }

    public void StopScanHandler()
    {
        /*
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
        */
        if (testingThread != null && testingThread.IsAlive)
        {
            testingThread.Abort();
            uiConsoleMessagesList.Enqueue("Killed the testing thread");
        }
    }

    public void StartServiceScanHandler()
    {
        /*
        if (!isScanningServices)
        {
            PrintToUiConsole("Now Scanning for Services");
            PrintToUiConsole("Waiting before scanning services");
            Thread.Sleep(1000);
            //deviceId = "BluetoothLE#BluetoothLE98:43:fa:23:ef:41-d4:ca:6e:f1:82:7f";
            BleApi.ScanServices(deviceId);
            isScanningServices = true;
        }
        */
    }

    public void StartCharacteristicsScanHandler()
    {
        /*
        if (!isScanningCharacteristics)
        {
            deviceId = "BluetoothLE#BluetoothLE98:43:fa:23:ef:41-d4:ca:6e:f1:82:77";
            //deviceId = "BluetoothLE#BluetoothLE98:43:fa:23:ef:41-d4:ca:6e:f1:82:7f"; // override

            // start new scan
            PrintToUiConsole("Now Scanning for Characteristics");
            PrintToUiConsole("Waiting before scanning for characteristics");
            PrintToUiConsole($"Device ID  -> {deviceId}");
            PrintToUiConsole($"Service ID -> {batteryServiceUuid}");
            Thread.Sleep(1000);
            PrintToUiConsole("Starting Scan");
            BleApi.ScanCharacteristics(deviceId, batteryServiceUuid);
            PrintToUiConsole("Past Scan Command");
            //isScanningCharacteristics = true;

        }
        */
    }

    private void ServiceScanning()
    {
        deviceId = "BluetoothLE#BluetoothLE98:43:fa:23:ef:41-d4:ca:6e:f1:82:77";
        //deviceId = "BluetoothLE#BluetoothLE98:43:fa:23:ef:41-d4:ca:6e:f1:82:7f"; // override

        if (true)
        {
            uiConsoleMessagesList.Enqueue("Now Scanning for Services");
            uiConsoleMessagesList.Enqueue("Waiting before scanning services");
            Thread.Sleep(1000);
            BleApi.ScanServices(deviceId);
            isScanningServices = true;
        }

        BleApi.ScanStatus status;

        BleApi.Service res = new BleApi.Service();
        do
        {
            status = BleApi.PollService(out res, false);
            if (status == BleApi.ScanStatus.AVAILABLE)
            {
                uiConsoleMessagesList.Enqueue("Poll Service = AVAILABLE");

                uiConsoleMessagesList.Enqueue($"Service found -> {res.uuid}");
                if (res.uuid.Equals(batteryServiceUuid))
                {
                    uiConsoleMessagesList.Enqueue($"Selected Service - Battery -> {batteryServiceUuid}");
                    hasFoundService = true;
                }
            }
            else if (status == BleApi.ScanStatus.FINISHED)
            {
                uiConsoleMessagesList.Enqueue("Poll Service = FINISHED");

                isScanningServices = false;
                uiConsoleMessagesList.Enqueue("Service Scan Completed");
            }
            else
            {
                uiConsoleMessagesList.Enqueue("Poll Service = PROCESSING");
            }
        } while (status == BleApi.ScanStatus.AVAILABLE);
        uiConsoleMessagesList.Enqueue("Out of the  service poll loop");
    }

    private void CharacteristicsScanning()
    {
        deviceId = "BluetoothLE#BluetoothLE98:43:fa:23:ef:41-d4:ca:6e:f1:82:77";
        //deviceId = "BluetoothLE#BluetoothLE98:43:fa:23:ef:41-d4:ca:6e:f1:82:7f"; // override

        uiConsoleMessagesList.Enqueue("Now Scanning for Characteristics");
        uiConsoleMessagesList.Enqueue($"Device ID  -> {deviceId}");
        uiConsoleMessagesList.Enqueue($"Service ID -> {batteryServiceUuid}");
        Thread.Sleep(1000);
        BleApi.ScanCharacteristics(deviceId, batteryServiceUuid);

        BleApi.ScanStatus status;
        /*
        if (isScanningCharacteristics)
        {
            BleApi.Characteristic res = new BleApi.Characteristic();
            while (BleApi.PollCharacteristic(out res, true) != BleApi.ScanStatus.FINISHED)
            {
                Debug.Log($"Characteristic found -> {res.uuid}, {res.userDescription}");
            }

            if (BleApi.PollCharacteristic(out res, true) == BleApi.ScanStatus.FINISHED)
            {
                isScanningCharacteristics = false;
                Debug.Log("Characteristics Scan Completed");
            }
        }
        */
        BleApi.Characteristic res = new BleApi.Characteristic();
        while (BleApi.PollCharacteristic(out res, true) != BleApi.ScanStatus.FINISHED)
        {
            uiConsoleMessagesList.Enqueue($"Characteristic found -> {res.uuid}, {res.userDescription}");
        }
    }


    public void ClearUiConsoleHandler()
    {
        uiConsoleMessages = "";
        uiConsoleMessagesList.Enqueue("UI console cleared");
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
