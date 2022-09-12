using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
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
    private ConcurrentDictionary<string, string> discoveredDevices = new ConcurrentDictionary<string, string>();

    private Dictionary<string, string> characteristicNames = new Dictionary<string, string>();
    private Thread readingThread, deviceScanningThread, batteryThread, connectionThread;
    private JoelBLE ble;
    private JoelBLE.BLEScan scan;

    private bool deviceFound = false;
    private bool isScanningForDevice = false;
    private bool isConnectedToDevice = false;
    private bool isSubscribed = false;


    // GUI Elements
    public Button discoverButton, batteryStatusButton, clearUiButton, connectButton;
    public TextMeshProUGUI UiConsoleText, ScanStatusTextBox;


    // GUI Related
    private string uiConsoleMessages;
    private ConcurrentQueue<string> uiConsoleMessagesList = new ConcurrentQueue<string>();
    private byte batteryLevel;
    private byte batteryStatus;



    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    // [START-UP]
    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    void Start()
    {
        // Create new BLE Implementation obj :
        ble = new JoelBLE();


        ScanStatusTextBox.text = "";
        UiConsoleText.text = "";
        batteryStatusButton.enabled = true;
        discoverButton.enabled = true;
        clearUiButton.enabled = true;
        connectButton.enabled = true;
        //readingThread = new Thread(ReadBleData);
    }


    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    // [UPDATE FRAMES]
    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    void Update()
    {
        // GUI updates
        PrintToConsoleBurst();

        /*
        if (isSubscribed)
        {
            if (!readingThread.IsAlive)
            {
                readingThread = new Thread(ReadBleData);
                readingThread.Start();
            }

            //uiConsoleMessagesList.Enqueue(ReadBatteryDetails(batteryLevel, batteryStatus));
            ScanStatusTextBox.text = ReadBatteryDetails(batteryLevel, batteryStatus);
        }
        */


    }


    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    // BUTTON-CLICK EVENT - Discover
    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    public void ScanForDeviceHandler()
    {
        Log("[INFO] - Discover Button Pressed");

        if (!isScanningForDevice)
        {

            if (!deviceFound)
            {
                Log($"Scanning for {deviceName}");

                discoveredDevices.Clear();
                isScanningForDevice = true;

                deviceScanningThread = new Thread(ScanBleDevices);
                deviceScanningThread.Start();
            }
            else
            {
                Log($"{deviceName} with ID {deviceId} already found");
            }
        }
        else
        {
            Log("Scanning is in progress");
        }
    }



    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    // [FUNCTION] - Find all BLE devices
    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    void ScanBleDevices()
    {
        // [BLE LIBRARY] - Scan Devices, return a "BLEScan" Object :
        scan = JoelBLE.ScanDevices();

        // Print to console :
        Log("BLE.ScanDevices() started");




        // [DELEGATE RETURN CALL] - "BLEScan" Object
        scan.Found = (_discoveredDeviceId, _discoveredDeviceName) =>
        {
            //uiConsoleMessagesList.Enqueue($"Found device with name: {_discoveredDeviceName} and ID: {_discoveredDeviceId}");
            Debug.Log($"Found device with name: {_discoveredDeviceName} and ID: {_discoveredDeviceId}");


            if (!discoveredDevices.ContainsKey(_discoveredDeviceId))
            {
                discoveredDevices.TryAdd(_discoveredDeviceId, _discoveredDeviceName);
            }


            // Find a device with the "Xsens DOT" name
            if (deviceId == null && _discoveredDeviceName.Equals(deviceName))
            {
                deviceId = _discoveredDeviceId;
                deviceFound = true;

                Log($"{deviceName} found -> ID: {deviceId}");
            }

        };




        // [DELEGATE RETURN CALL] - "BLEScan" Object
        scan.Finished = () =>
        {
            isScanningForDevice = false;
            
            Log("Scanning for device is now complete");

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
            // Print to console :
            Log($"{deviceName} has not been found");
            deviceFound = false;
        }

    }





    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    // BUTTON-CLICK EVENT - Battery
    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    public void SubscribeToBatteryHandler()
    {
        string deviceId = "BluetoothLE#BluetoothLE98:43:fa:23:ef:41-d4:ca:6e:f1:82:7f";
        string serviceId;
        string characteristicId;




        // READ TEST :
        new Thread(() =>
        {
            byte[] dataReceived = ble.ReadDataTest();

            batteryLevel  = dataReceived[0];
            batteryStatus = dataReceived[1];

            // Extract fields :
            Log($"\t Battery Lv       : {batteryLevel}%");
            Log($"\t ChargingStatus   : {batteryStatus}");

        }).Start();





        /*
        // TEST - Get Battery Data :
        serviceId        = "15173000-4947-11e9-8646-d663bd873d93";
        characteristicId = "15173001-4947-11e9-8646-d663bd873d93";

        new Thread(() =>
        {
            byte[] dataReceived = GetData(deviceId, serviceId, characteristicId);

            batteryLevel  = dataReceived[0];
            batteryStatus = dataReceived[1];

            // Extract fields :
            Log($"\t Battery Lv       : {batteryLevel}%");
            Log($"\t ChargingStatus   : {batteryStatus}");

        }).Start();
        */



        /*
        // TEST - Get ??? Data :
        serviceId        = "15173000-4947-11e9-8646-d663bd873d93";
        characteristicId = "15173001-4947-11e9-8646-d663bd873d93";


        new Thread(() =>
        {
            byte[] dataReceived = GetData(deviceId, serviceId, characteristicId);

            Log("SUCCESS!!");
            
            
            // Get sub array :
            byte[] yearSubArr             = getSubArray(dataReceived, 9, 2);
            byte[] SoftDeviceSubArr       = getSubArray(dataReceived, 16, 4);
            byte[] SerialNumber           = getSubArray(dataReceived, 20, 8);
            byte[] ShortProductCodeSubArr = getSubArray(dataReceived, 28, 6);

            // Extract fields :
            Log($"\t Built Year          : {BitConverter.ToUInt16(yearSubArr, 0)}");
            Log($"\t Build Month         : {dataReceived[11]}");
            Log($"\t Build Date          : {dataReceived[12]}");
            Log($"\t Build Hour          : {dataReceived[13]}");
            Log($"\t Build Minute        : {dataReceived[14]}");
            Log($"\t Build second        : {dataReceived[15]}");
            Log($"\t SoftDevice version  : {BitConverter.ToUInt32(SoftDeviceSubArr, 0)}");
            Log($"\t Serial Number       : {BitConverter.ToUInt64(SerialNumber, 0)}");
            Log($"\t Short Product Code  : {Encoding.Default.GetString(ShortProductCodeSubArr)}");
            

        }).Start();
        */


        /*
        if (deviceId != null && !deviceId.Equals("-1"))
        {
            Log("Connection Attempt");
            batteryThread = new Thread(GetBatteryStatus);
            batteryThread.Start();
        }
        else
        {
           Log($"Cannot subscribe since {deviceName} has not been found");
        }*/
    }





    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    // [FUNCTION] - Connect and Read Data
    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    public byte[] GetData(string deviceId, string serviceId, string characteristicId)
    {
        Log("[INFO] - Battery Button Pressed");

        bool isSuccessful = ble.SubscribeToCharacteristic(deviceId, serviceId, characteristicId);

        if (isSuccessful)
        {
            Log("Battery Subscription successful");
            isSubscribed = true;
            

            // Try read Data :
            Log("Reading data...");
            byte[] bleDataReceived = ble.ReadBytes();
            return bleDataReceived;


        }
        else
        {
            Log("Battery Subscription Failed");
            isSubscribed = false;

            // Empty byte array :
            return new byte[]{};


        }
    }


    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    // DESCRIPTION : Make subarray of array
    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    public static T[] getSubArray<T>(T[] array, int startIdx, int length)
    {
        T[] subArray = new T[length];

        Array.Copy(array, startIdx, subArray, 0, length);

        return subArray;
    }

























    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    // [FUNCTION] - Read Battery info
    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    void GetBatteryStatus()
    {
        string deviceId = "BluetoothLE#BluetoothLE98:43:fa:23:ef:41-d4:ca:6e:f1:82:7f";
        
        string batteryServiceUuid         = "{15173000-4947-11e9-8646-d663bd873d93}";
        string batteryCharacteristicsUuid = "15173001-4947-11e9-8646-d663bd873d93";

        Log("[INFO] - Battery Button Pressed");

        bool isSuccessful = ble.SubscribeToCharacteristic(deviceId, batteryServiceUuid, batteryCharacteristicsUuid);

        if (isSuccessful)
        {
            Log("Battery Subscription successful");
            isSubscribed = true;
            

            Thread.Sleep(1000);


            // Try read Data :
            Log("Reading data...");
            ReadBleData();

        }
        else
        {
            Log("Battery Subscription Failed");
            isSubscribed = false;
        }
    }


    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    // [FUNCTION] - Read Data
    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    private void ReadBleData()
    {
        byte[] bleDataReceived = ble.ReadBytes();

        
        Log($"[INFO - BATTERY] - ReadBleData() {bleDataReceived.Length}");


        batteryLevel = bleDataReceived[0];
        batteryStatus = bleDataReceived[1];


        string[] batteryStatusVerbose = { "[NOT CHARGING]", "[CHARGING]" };


        if (batteryStatus is 0 or 1)
        {
            Log($"Battery Level: {batteryLevel}% {batteryStatusVerbose[batteryStatus]}");
        }
        else
        {
            Log($"Battery Level: {batteryLevel}% and device charge status cannot be read.");
        }
        
    }














    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    // [FUNCTION] - Write to Unity Console UI
    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    public void Log(string logMessage)
    {
        uiConsoleMessagesList.Enqueue(logMessage);
    }






    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    // [UNITY] - Specific Methods
    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
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
        CleanUp();
        Application.Quit();
    }
   
    private void OnDestroy()
    {
        CleanUp();
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

    private void CleanUp()
    {
        try
        {
            scan.Cancel();
            ble.Close();
            deviceScanningThread.Abort();
        }
        catch (NullReferenceException e)
        {
            Debug.Log("Thread or object never initialized.\n" + e);
        }
        try
        {
            connectionThread.Abort();
        }
        catch (NullReferenceException e)
        {
            Debug.Log("Thread or object never initialized.\n" + e);
        }
        try
        {
            readingThread.Abort();
        }
        catch (NullReferenceException e)
        {
            Debug.Log("Thread or object never initialized.\n" + e);
        }
        try
        {
            batteryThread.Abort();
        }
        catch (NullReferenceException e)
        {
            Debug.Log("Thread or object never initialized.\n" + e);
        }
    }

    
   










    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    // PENDING
    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    public void ConnectHandler()
    {
        Log("Not implemented");
    }

    void ConnectBleDevice()
    {
        Log("Not implemented");
    }



    /*
    private void ReadBleData(object obj)
    {
        byte[] packageReceived = ble.ReadBytes();
        
        batteryLevel = packageReceived[0];
        batteryStatus = packageReceived[1];

        Debug.Log(ReadBatteryDetails(batteryLevel, batteryStatus));
    }

    
    private string ReadBatteryDetails(int inBatteryLevel, int inBatteryStatus)
    {
        string[] batteryStatusVerbose = { "[NOT CHARGING]", "[CHARGING]" };
        string outString = "";

        if (inBatteryStatus is 0 or 1)
        {
            outString = $"Battery Level: {inBatteryLevel}% {batteryStatusVerbose[inBatteryStatus]}";
        }
        else
        {
            outString = $"Battery Level: {inBatteryLevel}% and device charge status cannot be read.";
        }
        //PrintToUiConsole(outString);
        Debug.Log(outString);
        return outString;
    }
    */

}
