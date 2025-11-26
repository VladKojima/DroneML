using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Handlers
{
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
        public string thrustAxis = "Vertical";
        public string pitchAxis = "Vertical2";
        public string rollAxis = "Horizontal2";
        public string yawAxis = "Horizontal";

        [Header("Virtual Joystick")]
        public VirtualJoystick leftJoystick;
        public VirtualJoystick rightJoystick;

        [Header("Neurointerface")]
        public DroneAgent agent;


        [Header("Sensitivity")]
        [Range(0.1f, 3f)]
        public float inputSensitivity = 1f;

        private float _thrust = 0f;
        private float _pitch = 0f;
        private float _roll = 0f;
        private float _yaw = 0f;

        [Header("Input Smoothing")]
        public float inputSmoothing = 0.1f;
        private float _smoothThrust, _smoothPitch, _smoothRoll, _smoothYaw;

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
                Debug.LogError("DroneInputManager: FlightController ?? ??????!");
            }
        }

        void Update()
        {
            HandleModeToggle();

            switch (currentInputMode)
            {
                case InputMode.Keyboard: HandleKeyboardInput(); break;
                case InputMode.Gamepad: HandleGamepadInput(); break;
                case InputMode.VirtualJoystick: HandleVirtualJoystickInput(); break;
                case InputMode.Java: HandleJavaInput(); break;
                case InputMode.Neurointerface: HandleNeuroInput(); break;
            }

            if (invertPitch) _pitch = -_pitch;
            if (invertRoll) _roll = -_roll;
            if (invertYaw) _yaw = -_yaw;

            _smoothThrust = Mathf.Lerp(_smoothThrust, _thrust, Time.deltaTime / inputSmoothing);
            _smoothPitch = Mathf.Lerp(_smoothPitch, _pitch, Time.deltaTime / inputSmoothing);
            _smoothRoll = Mathf.Lerp(_smoothRoll, _roll, Time.deltaTime / inputSmoothing);
            _smoothYaw = Mathf.Lerp(_smoothYaw, _yaw, Time.deltaTime / inputSmoothing);

            if (flightController != null)
            {
                flightController.SetControlInputs(
                    _smoothThrust * inputSensitivity,
                    _smoothPitch * inputSensitivity,
                    _smoothRoll * inputSensitivity,
                    _smoothYaw * inputSensitivity
                );
            }
        }

        void HandleModeToggle()
        {
            if (Input.GetKeyDown(calibrateKey) && flightController != null)
            {
                flightController.StartCalibration();
            }

            if (Input.GetKeyDown(toggleStabilizationKey) && flightController != null)
            {
                flightController.ToggleStabilization();
            }

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
            _thrust = 0f;
            if (Input.GetKey(thrustUpKey))
                _thrust += 1f;
            if (Input.GetKey(thrustDownKey))
                _thrust -= 1f;

            _pitch = 0f;
            if (Input.GetKey(pitchForwardKey))
                _pitch += 1f;
            if (Input.GetKey(pitchBackwardKey))
                _pitch -= 1f;

            _roll = 0f;
            if (Input.GetKey(rollLeftKey))
                _roll += 1f;
            if (Input.GetKey(rollRightKey))
                _roll -= 1f;

            _yaw = 0f;
            if (Input.GetKey(yawLeftKey))
                _yaw -= 1f;
            if (Input.GetKey(yawRightKey))
                _yaw += 1f;
        }

        void HandleGamepadInput()
        {
            if (!useGamepad) return;

            _thrust = Input.GetAxis(thrustAxis);
            _pitch = Input.GetAxis(pitchAxis);
            _roll = Input.GetAxis(rollAxis);
            _yaw = Input.GetAxis(yawAxis);
        }

        void HandleVirtualJoystickInput()
        {
            if (leftJoystick != null)
            {
                _thrust = leftJoystick.Vertical;
                _yaw = leftJoystick.Horizontal;
            }
            else
            {
                _thrust = 0f;
                _yaw = 0f;
            }

            if (rightJoystick != null)
            {
                _pitch = rightJoystick.Vertical;
                _roll = -rightJoystick.Horizontal;
            }
            else
            {
                _pitch = 0f;
                _roll = 0f;
            }
        }


        void HandleJavaInput()
        {
            if (javaInputReceiver != null)
            {
                _thrust = javaInputReceiver.thrust;
                _pitch = javaInputReceiver.pitch;
                _roll = javaInputReceiver.roll;
                _yaw = javaInputReceiver.yaw;
            }
            else
            {
                _thrust = _pitch = _roll = _yaw = 0f;
            }
        }

        void HandleNeuroInput()
        {
            if (agent != null)
            {
                _thrust = agent.thrust;
                _pitch = agent.pitch;
                _roll = agent.roll;
                _yaw = agent.yaw;
            }
        }

        public void SetInputMode(InputMode mode)
        {
            currentInputMode = mode;
            Debug.Log($"????? ????? ??????? ??: {mode}");
        }

        public Vector4 GetCurrentInput()
        {
            return new Vector4(_smoothThrust, _smoothPitch, _smoothRoll, _smoothYaw);
        }

        public void SetManualInput(float thrustValue, float pitchValue, float rollValue, float yawValue)
        {
            _thrust = Mathf.Clamp(thrustValue, -1f, 1f);
            _pitch = Mathf.Clamp(pitchValue, -1f, 1f);
            _roll = Mathf.Clamp(rollValue, -1f, 1f);
            _yaw = Mathf.Clamp(yawValue, -1f, 1f);
        }

        void OnGUI()
        {
            return;
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label($"????? ?????: {currentInputMode}");
            GUILayout.Label($"????: {_smoothThrust:F2}");
            GUILayout.Label($"??????: {_smoothPitch:F2}");
            GUILayout.Label($"????: {_smoothRoll:F2}");
            GUILayout.Label($"????????: {_smoothYaw:F2}");

            if (flightController != null)
            {
                GUILayout.Label($"????????????: {(flightController.IsStabilizationEnabled() ? "???" : "????")}");
                GUILayout.Label($"??????????: {(flightController.IsCalibrated() ? "?????" : "?? ?????")}");
            }

            GUILayout.Label("???????: 1-4 (??????), C (??????????), T (????????????)");
            GUILayout.EndArea();
        }
    }
}
