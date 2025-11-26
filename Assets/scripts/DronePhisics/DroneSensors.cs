using UnityEngine;

public class DroneSensors : MonoBehaviour
{
    [Header("Настройки датчиков")]
    public bool useGroundRaycast = true;
    public LayerMask groundLayerMask = 1; // Слой по умолчанию
    public float raycastDistance = 100f;
    public Vector3 raycastOffset = Vector3.zero;

    [Header("Показания датчиков (только чтение)")]
    [SerializeField] private float _absoluteAltitude;
    [SerializeField] private float _pitch;
    [SerializeField] private float _roll;
    [SerializeField] private float _yaw;
    [SerializeField] private float _groundDistance;
    [SerializeField] private Vector3 _attitude; // Крен, тангаж, рыскание
    [SerializeField] private Vector3 _localVelocity;
    [SerializeField] private Vector3 _worldVelocity;
    [SerializeField] private Vector3 _angularVelocity;
    [SerializeField] private float _forwardSpeed;
    [SerializeField] private float _rightSpeed;
    [SerializeField] private float _verticalSpeed;
    [SerializeField] private float _totalSpeed;

    private Rigidbody _rb;

    // Публичные свойства для доступа к данным датчиков
    public float AbsoluteAltitude => _absoluteAltitude;
    public float Pitch => _pitch;
    public float Roll => _roll;
    public float Yaw => _yaw;
    public float GroundDistance => _groundDistance;
    public Vector3 Attitude => _attitude;
    public Vector3 LocalVelocity => _localVelocity;
    public Vector3 WorldVelocity => _worldVelocity;
    public Vector3 AngularVelocity => _angularVelocity;
    public float ForwardSpeed => _forwardSpeed;
    public float RightSpeed => _rightSpeed;
    public float VerticalSpeed => _verticalSpeed;
    public float TotalSpeed => _totalSpeed;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb == null)
        {
            Debug.LogWarning("Rigidbody не найден на дроне. Некоторые датчики могут работать некорректно.");
        }
    }

    void Update()
    {
        UpdateSensors();
    }

    private void UpdateSensors()
    {
        // Абсолютная высота (координата Y в мировых координатах)
        _absoluteAltitude = transform.position.y;

        // Углы наклона (в градусах)
        _pitch = transform.eulerAngles.x;
        _roll = transform.eulerAngles.z;
        _yaw = transform.eulerAngles.y;

        // Нормализация углов в диапазон -180 до 180 градусов
        _pitch = NormalizeAngle(_pitch);
        _roll = NormalizeAngle(_roll);
        _yaw = NormalizeAngle(_yaw);

        // Вектор attitude (крен, тангаж, рыскание)
        _attitude = new Vector3(_roll, _pitch, _yaw);

        // Скорость и угловая скорость
        UpdateVelocityData();

        // Дальномер до земли
        UpdateGroundDistance();
    }

    private void UpdateVelocityData()
    {
        if (_rb != null)
        {
            // Мировая скорость
            _worldVelocity = _rb.velocity;

            // Локальная скорость (относительно дрона)
            _localVelocity = transform.InverseTransformDirection(_worldVelocity);

            // Угловая скорость
            _angularVelocity = _rb.angularVelocity;

            // Компоненты скорости в локальных координатах
            _forwardSpeed = _localVelocity.z;  // Вперед/назад
            _rightSpeed = _localVelocity.x;    // Вправо/влево
            _verticalSpeed = _localVelocity.y; // Вверх/вниз

            // Общая скорость (только горизонтальная составляющая)
            _totalSpeed = new Vector3(_localVelocity.x, 0, _localVelocity.z).magnitude;
        }
        else
        {
            _worldVelocity = Vector3.zero;
            _localVelocity = Vector3.zero;
            _angularVelocity = Vector3.zero;
            _forwardSpeed = 0f;
            _rightSpeed = 0f;
            _verticalSpeed = 0f;
            _totalSpeed = 0f;
        }
    }

    private void UpdateGroundDistance()
    {
        if (useGroundRaycast)
        {
            RaycastHit hit;
            Vector3 rayOrigin = transform.position + transform.TransformDirection(raycastOffset);

            // Направление луча - вниз относительно дрона
            Vector3 rayDirection = -transform.up;

            if (Physics.Raycast(rayOrigin, rayDirection, out hit, raycastDistance, groundLayerMask))
            {
                _groundDistance = hit.distance;
            }
            else
            {
                _groundDistance = raycastDistance; // Максимальная дальность
            }
        }
        else
        {
            // Альтернативный метод - просто используем высоту
            _groundDistance = _absoluteAltitude;
        }
    }

    private float NormalizeAngle(float angle)
    {
        // Приводим угол к диапазону -180 до 180 градусов
        angle = angle % 360;
        if (angle > 180)
            angle -= 360;
        else if (angle < -180)
            angle += 360;

        return angle;
    }

    // Метод для получения нормализованного вектора направления к земле
    public Vector3 GetGroundNormal()
    {
        if (useGroundRaycast)
        {
            RaycastHit hit;
            Vector3 rayOrigin = transform.position + transform.TransformDirection(raycastOffset);
            Vector3 rayDirection = -transform.up;

            if (Physics.Raycast(rayOrigin, rayDirection, out hit, raycastDistance, groundLayerMask))
            {
                return hit.normal;
            }
        }

        return Vector3.up; // Возвращаем вектор вверх по умолчанию
    }

    // Метод для проверки, находится ли дрон над землей
    public bool IsGrounded(float threshold = 0.1f)
    {
        return _groundDistance <= threshold;
    }

    // Метод для получения скорости в указанном локальном направлении
    public float GetSpeedInDirection(Vector3 localDirection)
    {
        return Vector3.Dot(_localVelocity, localDirection.normalized);
    }
}