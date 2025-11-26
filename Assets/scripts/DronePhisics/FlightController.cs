using UnityEngine;
using System.Collections;

public class FlightController : MonoBehaviour
{
    [Header("Motors")]
    public DroneMotor frontLeftMotor;   // Передний левый
    public DroneMotor frontRightMotor;  // Передний правый
    public DroneMotor rearLeftMotor;    // Задний левый
    public DroneMotor rearRightMotor;   // Задний правый

    [Header("Flight Settings")]
    public float hoverThrust = 0.5f; // Базовая тяга для парения
    public bool stabilizationMode = true;

    [Header("Stabilization PID")]
    public float positionKp = 1.5f;    // Коэффициент для угловой позиции
    public float positionKd = 0.8f;    // Коэффициент для угловой скорости (демпфирование)
    public float velocityKp = 0.3f;    // Дополнительное демпфирование угловой скорости

    [Header("Yaw Control")]
    public float yawTorque = 10f;      // Сила поворота для рысканья

    [Header("Limits")]
    public float maxTiltAngle = 25f;   // Максимальный угол наклона в стаб режиме
    public float thrustSensitivity = 1f;
    public float pitchRollSensitivity = 1f;
    public float yawSensitivity = 1f;

    [Header("Calibration")]
    public bool autoCalibrate = true;
    public float calibrationTime = 3f;

    [Header("Debug")]
    public bool showDebugInfo = false;

    // Входные команды управления
    private float thrustInput = 0f;
    private float pitchInput = 0f;  // Тангаж (наклон вперед/назад)
    private float rollInput = 0f;   // Крен (наклон влево/вправо)
    private float yawInput = 0f;    // Рыскание (поворот)

    // Стабилизация (ТОЛЬКО для pitch и roll)
    private float targetPitch = 0f;
    private float targetRoll = 0f;

    // Фильтры для сглаживания
    private Vector3 filteredAngularVelocity = Vector3.zero;
    private float filterStrength = 0.9f;

    private Rigidbody rb;
    private bool isCalibrated = false;
    private bool isCalibrating = false;

    // Калибровочные данные
    private float[] motorCalibrations = new float[4];

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (autoCalibrate)
        {
            StartCalibration();
        }
        else
        {
            isCalibrated = true;
        }
    }

    void FixedUpdate()
    {
        if (isCalibrating) return;

        // Сглаживаем угловую скорость
        filteredAngularVelocity = Vector3.Lerp(filteredAngularVelocity, rb.angularVelocity, 1f - filterStrength);

        // Применяем управление рысканьем напрямую к дрону (БЕЗ стабилизации)
        ApplyYawControl();

        // Рассчитываем тягу моторов для стабилизации pitch/roll
        CalculateMotorThrusts();

        // Отладочная информация
        if (showDebugInfo)
        {
            DebugInfo();
        }
    }

    /// <summary>
    /// Устанавливает команды управления
    /// </summary>
    public void SetControlInputs(float thrust, float pitch, float roll, float yaw)
    {
        thrustInput = Mathf.Clamp(thrust, -1f, 1f);
        pitchInput = Mathf.Clamp(pitch, -1f, 1f);
        rollInput = Mathf.Clamp(roll, -1f, 1f);
        yawInput = Mathf.Clamp(yaw, -1f, 1f);

        // Обновляем целевые углы ТОЛЬКО для pitch и roll
        if (stabilizationMode)
        {
            targetPitch = pitchInput * maxTiltAngle;
            targetRoll = rollInput * maxTiltAngle;
        }
    }

    /// <summary>
    /// Применяет управление рысканьем напрямую к дрону (БЕЗ стабилизации)
    /// </summary>
    void ApplyYawControl()
    {
        // Прямое управление рысканьем через торсион
        // Никакой стабилизации - только прямое управление
        Vector3 yawTorqueVector = transform.up * yawInput * yawTorque;
        rb.AddTorque(yawTorqueVector, ForceMode.Force);
    }

    /// <summary>
    /// Рассчитывает тягу для каждого мотора (только для pitch/roll)
    /// </summary>
    void CalculateMotorThrusts()
    {
        // Базовая тяга
        float baseThrust = hoverThrust + (thrustInput * thrustSensitivity);

        // Корректировки для управления
        float pitchCorrection = 0f;
        float rollCorrection = 0f;

        if (stabilizationMode)
        {
            // Получаем текущие углы
            Vector3 currentEuler = transform.eulerAngles;
            float currentPitch = NormalizeAngle(currentEuler.x);
            float currentRoll = NormalizeAngle(currentEuler.z);

            // Вычисляем ошибки
            float pitchError = NormalizeAngle(targetPitch - currentPitch);
            float rollError = NormalizeAngle(targetRoll - currentRoll);

            // PD контроллер (без интегральной части для избежания накопления ошибки)
            // P часть - пропорциональна ошибке угла
            // D часть - пропорциональна угловой скорости (демпфирование)

            float pitchAngularVel = filteredAngularVelocity.x * Mathf.Rad2Deg;
            float rollAngularVel = filteredAngularVelocity.z * Mathf.Rad2Deg;

            pitchCorrection = (pitchError * positionKp) - (pitchAngularVel * positionKd);
            rollCorrection = (rollError * positionKp) - (rollAngularVel * positionKd);

            // Дополнительное демпфирование для предотвращения колебаний
            pitchCorrection -= pitchAngularVel * velocityKp;
            rollCorrection -= rollAngularVel * velocityKp;

            // Ограничиваем коррекцию
            pitchCorrection = Mathf.Clamp(pitchCorrection, -baseThrust * 0.5f, baseThrust * 0.5f);
            rollCorrection = Mathf.Clamp(rollCorrection, -baseThrust * 0.5f, baseThrust * 0.5f);
        }
        else
        {
            // Прямое управление без стабилизации
            pitchCorrection = pitchInput * pitchRollSensitivity * baseThrust * 0.3f;
            rollCorrection = rollInput * pitchRollSensitivity * baseThrust * 0.3f;
        }

        // Рассчет тяги для каждого мотора (БЕЗ учета рыскания)
        // Расположение моторов (вид сверху):
        // FL   FR
        //  \ /
        //   X
        //  / \
        // RL   RR

        // ИСПРАВЛЕННЫЕ ЗНАКИ: чтобы наклонить вперед - больше тяги сзади
        float frontLeftThrust = baseThrust + pitchCorrection - rollCorrection;
        float frontRightThrust = baseThrust + pitchCorrection + rollCorrection;
        float rearLeftThrust = baseThrust - pitchCorrection - rollCorrection;
        float rearRightThrust = baseThrust - pitchCorrection + rollCorrection;

        // Нормализуем тягу, чтобы избежать отрицательных значений
        float minThrust = Mathf.Min(frontLeftThrust, frontRightThrust, rearLeftThrust, rearRightThrust);
        if (minThrust < 0f)
        {
            float offset = -minThrust + 0.01f; // Небольшой офсет для избежания нуля
            frontLeftThrust += offset;
            frontRightThrust += offset;
            rearLeftThrust += offset;
            rearRightThrust += offset;
        }

        // При очень малой тяге отключаем моторы полностью (безопасность)
        float minMotorThrust = 0.05f;
        if (baseThrust < minMotorThrust)
        {
            frontLeftThrust = frontRightThrust = rearLeftThrust = rearRightThrust = 0f;
        }

        // Применяем тягу к моторам (нормализуем к 0-1 для DroneMotor)
        if (frontLeftMotor) frontLeftMotor.SetThrust(Mathf.Max(0f, frontLeftThrust) / frontLeftMotor.maxThrust);
        if (frontRightMotor) frontRightMotor.SetThrust(Mathf.Max(0f, frontRightThrust) / frontRightMotor.maxThrust);
        if (rearLeftMotor) rearLeftMotor.SetThrust(Mathf.Max(0f, rearLeftThrust) / rearLeftMotor.maxThrust);
        if (rearRightMotor) rearRightMotor.SetThrust(Mathf.Max(0f, rearRightThrust) / rearRightMotor.maxThrust);
    }

    /// <summary>
    /// Начинает процесс калибровки моторов
    /// </summary>
    /// =====Забил и не доделал====
    public void StartCalibration()
    {
        if (!isCalibrating)
        {
            StartCoroutine(CalibrateMotors());
        }
    }

    /// <summary>
    /// Корутина калибровки моторов
    /// </summary>
    IEnumerator CalibrateMotors()
    {
        isCalibrating = true;
        isCalibrated = false;

        Debug.Log("Начинается калибровка моторов...");

        DroneMotor[] motors = { frontLeftMotor, frontRightMotor, rearLeftMotor, rearRightMotor };

        // Сбрасываем калибровки
        for (int i = 0; i < 4; i++)
        {
            if (motors[i] != null)
            {
                motors[i].SetCalibrationOffset(0f);
                motors[i].SetThrust(hoverThrust);
            }
            motorCalibrations[i] = 0f;
        }

        yield return new WaitForSeconds(1f);

        // Измеряем дрейф при базовой тяге
        Vector3 totalAngularVelocity = Vector3.zero;
        int samples = 0;
        float measurementTime = calibrationTime;

        float startTime = Time.time;
        while (Time.time - startTime < measurementTime)
        {
            totalAngularVelocity += rb.angularVelocity;
            samples++;
            yield return new WaitForFixedUpdate();
        }

        if (samples > 0)
        {
            Vector3 avgAngularVelocity = totalAngularVelocity / samples;

            // Рассчитываем калибровочные поправки
            // Простой алгоритм: корректируем моторы пропорционально измеренному дрейфу
            float calibrationFactor = 0.05f;

            motorCalibrations[0] = (-avgAngularVelocity.z - avgAngularVelocity.x) * calibrationFactor; // FL
            motorCalibrations[1] = (avgAngularVelocity.z - avgAngularVelocity.x) * calibrationFactor;   // FR  
            motorCalibrations[2] = (-avgAngularVelocity.z + avgAngularVelocity.x) * calibrationFactor; // RL
            motorCalibrations[3] = (avgAngularVelocity.z + avgAngularVelocity.x) * calibrationFactor;   // RR

            // Применяем калибровки к моторам
            for (int i = 0; i < 4; i++)
            {
                if (motors[i] != null)
                {
                    motors[i].SetCalibrationOffset(motorCalibrations[i]);
                }
            }
        }

        isCalibrated = true;
        isCalibrating = false;

        Debug.Log($"Калибровка завершена! Поправки: FL={motorCalibrations[0]:F3}, FR={motorCalibrations[1]:F3}, RL={motorCalibrations[2]:F3}, RR={motorCalibrations[3]:F3}");
    }


    /// <summary>
    /// Переключает режим стабилизации
    /// </summary>
    public void ToggleStabilization()
    {
        stabilizationMode = !stabilizationMode;
        if (stabilizationMode)
        {
            // При включении стабилизации сбрасываем целевые углы ТОЛЬКО для pitch и roll
            targetPitch = 0f;
            targetRoll = 0f;
            // Yaw не трогаем - он управляется напрямую
        }
        Debug.Log($"Режим стабилизации: {(stabilizationMode ? "Включен" : "Выключен")}");
    }


    /// <summary>
    /// Нормализует угол в диапазон -180 до 180
    /// </summary>
    float NormalizeAngle(float angle)
    {
        angle = angle % 360f;
        if (angle > 180f) angle -= 360f;
        if (angle < -180f) angle += 360f;
        return angle;
    }

    /// <summary>
    /// Отладочная информация
    /// </summary>
    void DebugInfo()
    {
        Vector3 currentEuler = transform.eulerAngles;
        float currentPitch = NormalizeAngle(currentEuler.x);
        float currentRoll = NormalizeAngle(currentEuler.z);
        float currentYaw = NormalizeAngle(currentEuler.y);

        Debug.Log($"Углы: P={currentPitch:F1}°, R={currentRoll:F1}°, Y={currentYaw:F1}° | " +
                  $"Цели: P={targetPitch:F1}°, R={targetRoll:F1}° | " +
                  $"Угл.скорость: {rb.angularVelocity * Mathf.Rad2Deg}");
    }

    // Геттеры для информации о состоянии
    public bool IsCalibrated() => isCalibrated;
    public bool IsCalibrating() => isCalibrating;
    public bool IsStabilizationEnabled() => stabilizationMode;
    public Vector3 GetCurrentAngles() => new Vector3(
        NormalizeAngle(transform.eulerAngles.x),
        NormalizeAngle(transform.eulerAngles.y),
        NormalizeAngle(transform.eulerAngles.z)
    );
    public Vector3 GetTargetAngles() => new Vector3(targetPitch, 0f, targetRoll); // Yaw цель убрана
}