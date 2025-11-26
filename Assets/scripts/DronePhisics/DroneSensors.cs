using UnityEngine;

public class DroneSensors : MonoBehaviour
{
    [Header("��������� ��������")]
    public bool useGroundRaycast = true;
    public LayerMask groundLayerMask = 1; // ���� �� ���������
    public float raycastDistance = 100f;
    public Vector3 raycastOffset = Vector3.zero;

    [Header("��������� �������� (������ ������)")]
    [SerializeField] private float _absoluteAltitude;
    [SerializeField] private float _pitch;
    [SerializeField] private float _roll;
    [SerializeField] private float _yaw;
    [SerializeField] private float _groundDistance;
    [SerializeField] private Vector3 _attitude; // ����, ������, ��������
    [SerializeField] private Vector3 _localVelocity;
    [SerializeField] private Vector3 _worldVelocity;
    [SerializeField] private Vector3 _angularVelocity;
    [SerializeField] private float _forwardSpeed;
    [SerializeField] private float _rightSpeed;
    [SerializeField] private float _verticalSpeed;
    [SerializeField] private float _totalSpeed;

    private Rigidbody _rb;

    // ��������� �������� ��� ������� � ������ ��������
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
            Debug.LogWarning("Rigidbody �� ������ �� �����. ��������� ������� ����� �������� �����������.");
        }
    }

    void Update()
    {
        UpdateSensors();
    }

    private void UpdateSensors()
    {
        // ���������� ������ (���������� Y � ������� �����������)
        _absoluteAltitude = transform.position.y;

        // ���� ������� (� ��������)
        _pitch = transform.eulerAngles.x;
        _roll = transform.eulerAngles.z;
        _yaw = transform.eulerAngles.y;

        // ������������ ����� � �������� -180 �� 180 ��������
        _pitch = NormalizeAngle(_pitch);
        _roll = NormalizeAngle(_roll);
        _yaw = NormalizeAngle(_yaw);

        // ������ attitude (����, ������, ��������)
        _attitude = new Vector3(_roll, _pitch, _yaw);

        // �������� � ������� ��������
        UpdateVelocityData();

        // ��������� �� �����
        UpdateGroundDistance();
    }

    private void UpdateVelocityData()
    {
        if (_rb != null)
        {
            // ������� ��������
            _worldVelocity = _rb.linearVelocity;

            // ��������� �������� (������������ �����)
            _localVelocity = transform.InverseTransformDirection(_worldVelocity);

            // ������� ��������
            _angularVelocity = _rb.angularVelocity;

            // ���������� �������� � ��������� �����������
            _forwardSpeed = _localVelocity.z;  // ������/�����
            _rightSpeed = _localVelocity.x;    // ������/�����
            _verticalSpeed = _localVelocity.y; // �����/����

            // ����� �������� (������ �������������� ������������)
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

            // ����������� ���� - ���� ������������ �����
            Vector3 rayDirection = -transform.up;

            if (Physics.Raycast(rayOrigin, rayDirection, out hit, raycastDistance, groundLayerMask))
            {
                _groundDistance = hit.distance;
            }
            else
            {
                _groundDistance = raycastDistance; // ������������ ���������
            }
        }
        else
        {
            // �������������� ����� - ������ ���������� ������
            _groundDistance = _absoluteAltitude;
        }
    }

    private float NormalizeAngle(float angle)
    {
        // �������� ���� � ��������� -180 �� 180 ��������
        angle = angle % 360;
        if (angle > 180)
            angle -= 360;
        else if (angle < -180)
            angle += 360;

        return angle;
    }

    // ����� ��� ��������� ���������������� ������� ����������� � �����
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

        return Vector3.up; // ���������� ������ ����� �� ���������
    }

    // ����� ��� ��������, ��������� �� ���� ��� ������
    public bool IsGrounded(float threshold = 0.1f)
    {
        return _groundDistance <= threshold;
    }

    // ����� ��� ��������� �������� � ��������� ��������� �����������
    public float GetSpeedInDirection(Vector3 localDirection)
    {
        return Vector3.Dot(_localVelocity, localDirection.normalized);
    }
}