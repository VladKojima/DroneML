using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System;

public class UdpStreamer : MonoBehaviour
{
    public Camera cam;
    public DroneSensors droneSensors;
    public int width = 320;
    public int height = 240;
    public int quality = 50;
    public string videoIp = "192.168.0.101";
    public int videoPort = 5006;
    public int sensorsPort = 5007;
    public float sensorsUpdateRate = 30f; // Hz

    private UdpClient videoClient;
    private UdpClient sensorsClient;
    private IPEndPoint videoEndPoint;
    private IPEndPoint sensorsEndPoint;
    private Texture2D tex;
    private RenderTexture rt;
    private float sensorsUpdateInterval;
    private float timeSinceLastSensorUpdate;
    // Ключи для сохранения
    private const string WIDTH_KEY = "UdpStreamer_Width";
    private const string HEIGHT_KEY = "UdpStreamer_Height";
    private const string QUALITY_KEY = "UdpStreamer_Quality";
    private const string VIDEO_IP_KEY = "UdpStreamer_VideoIp";
    private const string VIDEO_PORT_KEY = "UdpStreamer_VideoPort";
    private const string SENSORS_PORT_KEY = "UdpStreamer_SensorsPort";
    private const string UPDATE_RATE_KEY = "UdpStreamer_UpdateRate";

    void Awake()
    {
        LoadSettings();
    }

    public void Start()
    {
        InitializeStreamer();
    }

    void OnDestroy()
    {
        SaveSettings();
        Cleanup();
    }

    void OnApplicationQuit()
    {
        SaveSettings();
        Cleanup();
    }

    private void LoadSettings()
    {
        if (PlayerPrefs.HasKey(WIDTH_KEY)) width = PlayerPrefs.GetInt(WIDTH_KEY);
        if (PlayerPrefs.HasKey(HEIGHT_KEY)) height = PlayerPrefs.GetInt(HEIGHT_KEY);
        if (PlayerPrefs.HasKey(QUALITY_KEY)) quality = PlayerPrefs.GetInt(QUALITY_KEY);
        if (PlayerPrefs.HasKey(VIDEO_IP_KEY)) videoIp = PlayerPrefs.GetString(VIDEO_IP_KEY);
        if (PlayerPrefs.HasKey(VIDEO_PORT_KEY)) videoPort = PlayerPrefs.GetInt(VIDEO_PORT_KEY);
        if (PlayerPrefs.HasKey(SENSORS_PORT_KEY)) sensorsPort = PlayerPrefs.GetInt(SENSORS_PORT_KEY);
        if (PlayerPrefs.HasKey(UPDATE_RATE_KEY)) sensorsUpdateRate = PlayerPrefs.GetFloat(UPDATE_RATE_KEY);
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetInt(WIDTH_KEY, width);
        PlayerPrefs.SetInt(HEIGHT_KEY, height);
        PlayerPrefs.SetInt(QUALITY_KEY, quality);
        PlayerPrefs.SetString(VIDEO_IP_KEY, videoIp);
        PlayerPrefs.SetInt(VIDEO_PORT_KEY, videoPort);
        PlayerPrefs.SetInt(SENSORS_PORT_KEY, sensorsPort);
        PlayerPrefs.SetFloat(UPDATE_RATE_KEY, sensorsUpdateRate);
        PlayerPrefs.Save();
    }

    public void InitializeStreamer()
    {
        Cleanup();

        try
        {
            videoClient = new UdpClient();
            sensorsClient = new UdpClient();

            videoEndPoint = new IPEndPoint(IPAddress.Parse(videoIp), videoPort);
            sensorsEndPoint = new IPEndPoint(IPAddress.Parse(videoIp), sensorsPort);

            rt = new RenderTexture(width, height, 24);
            tex = new Texture2D(width, height, TextureFormat.RGB24, false);

            if (droneSensors == null)
                droneSensors = GetComponent<DroneSensors>();

            sensorsUpdateInterval = 1f / sensorsUpdateRate;
            timeSinceLastSensorUpdate = 0f;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize UDP Streamer: {e.Message}");
        }
    }

    private void Cleanup()
    {
        if (videoClient != null)
        {
            videoClient.Close();
            videoClient = null;
        }
        if (sensorsClient != null)
        {
            sensorsClient.Close();
            sensorsClient = null;
        }
        if (rt != null)
        {
            Destroy(rt);
            rt = null;
        }
        if (tex != null)
        {
            Destroy(tex);
            tex = null;
        }
    }

    public void UpdateSettings(int newWidth, int newHeight, int newQuality, string newVideoIp,
                              int newVideoPort, int newSensorsPort, float newUpdateRate)
    {
        bool needsRestart = (newWidth != width || newHeight != height || newQuality != quality ||
                            newVideoIp != videoIp || newVideoPort != videoPort ||
                            newSensorsPort != sensorsPort || Mathf.Abs(newUpdateRate - sensorsUpdateRate) > 0.1f);

        width = newWidth;
        height = newHeight;
        quality = newQuality;
        videoIp = newVideoIp;
        videoPort = newVideoPort;
        sensorsPort = newSensorsPort;
        sensorsUpdateRate = newUpdateRate;

        if (needsRestart)
        {
            InitializeStreamer();
            SaveSettings();
        }
    }

    // Метод для вызова из Debug Monitor
    public void ApplySettingsFromGUI()
    {
        SaveSettings();
        InitializeStreamer();
    }

    void LateUpdate()
    {
        //try
        //{
            // Видео поток
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            byte[] imageData = tex.EncodeToJPG(quality);
            videoClient.Send(imageData, imageData.Length, videoEndPoint);

            cam.targetTexture = null;
            RenderTexture.active = null;
        //}
        //finally { Debug.LogWarning("Что то в стримере..."); } // похуй, лень проверять ошибку

        // Данные датчиков (отправляем с фиксированной частотой)
        timeSinceLastSensorUpdate += Time.deltaTime;
        if (timeSinceLastSensorUpdate >= sensorsUpdateInterval)
        {
            SendSensorData();
            timeSinceLastSensorUpdate = 0f;
        }
    }

    private void SendSensorData()
    {
        SensorData data = new SensorData
        {
            altitude = droneSensors.AbsoluteAltitude,
            groundDistance = droneSensors.GroundDistance,
            pitch = droneSensors.Pitch,
            roll = droneSensors.Roll,
            yaw = droneSensors.Yaw,
            speed = droneSensors.TotalSpeed,
            velocityX = droneSensors.ForwardSpeed,
            velocityY = droneSensors.VerticalSpeed,
            velocityZ = droneSensors.RightSpeed
        };

        string json = JsonUtility.ToJson(data);
        byte[] sensorData = Encoding.UTF8.GetBytes(json);

        sensorsClient.Send(sensorData, sensorData.Length, sensorsEndPoint);
    }

    [System.Serializable]
    public struct SensorData
    {
        public float altitude;
        public float groundDistance;
        public float pitch;
        public float roll;
        public float yaw;
        public float speed;
        public float velocityX;
        public float velocityY;
        public float velocityZ;
    }
}