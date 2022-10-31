using System; // For catching exceptions
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO.Ports; // For serial

public class SceneMgrScript:MonoBehaviour {
    [Header("Scene Objects")]
    public Camera mainCamera;
    public Light ceilingLamp;
    public Light ambientLightSource;
    public TextMeshProUGUI subtitles;

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
    public float skyAmbientStrength = 0.5f;
    [Range(0f, 1f)]
    public float lampAmbientStrength = 0.25f;

    [System.Serializable]
    public class AnimFrame
    {
        public GameObject model;
        public int duration = 1;
    }

    [System.Serializable]
    public class AnimList
    {
        public List<AnimFrame> work;
        public List<AnimFrame> switchLight;
        public int switchLightOnFrame;
        public int endSubtitleOnFrame;

        private int currentAnim = 0;
        private int currentFrame = 0;

        public int ChangeAnimation(int which)
        {
            if (which == 0 || which == 1)
            {
                currentAnim = which;
                currentFrame = -1;
                return NextFrame().frameObj.duration;
            }
            return -1;
        }
        
        public (AnimFrame frameObj, int number) NextFrame()
        {
            foreach (AnimFrame frame in work) frame.model.SetActive(false);
            foreach (AnimFrame frame in switchLight) frame.model.SetActive(false);
            currentFrame++;
            AnimFrame currentFrameObj = null;
            switch (currentAnim)
            {
                case 0:
                    if (currentFrame >= work.Count) currentFrame = 0;
                    currentFrameObj = work[currentFrame];
                    break;
                case 1:
                    if (currentFrame >= switchLight.Count) currentFrame = 0;
                    currentFrameObj = switchLight[currentFrame];
                    break;
            }
            currentFrameObj.model.SetActive(true);
            return (currentFrameObj, currentFrame);
        }
    }

    [Header("Animation")]
    public float framesPerSecond = 8f;
    public AnimList animations;

    private int readingCount = 0;
    private int[] readings;
    private int readIndex = 0;
    private int readingTotal = 0;
    private float readingAvg = 0f;

    private bool lightsOn = false;
    private bool lightsShouldBeOn = false;
    private bool guyWorking = true;
    private float sinceLastAnimFrame = 0f;
    private float currentFrameDuration = 1f;

    private Color skyColor = new();
    private Color ambientColor = new();

    void Start()
    {
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
        if (initialReading <= nightUnder) lightsOn = lightsShouldBeOn = true;
            
        ceilingLamp.gameObject.SetActive(lightsOn);
        animations.ChangeAnimation(0);
        subtitles.text = "";
    }

    void Update()
    {
        int newReading = int.Parse(stream.ReadLine());
        if (lightsOn) newReading -= ledBias;
        readingTotal -= readings[readIndex];
        readings[readIndex] = newReading;
        readingTotal += readings[readIndex];
        readIndex++;
        if (readIndex >= readingCount)
        {
            readIndex = 0;
        }
        readingAvg = readingTotal / readingCount;

        skyColor = Color.Lerp(nightSkyColor, daySkyColor, Mathf.InverseLerp(minLight, maxLight, readingAvg));
        mainCamera.backgroundColor = skyColor;

        ambientColor = Color.Lerp(Color.black, skyColor, skyAmbientStrength);
        if (lightsOn) ambientColor += Color.Lerp(Color.black, ceilingLamp.color, lampAmbientStrength);
        RenderSettings.ambientSkyColor = ambientColor;
        ambientLightSource.color = ambientColor;

        sinceLastAnimFrame += Time.deltaTime * framesPerSecond;
        if (guyWorking)
        {
            if (sinceLastAnimFrame >= currentFrameDuration)
            {
                currentFrameDuration = animations.NextFrame().frameObj.duration;
                sinceLastAnimFrame = 0;
            }

            if (readingAvg < nightUnder && !lightsOn)
            {
                lightsShouldBeOn = true;
                subtitles.text = "Night already?!";
                guyWorking = false;
                sinceLastAnimFrame = 0;
                currentFrameDuration = animations.ChangeAnimation(1);
            }
            if (readingAvg > dayOver && lightsOn)
            {
                lightsShouldBeOn = false;
                subtitles.text = "Morning already?!";
                guyWorking = false;
                sinceLastAnimFrame = 0;
                currentFrameDuration = animations.ChangeAnimation(1);
            }
        }
        else
        {
            if (sinceLastAnimFrame >= currentFrameDuration)
            {
                (AnimFrame currentFrameObj, int currentFrameNumber) = animations.NextFrame();
                currentFrameDuration = currentFrameObj.duration;
                sinceLastAnimFrame = 0;

                if (currentFrameNumber == 0)
                {
                    guyWorking = true;
                    sinceLastAnimFrame = 0;
                    currentFrameDuration = animations.ChangeAnimation(0);
                }
                if (currentFrameNumber == animations.switchLightOnFrame)
                {
                    lightsOn = lightsShouldBeOn;
                    ceilingLamp.gameObject.SetActive(lightsOn);
                }
                if (currentFrameNumber == animations.endSubtitleOnFrame)
                {
                    subtitles.text = "";
                }
            }
        }

        stream.Write(lightsOn ? "1" : "0");
        stream.ReadExisting(); // Dispose extra data
    }
}
