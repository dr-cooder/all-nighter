using System; // For catching exceptions
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports; // For serial

public class SceneMgrScript:MonoBehaviour {
    [Header("Scene Objects")]
    public Camera mainCamera;
    public Light ceilingLamp;

    [Header("Arduino I/O")]
    public string sPort = "COM4";
    SerialPort stream; // From the Nano
    public int baud = 9600;
    public int readingsToSmooth = 144;

    [Header("Change according to physical room's light range")]
    public int minLight = 10;
    public int nightUnder = 15;
    public int dayOver = 30;
    public int maxLight = 50;
    public int ledBias = 10;

    [Header("Sky and ambient lighting")]
    public Color daySkyColor;
    public Color nightSkyColor;
    [Range(0f, 1f)]
    public float ambientStrength = 0.5f;

    private int readingCount = 0;
    private int[] readings;
    private int readIndex = 0;
    private int readingTotal = 0;
    private float readingAvg = 0;

    private bool lightsOn = false;
    private bool lightsShouldBeOn = false;
    private bool guyWorking = true;
    private float sinceLastAnimFrame = 0;

    private Color skyColor = new Color();

    void Start()
    {
        ceilingLamp.gameObject.SetActive(lightsOn);
        stream = new SerialPort(sPort, baud);
        stream.Open(); // Open serial stream
        int initialReading = int.Parse(stream.ReadLine());
        readingCount = readingsToSmooth;
        readings = new int[readingCount];
        for (int i = 0; i < readingCount; i++)
        {
            readings[i] = initialReading;
        }
        readingTotal = initialReading * readingCount;
        readingAvg = initialReading;
    }

    void Update()
    {
        readingTotal -= readings[readIndex];
        int newReading = int.Parse(stream.ReadLine());
        if (lightsOn) newReading -= ledBias;
        readings[readIndex] = newReading;
        readingTotal += readings[readIndex];
        readIndex++;
        if (readIndex >= readingCount)
        {
            readIndex = 0;
        }
        readingAvg = readingTotal / readingCount;

        skyColor = Color.Lerp(nightSkyColor, daySkyColor, Mathf.InverseLerp(minLight, maxLight, readingAvg));
        RenderSettings.ambientSkyColor = Color.Lerp(Color.black, skyColor, ambientStrength);
        mainCamera.backgroundColor = skyColor;

        if (guyWorking)
        {
            if (readingAvg < nightUnder && !lightsOn)
            {
                lightsShouldBeOn = true;
                guyWorking = false;
                sinceLastAnimFrame = 0;
                // Play "turning lights on" animation
                Debug.Log("Night already?!");
            }
            if (readingAvg > dayOver && lightsOn)
            {
                lightsShouldBeOn = false;
                guyWorking = false;
                sinceLastAnimFrame = 0;
                // Play "turning lights off" animation
                Debug.Log("Morning already?!");
            }
        }
        else
        {
            // Manage turning on/off light animation
            sinceLastAnimFrame += Time.deltaTime;
            if (sinceLastAnimFrame > 1)
            {
                lightsOn = lightsShouldBeOn;
                ceilingLamp.gameObject.SetActive(lightsOn);
                Debug.Log($"*Click!* Lights {(lightsOn ? "on" : "off")}!");
                guyWorking = true;
                sinceLastAnimFrame = 0;
            }
        }

        stream.Write(lightsOn ? "1" : "0");
        stream.ReadExisting(); // Dispose extra data
    }
}