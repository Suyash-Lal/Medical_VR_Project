using System.IO.Ports; // Required for serial communication
using UnityEngine;
using UnityEngine.InputSystem; // Required for the new Input System

public class ArduinoController : MonoBehaviour
{
    public string portName = "COM5"; // Replace with your Arduino's COM port
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
            // Using new Input System for keypress detection
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                arduinoPort.Write("1");
                Debug.Log("Sent: 1");
            }
            else if (Keyboard.current.digit0Key.wasPressedThisFrame)
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
