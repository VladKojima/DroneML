using TMPro;
using UnityEngine;

public class DroneMotor : MonoBehaviour
{
    [Header("Motor Settings")]
    public float maxThrust = 50f;
    public float motorResponseTime = 0.1f;

    [Header("Calibration")]
    public float calibrationOffset = 0f; // Калибровочное смещение для этого мотора

    private float currentThrust = 0f;
    private float targetThrust = 0f;
    private Rigidbody droneRigidbody;

    [Header("Debug")]
    public bool showForceGizmo = true;

    void Start()
    {
        // Получаем Rigidbody дрона (должен быть на родительском объекте)
        droneRigidbody = GetComponentInParent<Rigidbody>();
        if (droneRigidbody == null)
        {
            Debug.LogError("DroneMotor: Не найден Rigidbody на родительском объекте!");
        }
    }

    void FixedUpdate()
    {
        // Плавно изменяем текущую тягу к целевой
        currentThrust = Mathf.Lerp(currentThrust, targetThrust, Time.fixedDeltaTime / motorResponseTime);

        // Применяем силу с учетом калибровочного смещения
        float adjustedThrust = currentThrust + calibrationOffset;

        if (droneRigidbody != null && adjustedThrust > 0)
        {
            Vector3 force = transform.up * adjustedThrust;
            droneRigidbody.AddForceAtPosition(force, transform.position);
        }
    }

    private void Update()
    {
        DebugLines();
    }

    /// <summary>
    /// Устанавливает тягу мотора в абсолютных единицах
    /// </summary>
    public void SetThrust(float thrust)
    {
        // Убираем нормализацию - принимаем абсолютные значения
        targetThrust = Mathf.Clamp(thrust, 0f, maxThrust);
    }

    /// <summary>
    /// Устанавливает нормализованную тягу мотора (0-1)
    /// </summary>
    public void SetNormalizedThrust(float normalizedThrust)
    {
        targetThrust = Mathf.Clamp01(normalizedThrust) * maxThrust;
    }

    /// <summary>
    /// Получает текущую тягу мотора
    /// </summary>
    public float GetCurrentThrust()
    {
        return currentThrust;
    }

    /// <summary>
    /// Получает целевую тягу мотора
    /// </summary>
    public float GetTargetThrust()
    {
        return targetThrust;
    }

    /// <summary>
    /// Получает нормализованную текущую тягу (0-1)
    /// </summary>
    public float GetCurrentThrustNormalized()
    {
        return currentThrust / maxThrust;
    }

    /// <summary>
    /// Устанавливает калибровочное смещение
    /// </summary>
    public void SetCalibrationOffset(float offset)
    {
        calibrationOffset = offset;
    }

    /// <summary>
    /// Получает калибровочное смещение
    /// </summary>
    public float GetCalibrationOffset()
    {
        return calibrationOffset;
    }

    void OnDrawGizmos()
    {// ВЫКЛЮЧЕНО
        if (false && showForceGizmo && Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            float gizmoLength = (currentThrust + calibrationOffset) * 0.02f; // Уменьшил масштаб для лучшей видимости
            Gizmos.DrawRay(transform.position, transform.up * gizmoLength);

            // Показываем текущую тягу текстом
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + transform.up * gizmoLength,
                $"{currentThrust:F1}N");
#endif
        }
    }

    void DebugLines()
    {
        if (!showForceGizmo) return;

        float gizmoLength = (currentThrust + calibrationOffset) * 0.05f;

        Debug.DrawRay(transform.position, transform.up * gizmoLength, Color.yellow);
    }
}