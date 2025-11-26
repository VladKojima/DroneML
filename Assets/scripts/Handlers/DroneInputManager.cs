using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DroneInputManager : MonoBehaviour
{
    [Header("Flight Controller")]
    public FlightController flightController;

    [Header("Java Input")]
    public JavaInputReceiver javaInputReceiver;

    [Header("Input Settings")]
    public InputMode currentInputMode = InputMode.Keyboard;
    public bool invertPitch = false;
    public bool invertRoll = false;
    public bool invertYaw = false;

    [Header("Keyboard Controls")]
    public KeyCode thrustUpKey = KeyCode.Space;
    public KeyCode thrustDownKey = KeyCode.LeftShift;
    public KeyCode pitchForwardKey = KeyCode.W;
    public KeyCode pitchBackwardKey = KeyCode.S;
    public KeyCode rollLeftKey = KeyCode.A;
    public KeyCode rollRightKey = KeyCode.D;
    public KeyCode yawLeftKey = KeyCode.Q;
    public KeyCode yawRightKey = KeyCode.E;
    public KeyCode calibrateKey = KeyCode.C;
    public KeyCode toggleStabilizationKey = KeyCode.T;

    [Header("Gamepad Controls")]
    public bool useGamepad = false;
    public string thrustAxis = "Vertical"; // Левый стик Y или триггеры
    public string pitchAxis = "Vertical2"; // Правый стик Y
    public string rollAxis = "Horizontal2"; // Правый стик X
    public string yawAxis = "Horizontal"; // Левый стик X или плечевые кнопки

    [Header("Virtual Joystick")]
    public VirtualJoystick leftJoystick; // Для тяги и рыскания
    public VirtualJoystick rightJoystick; // Для тангажа и крена

    [Header("Neurointerface")]
    public DroneAgent agent;


    [Header("Sensitivity")]
    [Range(0.1f, 3f)]
    public float inputSensitivity = 1f;

    // Текущие входные значения
    private float thrust = 0f;
    private float pitch = 0f;
    private float roll = 0f;
    private float yaw = 0f;

    // Сглаживание ввода
    [Header("Input Smoothing")]
    public float inputSmoothing = 0.1f;
    private float smoothThrust, smoothPitch, smoothRoll, smoothYaw;

    public enum InputMode
    {
        Keyboard,
        Gamepad,
        VirtualJoystick,
        Java,
        Neurointerface,
    }

    public List<string> GetInputModes()
    {
        var modes = Enum.GetNames(typeof(InputMode)).ToList();
        return modes;
    }

    public void SetInputMode(int value)
    {
        currentInputMode = (InputMode)value;
    }


    void Start()
    {
        if (flightController == null)
            flightController = GetComponent<FlightController>();

        if (flightController == null)
        {
            Debug.LogError("DroneInputManager: FlightController не найден!");
        }
    }

    void Update()
    {
        // Обработка переключения режимов
        HandleModeToggle();

        // Получение ввода в зависимости от режима
        switch (currentInputMode)
        {
            case InputMode.Keyboard: HandleKeyboardInput(); break;
            case InputMode.Gamepad: HandleGamepadInput(); break;
            case InputMode.VirtualJoystick: HandleVirtualJoystickInput(); break;
            case InputMode.Java: HandleJavaInput(); break;
            case InputMode.Neurointerface: HandleNeuroInput(); break;
        }

        // Применяем инверсию если нужно
        if (invertPitch) pitch = -pitch;
        if (invertRoll) roll = -roll;
        if (invertYaw) yaw = -yaw;

        // Сглаживание ввода
        smoothThrust = Mathf.Lerp(smoothThrust, thrust, Time.deltaTime / inputSmoothing);
        smoothPitch = Mathf.Lerp(smoothPitch, pitch, Time.deltaTime / inputSmoothing);
        smoothRoll = Mathf.Lerp(smoothRoll, roll, Time.deltaTime / inputSmoothing);
        smoothYaw = Mathf.Lerp(smoothYaw, yaw, Time.deltaTime / inputSmoothing);

        // Передаем команды в полетный контроллер
        if (flightController != null)
        {
            flightController.SetControlInputs(
                smoothThrust * inputSensitivity,
                smoothPitch * inputSensitivity,
                smoothRoll * inputSensitivity,
                smoothYaw * inputSensitivity
            );
        }
    }

    void HandleModeToggle()
    {
        // Обработка специальных клавиш
        if (Input.GetKeyDown(calibrateKey) && flightController != null)
        {
            flightController.StartCalibration();
        }

        if (Input.GetKeyDown(toggleStabilizationKey) && flightController != null)
        {
            flightController.ToggleStabilization();
        }

        // Переключение между режимами
        if (Input.GetKeyDown(KeyCode.Alpha1))
            currentInputMode = InputMode.Keyboard;
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            currentInputMode = InputMode.Gamepad;
        else if (Input.GetKeyDown(KeyCode.Alpha3))
            currentInputMode = InputMode.VirtualJoystick;
        else if (Input.GetKeyDown(KeyCode.Alpha4))
            currentInputMode = InputMode.Java;
    }

    void HandleKeyboardInput()
    {
        // Тяга
        thrust = 0f;
        if (Input.GetKey(thrustUpKey))
            thrust += 1f;
        if (Input.GetKey(thrustDownKey))
            thrust -= 1f;

        // Тангаж (наклон вперед/назад)
        pitch = 0f;
        if (Input.GetKey(pitchForwardKey))
            pitch += 1f;
        if (Input.GetKey(pitchBackwardKey))
            pitch -= 1f;

        // Крен (наклон влево/вправо)
        roll = 0f;
        if (Input.GetKey(rollLeftKey))
            roll += 1f;
        if (Input.GetKey(rollRightKey))
            roll -= 1f;

        // Рыскание (поворот)
        yaw = 0f;
        if (Input.GetKey(yawLeftKey))
            yaw -= 1f;
        if (Input.GetKey(yawRightKey))
            yaw += 1f;
    }

    void HandleGamepadInput()
    {
        if (!useGamepad) return;

        // Получаем ввод с геймпада
        thrust = Input.GetAxis(thrustAxis);
        pitch = Input.GetAxis(pitchAxis);
        roll = Input.GetAxis(rollAxis);
        yaw = Input.GetAxis(yawAxis);
    }

    void HandleVirtualJoystickInput()
    {
        if (leftJoystick != null)
        {
            thrust = leftJoystick.Vertical;
            yaw = leftJoystick.Horizontal;
        }
        else
        {
            thrust = 0f;
            yaw = 0f;
        }

        if (rightJoystick != null)
        {
            pitch = rightJoystick.Vertical;
            roll = -rightJoystick.Horizontal;
        }
        else
        {
            pitch = 0f;
            roll = 0f;
        }
    }


    void HandleJavaInput()
    {
        if (javaInputReceiver != null)
        {
            thrust = javaInputReceiver.thrust;
            pitch = javaInputReceiver.pitch;
            roll = javaInputReceiver.roll;
            yaw = javaInputReceiver.yaw;
        }
        else
        {
            thrust = pitch = roll = yaw = 0f;
        }
    }

    void HandleNeuroInput()
    {
        if (agent != null)
        {
            thrust = agent.thrust;
            pitch = agent.pitch;
            roll = agent.roll;
            yaw = agent.yaw;
        }
    }

    /// <summary>
    /// Устанавливает режим ввода программно
    /// </summary>
    public void SetInputMode(InputMode mode)
    {
        currentInputMode = mode;
        Debug.Log($"Режим ввода изменен на: {mode}");
    }

    /// <summary>
    /// Получает текущие значения ввода (для отладки)
    /// </summary>
    public Vector4 GetCurrentInput()
    {
        return new Vector4(smoothThrust, smoothPitch, smoothRoll, smoothYaw);
    }

    /// <summary>
    /// Принудительно устанавливает значения ввода
    /// </summary>
    public void SetManualInput(float thrustValue, float pitchValue, float rollValue, float yawValue)
    {
        thrust = Mathf.Clamp(thrustValue, -1f, 1f);
        pitch = Mathf.Clamp(pitchValue, -1f, 1f);
        roll = Mathf.Clamp(rollValue, -1f, 1f);
        yaw = Mathf.Clamp(yawValue, -1f, 1f);
    }

    void OnGUI()
    {
        return;
        // Отображение информации о текущем режиме
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label($"Режим ввода: {currentInputMode}");
        GUILayout.Label($"Тяга: {smoothThrust:F2}");
        GUILayout.Label($"Тангаж: {smoothPitch:F2}");
        GUILayout.Label($"Крен: {smoothRoll:F2}");
        GUILayout.Label($"Рыскание: {smoothYaw:F2}");

        if (flightController != null)
        {
            GUILayout.Label($"Стабилизация: {(flightController.IsStabilizationEnabled() ? "ВКЛ" : "ВЫКЛ")}");
            GUILayout.Label($"Калибровка: {(flightController.IsCalibrated() ? "Готов" : "Не готов")}");
        }

        GUILayout.Label("Клавиши: 1-4 (режимы), C (калибровка), T (стабилизация)");
        GUILayout.EndArea();
    }
}
