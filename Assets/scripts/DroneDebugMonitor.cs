using UnityEngine;

public class DroneDebugMonitor : MonoBehaviour
{
    [Header("References")]
    public FlightController flightController;
    public DroneInputManager inputManager;
    public DroneSensors droneSensors;
    public UdpStreamer udpStreamer;

    [Header("Debug Display")]
    public bool showDebugInfo = true;
    public bool showMotorForces = true;
    public bool logToConsole = false;
    public bool showAngles = true;
    public bool showSensors = false;
    public bool showUDPSettings = true;

    [Header("GUI Settings")]
    [Range(300f, 600f)]
    public float guiWidth = 350f;
    [Range(400f, 1200f)]
    public float guiHeight = 500f;

    private Rigidbody rb;
    private GUIStyle labelStyle;
    private GUIStyle textFieldStyle;
    private GUIStyle toggleStyle;
    private Vector2 scrollPosition = Vector2.zero;

    // Данные для мониторинга
    private float currentPitch, currentRoll, currentYaw;
    private Vector3 angularVelocity;
    private Vector3 velocity;
    private float[] motorThrusts = new float[4];

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        droneSensors = GetComponent<DroneSensors>();
        udpStreamer = GetComponent<UdpStreamer>();

        // Настройка стиля GUI
        labelStyle = new GUIStyle();
        labelStyle.fontSize = 22;
        labelStyle.normal.textColor = Color.white;
        labelStyle.wordWrap = true;
    }

    void Update()
    {
        if (rb == null) return;

        // Обновляем данные мониторинга
        UpdateMonitoringData();

        // Логирование в консоль
        if (logToConsole && Time.time % 1f < Time.deltaTime)
        {
            LogDebugInfo();
        }

        DebugLines();
    }

    void UpdateMonitoringData()
    {
        Vector3 eulerAngles = transform.eulerAngles;
        currentPitch = NormalizeAngle(eulerAngles.x);
        currentRoll = NormalizeAngle(eulerAngles.z);
        currentYaw = NormalizeAngle(eulerAngles.y);

        angularVelocity = rb.angularVelocity;
        velocity = rb.velocity;

        // Получаем силы моторов
        if (flightController != null)
        {
            if (flightController.frontLeftMotor) motorThrusts[0] = flightController.frontLeftMotor.GetCurrentThrust();
            if (flightController.frontRightMotor) motorThrusts[1] = flightController.frontRightMotor.GetCurrentThrust();
            if (flightController.rearLeftMotor) motorThrusts[2] = flightController.rearLeftMotor.GetCurrentThrust();
            if (flightController.rearRightMotor) motorThrusts[3] = flightController.rearRightMotor.GetCurrentThrust();
        }
    }

    void LogDebugInfo()
    {
        Debug.Log($"Drone Debug - Pitch: {currentPitch:F1}°, Roll: {currentRoll:F1}°, Yaw: {currentYaw:F1}°");
        Debug.Log($"Angular Velocity - X: {angularVelocity.x:F2}, Y: {angularVelocity.y:F2}, Z: {angularVelocity.z:F2}");
        Debug.Log($"Motor Thrusts - FL: {motorThrusts[0]:F1}, FR: {motorThrusts[1]:F1}, RL: {motorThrusts[2]:F1}, RR: {motorThrusts[3]:F1}");
    }

    void OnGUI()
    {
        if (!showDebugInfo) return;

        textFieldStyle = new GUIStyle(GUI.skin.textField);
        textFieldStyle.fontSize = 22;
        textFieldStyle.fixedHeight = 30; 
        
        toggleStyle = new GUIStyle(GUI.skin.toggle);
        toggleStyle.fontSize = 22;
        toggleStyle.fixedHeight = 30;


        // Основная область с фиксированным размером
        Rect mainRect = new Rect(Screen.width - guiWidth - 10, 10, guiWidth, guiHeight);

        GUILayout.BeginArea(mainRect);
        GUILayout.BeginVertical("box");

        // Заголовок (всегда виден)
        GUILayout.Label("=== DRONE DEBUG MONITOR ===", labelStyle);
        GUILayout.Space(5);

        // Область с прокруткой для основного контента
        scrollPosition = GUILayout.BeginScrollView(scrollPosition,
            GUILayout.Width(guiWidth - 30),
            GUILayout.Height(guiHeight - 250)); // Оставляем место для заголовка и отступов

        // Углы и скорости
        GUILayout.Label("ORIENTATION:", GetBoldStyle());
        GUILayout.Label($"Pitch: {currentPitch:F1}°", GetColoredStyle(Mathf.Abs(currentPitch) > 20f));
        GUILayout.Label($"Roll: {currentRoll:F1}°", GetColoredStyle(Mathf.Abs(currentRoll) > 20f));
        GUILayout.Label($"Yaw: {currentYaw:F1}°", labelStyle);
        GUILayout.Space(5);

        // Состояние системы
        if (flightController != null)
        {
            GUILayout.Label("FLIGHT CONTROLLER:", GetBoldStyle());
            GUILayout.Label($"Stabilization: {(flightController.IsStabilizationEnabled() ? "ON" : "OFF")}",
                GetColoredStyle(!flightController.IsStabilizationEnabled()));
            GUILayout.Space(5);
        }

        // Силы моторов
        if (showMotorForces)
        {
            GUILayout.Label("MOTOR FORCES:", GetBoldStyle());
            GUILayout.Label($"Front Left:  {motorThrusts[0]:F1}N", labelStyle);
            GUILayout.Label($"Front Right: {motorThrusts[1]:F1}N", labelStyle);
            GUILayout.Label($"Rear Left:   {motorThrusts[2]:F1}N", labelStyle);
            GUILayout.Label($"Rear Right:  {motorThrusts[3]:F1}N", labelStyle);

            // Показываем разность тяг (для диагностики калибровки)
            float avgThrust = (motorThrusts[0] + motorThrusts[1] + motorThrusts[2] + motorThrusts[3]) / 4f;
            GUILayout.Label($"Average: {avgThrust:F1}N", labelStyle);

            float maxDiff = 0f;
            for (int i = 0; i < 4; i++)
            {
                float diff = Mathf.Abs(motorThrusts[i] - avgThrust);
                if (diff > maxDiff) maxDiff = diff;
            }
            GUILayout.Label($"Max Diff: {maxDiff:F1}N", GetColoredStyle(maxDiff > 5f));

            // Показываем различия для каждого мотора
            for (int i = 0; i < 4; i++)
            {
                float diff = motorThrusts[i] - avgThrust;
                string motorName = i == 0 ? "FL" : i == 1 ? "FR" : i == 2 ? "RL" : "RR";
                GUILayout.Label($"{motorName} Diff: {diff:+F1;-F1}N", GetColoredStyle(Mathf.Abs(diff) > 3f));
            }
            GUILayout.Space(5);
        }

        // Входные данные
        if (inputManager != null)
        {
            GUILayout.Label("INPUT:", GetBoldStyle());
            Vector4 input = inputManager.GetCurrentInput();
            GUILayout.Label($"Thrust: {input.x:F2}", labelStyle);
            GUILayout.Label($"Pitch: {input.y:F2}", labelStyle);
            GUILayout.Label($"Roll: {input.z:F2}", labelStyle);
            GUILayout.Label($"Yaw: {input.w:F2}", labelStyle);
            GUILayout.Label($"Mode: {inputManager.currentInputMode}", labelStyle);
            GUILayout.Space(5);
        }

        // Дополнительная информация о физике
        GUILayout.Label("PHYSICS:", GetBoldStyle());
        GUILayout.Label($"Mass: {rb.mass:F1}kg", labelStyle);
        GUILayout.Label($"Drag: {rb.drag:F2}", labelStyle);
        GUILayout.Label($"Angular Drag: {rb.angularDrag:F2}", labelStyle);
        Vector3 com = rb.centerOfMass;
        GUILayout.Label($"Center of Mass: ({com.x:F2}, {com.y:F2}, {com.z:F2})", labelStyle);
        GUILayout.Space(5);

        // === ДАННЫЕ СЕНСОРОВ ===
        if (droneSensors != null && showSensors)
        {
            GUILayout.Label("SENSORS:", GetBoldStyle());
            GUILayout.Label($"Absolute Altitude: {droneSensors.AbsoluteAltitude:F2}m", labelStyle);
            GUILayout.Label($"Ground Distance: {droneSensors.GroundDistance:F2}m",
                GetColoredStyle(droneSensors.GroundDistance < 5f));
            GUILayout.Label($"Attitude: ({droneSensors.Attitude.x:F1}°, {droneSensors.Attitude.y:F1}°, {droneSensors.Attitude.z:F1}°)", labelStyle);
            GUILayout.Label($"Angular Velocity: ({droneSensors.AngularVelocity.x:F1}, {droneSensors.AngularVelocity.y:F1}, {droneSensors.AngularVelocity.z:F1}) rad/s", labelStyle);
            GUILayout.Label($"Forward Speed: {droneSensors.ForwardSpeed:F1} m/s", labelStyle);
            GUILayout.Label($"Right Speed: {droneSensors.RightSpeed:F1} m/s", labelStyle);
            GUILayout.Label($"Vertical Speed: {droneSensors.VerticalSpeed:F1} m/s",
                GetColoredStyle(Mathf.Abs(droneSensors.VerticalSpeed) > 2f));
            GUILayout.Label($"Total Speed: {droneSensors.TotalSpeed:F1} m/s", labelStyle);
            GUILayout.Space(5);
        }

        // === НАСТРОЙКИ UDP СТРИМЕРА ===
        if (showUDPSettings)
        {
            GUILayout.Label("UDP STREAM SETTINGS:", GetBoldStyle());

            // Разрешение
            GUILayout.Label("Resolution:", labelStyle);
            GUILayout.BeginHorizontal();
            int newWidth = (int)GUILayout.HorizontalSlider(udpStreamer.width, 64, 1920);
            GUILayout.Label($"{udpStreamer.width}x{udpStreamer.height}", labelStyle);
            GUILayout.EndHorizontal();

            // Качество
            GUILayout.Label("Quality:", labelStyle);
            int newQuality = (int)GUILayout.HorizontalSlider(udpStreamer.quality, 1, 100);

            // IP адрес
            GUILayout.Label("Video IP:", labelStyle);
            string newVideoIp = GUILayout.TextField(udpStreamer.videoIp, textFieldStyle);

            // Порт видео
            GUILayout.Label("Video Port:", labelStyle);
            int newVideoPort = int.Parse(GUILayout.TextField(udpStreamer.videoPort.ToString(), textFieldStyle));

            // Порт сенсоров
            GUILayout.Label("Sensors Port:", labelStyle);
            int newSensorsPort = int.Parse(GUILayout.TextField(udpStreamer.sensorsPort.ToString(), textFieldStyle));

            // Частота обновления сенсоров
            GUILayout.Label("Sensors Rate (Hz):", labelStyle);
            float newSensorsUpdateRate = GUILayout.HorizontalSlider(udpStreamer.sensorsUpdateRate, 1, 60);
            GUILayout.Label($"{udpStreamer.sensorsUpdateRate:F1} Hz", labelStyle);

            // сохранение настроек для стримера
            // (int)(newWidth * 0.75f); Сохраняем соотношение 4:3
            udpStreamer.UpdateSettings(newWidth, (int)(newWidth * 0.75f), newQuality, newVideoIp,
                                  newVideoPort, newSensorsPort, newSensorsUpdateRate);

            GUILayout.Space(10);
        }

        { // ==== НАСТРОЙКИ ГРАФИКИ ====
            GUILayout.Label("GRAPHICS PRESET:", GetBoldStyle());

            string[] presetNames = new string[] { "Very Low", "Low", "Medium", "High", "Very High", "Ultra" };
            int maxAvailable = QualitySettings.names.Length - 1;

            // Показываем только доступные пресеты
            string[] availablePresets = new string[maxAvailable + 1];
            for (int i = 0; i <= maxAvailable; i++)
            {
                availablePresets[i] = presetNames[Mathf.Min(i, presetNames.Length - 1)];
            }

            int currentLevel = QualitySettings.GetQualityLevel();
            int newLevel = GUILayout.SelectionGrid(currentLevel, availablePresets, 3);

            if (newLevel != currentLevel)
            {
                QualitySettings.SetQualityLevel(newLevel, true);
                PlayerPrefs.SetInt("GraphicsQuality", newLevel);
                PlayerPrefs.Save();
            }
        }

        GUILayout.EndScrollView();
        GUILayout.Space(10);

        // ====  КНОПКИ УПРАВЛЕНИЯ  ====
        GUILayout.Label("CONTROLS:", GetBoldStyle());

        if (GUILayout.Button("Toggle Stabilization", GUILayout.Height(25)) && flightController != null)
        {
            flightController.ToggleStabilization();
        }

        if (GUILayout.Toggle(showAngles, "Show Angles", toggleStyle))
        {
            showAngles = true;
        }
        else showAngles = false;

        // === ГАЛОЧКА ДЛЯ СЕНСОРОВ ===
        if (GUILayout.Toggle(showSensors, "Show Sensors", toggleStyle))
        {
            showSensors = true;
        }
        else showSensors = false; 

        // === ГАЛОЧКА ДЛЯ UDP ===
        if (GUILayout.Toggle(showUDPSettings, "Show UDP Settings", toggleStyle))
        {
            showUDPSettings = true;
        }
        else showUDPSettings = false;

        GUILayout.Space(10);

        GUILayout.EndVertical();
        GUILayout.EndArea();

        // Показываем подсказку о прокрутке в правом нижнем углу
        GUIStyle hintStyle = new GUIStyle(labelStyle);
        hintStyle.fontSize = 10;
        hintStyle.normal.textColor = Color.gray;
        GUI.Label(new Rect(Screen.width - 200, Screen.height - 30, 190, 20),
                  "Scroll: Mouse Wheel in Debug Panel", hintStyle);
    }

    GUIStyle GetColoredStyle(bool warning)
    {
        GUIStyle style = new GUIStyle(labelStyle);
        style.normal.textColor = warning ? Color.red : Color.white;
        return style;
    }

    GUIStyle GetBoldStyle()
    {
        GUIStyle style = new GUIStyle(labelStyle);
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.yellow;
        return style;
    }

    float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }

    void OnDrawGizmos()
    {// всегда выключен
        if (true) return;

        // Отображаем ориентацию дрона
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * 2f); // Направление "вперед"

        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.up * 2f); // Направление "вверх"

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.right * 2f); // Направление "вправо"

        // Показываем целевое направление (горизонталь)
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, Vector3.up * 3f);

        // Показываем центр масс
        if (GetComponent<Rigidbody>() != null)
        {
            Gizmos.color = Color.magenta;
            Vector3 centerOfMass = transform.TransformPoint(GetComponent<Rigidbody>().centerOfMass);
            Gizmos.DrawWireSphere(centerOfMass, 0.2f);
        }

        // Рейкаст дальномер
        Gizmos.color = Color.green;
        Vector3 rayOrigin = transform.position + transform.TransformDirection(droneSensors.raycastOffset);
        Vector3 rayDirection = -transform.up;

        Gizmos.DrawLine(rayOrigin, rayOrigin + rayDirection * droneSensors.GroundDistance);
        Gizmos.DrawSphere(rayOrigin + rayDirection * droneSensors.GroundDistance, 0.1f);

        // Дополнительно рисуем максимальную дальность
        Gizmos.color = Color.red;
        Gizmos.DrawLine(rayOrigin, rayOrigin + rayDirection * droneSensors.raycastDistance);
    }

    void DebugLines()
    {
        if (!showAngles) return;

        // Направление "вперед" (красный)
        Debug.DrawRay(transform.position, transform.forward * 2f, Color.red);

        // Направление "вверх" (зеленый)
        Debug.DrawRay(transform.position, transform.up * 2f, Color.green);

        // Направление "вправо" (синий)
        Debug.DrawRay(transform.position, transform.right * 2f, Color.blue);

        // Целевое направление (желтый, вертикаль)
        Debug.DrawRay(transform.position, Vector3.up * 3f, Color.yellow);

        // Центр масс (магентовый — линиями крестик вместо сферы)
        if (TryGetComponent(out Rigidbody rb))
        {
            Vector3 com = transform.TransformPoint(rb.centerOfMass);
            float s = 0.2f;
            Debug.DrawLine(com - Vector3.right * s, com + Vector3.right * s, Color.magenta);
            Debug.DrawLine(com - Vector3.up * s, com + Vector3.up * s, Color.magenta);
            Debug.DrawLine(com - Vector3.forward * s, com + Vector3.forward * s, Color.magenta);
        }

        // Рейкаст дальномер (зеленый)
        Vector3 rayOrigin = transform.position + transform.TransformDirection(droneSensors.raycastOffset);
        Vector3 rayDirection = -transform.up;
        Debug.DrawLine(rayOrigin, rayOrigin + rayDirection * droneSensors.GroundDistance, Color.green);

        // Кончик луча — маленький крестик
        Vector3 hitPoint = rayOrigin + rayDirection * droneSensors.GroundDistance;
        float size = 0.05f;
        Debug.DrawLine(hitPoint - Vector3.right * size, hitPoint + Vector3.right * size, Color.green);
        Debug.DrawLine(hitPoint - Vector3.forward * size, hitPoint + Vector3.forward * size, Color.green);

        // Максимальная дальность (красный)
        Debug.DrawLine(rayOrigin, rayOrigin + rayDirection * droneSensors.raycastDistance, Color.red);
    }


    /// <summary>
    /// Принудительная стабилизация для экстренных случаев
    /// </summary>
    public void EmergencyStabilize()
    {
        if (rb != null)
        {
            // Гасим угловую скорость
            rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, Vector3.zero, Time.fixedDeltaTime * 10f);

            // Выравниваем дрон
            Vector3 targetUp = Vector3.up;
            Vector3 currentUp = transform.up;
            Vector3 correctionTorque = Vector3.Cross(currentUp, targetUp) * 100f;
            rb.AddTorque(correctionTorque, ForceMode.Force);
        }
    }

    /// <summary>
    /// Получает диагностическую информацию
    /// </summary>
    public string GetDiagnostics()
    {
        return $"Pitch: {currentPitch:F1}°, Roll: {currentRoll:F1}°, " +
               $"AngVel: {angularVelocity.magnitude:F2}, " +
               $"Motors: FL={motorThrusts[0]:F1}, FR={motorThrusts[1]:F1}, " +
               $"RL={motorThrusts[2]:F1}, RR={motorThrusts[3]:F1}";
    }
}