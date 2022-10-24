using System; // For catching exceptions
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports; // For serial

public class SceneMgrScript:MonoBehaviour {
    public string sPort = "COM4";
    SerialPort stream; // From the Nano
    public int baud = 9600;
    public int nightThreshold = 15; // CHANGE THIS ACCORDING TO THE ROOM'S LIGHT LEVELS
    public int dayThreshold = 30; // THIS TOO

    private bool lightsOn = false;
    private bool lightsShouldBeOn = false;
    private bool guyWorking = true;
    private float guyAnimTime = 0;

    void Start()
    {
        stream = new SerialPort(sPort, baud);
        stream.Open(); // Open serial stream
    }

    void Update()
    {
        int sunLevel = int.Parse(stream.ReadLine());
        if (guyWorking)
        {
            if (sunLevel < nightThreshold && !lightsOn)
            {
                lightsShouldBeOn = true;
                guyWorking = false;
                guyAnimTime = 0;
                // Play "turning lights on" animation
                Debug.Log("Night already?!");
            }
            if (sunLevel > dayThreshold && lightsOn)
            {
                lightsShouldBeOn = false;
                guyWorking = false;
                guyAnimTime = 0;
                // Play "turning lights off" animation
                Debug.Log("Morning already?!");
            }
        }
        else
        {
            // Manage turning on light animation
            guyAnimTime += Time.deltaTime;
            if (guyAnimTime > 1)
            {
                lightsOn = lightsShouldBeOn;
                Debug.Log($"*Click!* Lights {(lightsOn ? "on" : "off")}!");
                guyWorking = true;
                guyAnimTime = 0;
            }
        }
        stream.Write(lightsOn ? "1" : "0");
        stream.ReadExisting(); // Dispose extra data
    }
}