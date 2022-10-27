using System; // For catching exceptions
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports; // For serial

public class SceneMgrScript:MonoBehaviour {
    public Camera mainCamera;
    public string sPort = "COM4";
    SerialPort stream; // From the Nano
    public int baud = 9600;

    // CHANGE THESE ACCORDING TO THE ROOM'S LIGHT LEVELS
    public int minLight = 10;
    public int nightUnder = 15;
    public int dayOver = 30;
    public int maxLight = 50;
    public int ledBias = 10;

    public Color dayAmbient;
    public Color nightAmbient;

    private bool lightsOn = false;
    private bool lightsShouldBeOn = false;
    private bool guyWorking = true;
    private float guyAnimTime = 0;

    private Color skyColor = new Color();

    void Start()
    {
        stream = new SerialPort(sPort, baud);
        stream.Open(); // Open serial stream
    }

    void Update()
    {
        int sunLevel = int.Parse(stream.ReadLine());
        if (lightsOn) sunLevel -= ledBias;
        skyColor = Color.Lerp(nightAmbient, dayAmbient, Mathf.InverseLerp(minLight, maxLight, sunLevel));
        RenderSettings.ambientSkyColor = skyColor;
        mainCamera.backgroundColor = skyColor;
        if (guyWorking)
        {
            if (sunLevel < nightUnder && !lightsOn)
            {
                lightsShouldBeOn = true;
                guyWorking = false;
                guyAnimTime = 0;
                // Play "turning lights on" animation
                Debug.Log("Night already?!");
            }
            if (sunLevel > dayOver && lightsOn)
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
            // Manage turning on/off light animation
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