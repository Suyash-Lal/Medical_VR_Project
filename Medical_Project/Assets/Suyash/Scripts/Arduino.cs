using System.IO.Ports; // Required for serial communication
using UnityEngine;

public class ArduinoController : MonoBehaviour
{
    public string portName = "COM3"; // Replace with your Arduino's COM port
    public int baudRate = 9600;     // Must match the baud rate in the Arduino code

    private SerialPort arduinoPort;

    void Start()
    {
        arduinoPort = new SerialPort(portName, baudRate);
        try
        {
            arduinoPort.Open(); // Open the serial port
            Debug.Log("Serial port opened successfully.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to open serial port: {e.Message}");
        }
    }

    void Update()
    {
        if (arduinoPort.IsOpen)
        {
            // Example condition: Pressing "1" key sends '1' to turn on the motor, "0" key sends '0' to turn off
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                arduinoPort.Write("1");
                Debug.Log("Sent: 1");
            }
            else if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                arduinoPort.Write("0");
                Debug.Log("Sent: 0");
            }
        }
    }

    void OnApplicationQuit()
    {
        if (arduinoPort != null && arduinoPort.IsOpen)
        {
            arduinoPort.Close(); // Close the serial port when exiting the application
            Debug.Log("Serial port closed.");
        }
    }
}