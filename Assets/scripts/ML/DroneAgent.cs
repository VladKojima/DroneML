using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class DroneAgent : Agent
{
    public TargetSpawner spawner;

    private Vector3 startPos;
    private Quaternion startAng;

    public float thrust;
    public float pitch;
    public float yaw;
    public float roll;

    // Start is called before the first frame update
    void Start()
    {
        startPos = transform.position;
        startAng = transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {

    }

    private IEnumerator RestartDroneCoroutine(Vector3 pos)
    {
        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        transform.SetPositionAndRotation(pos, Quaternion.Euler(0, 0, 0));
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        // 4. ∆дем чтобы моторы "остановились"
        yield return new WaitForSeconds(0.2f);
        rb.isKinematic = false;
    }

    float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }

    public override void OnEpisodeBegin()
    {
        thrust = 0;
        pitch = 0;
        yaw = 0;
        roll = 0;

        var floors = GameObject.FindGameObjectsWithTag("Floor");

        var floor = floors[Random.Range(0, floors.Length)];

        StartCoroutine(RestartDroneCoroutine(floor.transform.position + new Vector3(0, 3, 6)));
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);

        foreach (var raySensor in GetComponentsInChildren<RayPerceptionSensorComponent3D>())
        {
            sensor.AddObservation(raySensor);
        }

        sensor.AddObservation(NormalizeAngle(transform.eulerAngles.x));
        sensor.AddObservation(NormalizeAngle(transform.eulerAngles.z));
        sensor.AddObservation(NormalizeAngle(transform.eulerAngles.y));
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        thrust = actions.ContinuousActions[0];
        pitch = actions.ContinuousActions[1];
        yaw = actions.ContinuousActions[2];
        roll = actions.ContinuousActions[3];

        float currentPitch = Mathf.Abs(NormalizeAngle(transform.eulerAngles.x));
        float currentRoll = Mathf.Abs(NormalizeAngle(transform.eulerAngles.z));
        float currentYaw = Mathf.Abs(NormalizeAngle(transform.eulerAngles.y));

        if (currentPitch >= 85 || currentRoll >= 85)
        {
            AddReward(-1000.0f);
            EndEpisode();
        }

        if (currentPitch <= 70 || currentRoll <= 70)
        {
            AddReward(0.1f);
        }

        AddReward(-Mathf.Abs(yaw) / 10f);
        AddReward(-Mathf.Abs(pitch) / 10f);
        AddReward(-Mathf.Abs(roll) / 10f);
        AddReward(-Mathf.Abs(thrust) / 10f);
    }

    void OnCollisionEnter(Collision collision)
    {
        AddReward(-1000.0f);
        EndEpisode();
    }
}
