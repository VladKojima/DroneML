using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Основные настройки")]
    [SerializeField] private Transform target; // объект, за которым следим
    [SerializeField] private Vector3 offset = new Vector3(0, 5, -10); // смещение камеры
    [SerializeField] private float smoothSpeed = 0.125f; // плавность движения

    [Header("Вращение камеры")]
    [SerializeField] private float rotationSpeed = 2f; // скорость вращения
    [SerializeField] private float minVerticalAngle = -80f; // минимальный угол наклона
    [SerializeField] private float maxVerticalAngle = 80f; // максимальный угол наклона
    [SerializeField] private float distance = 10f; // расстояние до цели
    [SerializeField] private bool invertY = false; // инвертировать ось Y

    private float currentRotationX = 0f;
    private float currentRotationY = 0f;
    private Vector3 currentOffset;

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("Target не назначен для CameraFollow!");
            return;
        }

        // Инициализируем текущее смещение
        currentOffset = offset;

        // Инициализируем углы вращения на основе начального положения камеры
        Vector3 directionToCamera = transform.position - target.position;
        distance = directionToCamera.magnitude;

        currentRotationY = transform.eulerAngles.y;
        currentRotationX = transform.eulerAngles.x;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Обработка вращения камеры
        HandleCameraRotation();

        // Вычисляем позицию камеры
        Vector3 desiredPosition = CalculateCameraPosition();

        // Плавное перемещение
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;

        // Камера всегда смотрит на цель
        transform.LookAt(target);
    }

    private void HandleCameraRotation()
    {
        // Проверяем, нажата ли правая кнопка мыши для вращения
        if (Input.GetMouseButton(1)) // Правая кнопка мыши
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            // Вращение по горизонтали
            currentRotationY += mouseX * rotationSpeed;

            // Вращение по вертикали (с инверсией если нужно)
            float verticalInput = invertY ? -mouseY : mouseY;
            currentRotationX += verticalInput * rotationSpeed;

            // Ограничиваем вертикальный угол
            currentRotationX = Mathf.Clamp(currentRotationX, minVerticalAngle, maxVerticalAngle);
        }

        // Возможность изменения дистанции колесиком мыши
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            distance = Mathf.Clamp(distance - scroll * 5f, 2f, 50f);
        }
    }

    private Vector3 CalculateCameraPosition()
    {
        // Создаем вращение на основе текущих углов
        Quaternion rotation = Quaternion.Euler(currentRotationX, currentRotationY, 0);

        // Вычисляем позицию камеры относительно цели
        Vector3 cameraPosition = target.position + rotation * new Vector3(0, 0, -distance);

        return cameraPosition;
    }

    // Метод для сброса камеры в начальное положение
    public void ResetCamera()
    {
        currentRotationX = 0f;
        currentRotationY = 0f;
        distance = offset.magnitude;
    }

    // Метод для установки новой цели
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    // Метод для получения текущей дистанции
    public float GetCurrentDistance()
    {
        return distance;
    }

    // Метод для установки дистанции
    public void SetDistance(float newDistance)
    {
        distance = Mathf.Clamp(newDistance, 2f, 50f);
    }
}