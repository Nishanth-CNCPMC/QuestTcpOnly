using UnityEngine;
using UnityEngine.XR;

public class QuestInputReader : MonoBehaviour
{
    public XRNode controllerNode = XRNode.RightHand;
    public XRNode headNode = XRNode.Head;

    private bool previousPrimaryButton;

    public InputDevice Device { get; private set; }
    public InputDevice HeadDevice { get; private set; }
    public InputDevice LeftDevice { get; private set; }
    public InputDevice RightDevice { get; private set; }
    public bool ControllerDetected { get; private set; }
    public bool HasPose { get; private set; }
    public Vector3 Position { get; private set; }
    public Quaternion Rotation { get; private set; } = Quaternion.identity;
    public float Trigger { get; private set; }
    public bool PrimaryButton { get; private set; }
    public bool PrimaryButtonDown { get; private set; }
    public TrackedDeviceState Head { get; private set; }
    public TrackedDeviceState Left { get; private set; }
    public TrackedDeviceState Right { get; private set; }

    public void ReadInput()
    {
        HeadDevice = InputDevices.GetDeviceAtXRNode(headNode);
        LeftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        RightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        Device = controllerNode == XRNode.LeftHand ? LeftDevice : RightDevice;

        Head = ReadDevice(HeadDevice);
        Left = ReadDevice(LeftDevice);
        Right = ReadDevice(RightDevice);

        TrackedDeviceState selected = controllerNode == XRNode.LeftHand ? Left : Right;
        ControllerDetected = selected.detected;

        if (!ControllerDetected)
        {
            HasPose = false;
            Trigger = 0.0f;
            PrimaryButton = false;
            PrimaryButtonDown = false;
            previousPrimaryButton = false;
            return;
        }

        HasPose = selected.hasPose;
        Position = selected.position;
        Rotation = selected.rotation;
        Trigger = selected.trigger;
        PrimaryButton = selected.primaryButton;
        PrimaryButtonDown = PrimaryButton && !previousPrimaryButton;
        previousPrimaryButton = PrimaryButton;
    }

    private static TrackedDeviceState ReadDevice(InputDevice device)
    {
        TrackedDeviceState state = new TrackedDeviceState();
        state.detected = device.isValid;
        state.rotation = Quaternion.identity;

        if (!state.detected)
        {
            return state;
        }

        bool hasPosition = device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position);
        bool hasRotation = device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation);
        state.hasPose = hasPosition && hasRotation;
        if (hasPosition)
        {
            state.position = position;
        }
        if (hasRotation)
        {
            state.rotation = rotation;
        }

        device.TryGetFeatureValue(CommonUsages.trigger, out state.trigger);
        device.TryGetFeatureValue(CommonUsages.grip, out state.grip);
        device.TryGetFeatureValue(CommonUsages.primary2DAxis, out state.joystick);
        device.TryGetFeatureValue(CommonUsages.primaryButton, out state.primaryButton);
        device.TryGetFeatureValue(CommonUsages.secondaryButton, out state.secondaryButton);
        device.TryGetFeatureValue(CommonUsages.triggerButton, out state.triggerButton);
        device.TryGetFeatureValue(CommonUsages.gripButton, out state.gripButton);
        device.TryGetFeatureValue(CommonUsages.menuButton, out state.menuButton);
        device.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out state.joystickClick);
        device.TryGetFeatureValue(CommonUsages.primary2DAxisTouch, out state.joystickTouch);
        return state;
    }

    public struct TrackedDeviceState
    {
        public bool detected;
        public bool hasPose;
        public Vector3 position;
        public Quaternion rotation;
        public float trigger;
        public float grip;
        public Vector2 joystick;
        public bool primaryButton;
        public bool secondaryButton;
        public bool triggerButton;
        public bool gripButton;
        public bool menuButton;
        public bool joystickClick;
        public bool joystickTouch;
    }
}
