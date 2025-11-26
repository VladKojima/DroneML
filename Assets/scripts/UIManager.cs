using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [SerializeField]
    private Vector3 resetPosition;
    public GameObject drone;
    public DroneDebugMonitor debugMonitor;

    public DroneInputManager InputManager;
    public TMP_Dropdown controlsDropdown;

    // Start is called before the first frame update
    void Start()
    {
        var modes = InputManager.GetInputModes();
        controlsDropdown.AddOptions(modes);
        controlsDropdown.value = (int)InputManager.currentInputMode;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnResetButtonDown()
    {
        StartCoroutine(RestartDroneCoroutine());
    }
    private IEnumerator RestartDroneCoroutine()
    {
        var rb = drone.GetComponent<Rigidbody>();
        rb.isKinematic = true;
        drone.transform.SetPositionAndRotation(resetPosition, Quaternion.Euler(0, 0, 0));
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        // 4. ∆дем чтобы моторы "остановились"
        yield return new WaitForSeconds(0.2f);
        rb.isKinematic = false;
    }
    public void OnSetStartPositionButtonDown()
    {
        resetPosition = drone.transform.position;
    }

    public void OnShowDebugChanged(bool value)
    {
        debugMonitor.showDebugInfo = value;
    }

    public void OnInputModeChanged(int value)
    {
        InputManager.SetInputMode(value);
    }
    
}
