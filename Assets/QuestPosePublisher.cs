using System.Globalization;
using UnityEngine;

public class QuestPosePublisher : MonoBehaviour
{
    public float sendInterval = 0.05f;
    public bool recording;

    private QuestInputReader inputReader;
    private QuestHud hud;
    private QuestTcpClient tcpClient;
    private float lastSendTime;

    protected virtual void Start()
    {
        inputReader = EnsureComponent<QuestInputReader>();
        hud = EnsureComponent<QuestHud>();
        tcpClient = EnsureComponent<QuestTcpClient>();
    }

    protected virtual void Update()
    {
        inputReader.ReadInput();

        bool appActive = inputReader.Head.detected && inputReader.Head.hasPose;
        bool rightReady = inputReader.Right.detected && inputReader.Right.hasPose;
        string instruction = GetInstruction(appActive, rightReady);

        hud.SetStatus(
            tcpClient.IsConnected,
            inputReader.Right.detected,
            appActive,
            instruction,
            rightReady,
            inputReader.Right.position,
            inputReader.Right.trigger,
            recording);

        if (Time.time - lastSendTime < sendInterval)
        {
            return;
        }

        lastSendTime = Time.time;
        tcpClient.SendLine(BuildMessage(appActive, rightReady));
    }

    private string GetInstruction(bool appActive, bool rightReady)
    {
        if (!appActive)
        {
            return "Waiting for head pose.";
        }

        if (!inputReader.Right.detected)
        {
            return "No right controller detected.";
        }

        if (!rightReady)
        {
            return "Right controller detected, pose unavailable.";
        }

        return "Quest tracking active.";
    }

    private string BuildMessage(bool appActive, bool rightReady)
    {
        string deviceJson = BuildAllDevicesJson();
        string prefix = "{"
            + "\"app_active\":" + B(appActive) + ","
            + "\"head_detected\":" + B(inputReader.Head.detected) + ","
            + "\"right_detected\":" + B(inputReader.Right.detected) + ","
            + "\"left_detected\":" + B(inputReader.Left.detected) + ","
            + "\"origin_set\":" + B(appActive) + ",";

        if (!appActive)
        {
            return prefix
                + "\"message\":\"waiting for head pose\","
                + deviceJson
                + "}";
        }

        if (!inputReader.Right.detected)
        {
            return prefix
                + "\"message\":\"no right controller detected\","
                + deviceJson
                + "}";
        }

        if (!rightReady)
        {
            return prefix
                + "\"message\":\"right controller detected, pose unavailable\","
                + deviceJson
                + "}";
        }

        return prefix
            + "\"message\":\"quest tracking active\","
            + "\"rel_pos\":" + Vec3(inputReader.Right.position) + ","
            + "\"rel_rot\":" + Quat(inputReader.Right.rotation) + ","
            + "\"trigger\":" + F(inputReader.Right.trigger) + ","
            + deviceJson
            + "}";
    }

    private string BuildAllDevicesJson()
    {
        return "\"head\":" + BuildDeviceJson(inputReader.Head, false) + ","
            + "\"left\":" + BuildDeviceJson(inputReader.Left, true) + ","
            + "\"right\":" + BuildDeviceJson(inputReader.Right, true);
    }

    private string BuildDeviceJson(QuestInputReader.TrackedDeviceState state, bool includeControls)
    {
        string json = "{"
            + "\"detected\":" + B(state.detected) + ","
            + "\"has_pose\":" + B(state.hasPose) + ","
            + "\"pos\":" + Vec3(state.position) + ","
            + "\"rot\":" + Quat(state.rotation) + ","
            + "\"has_rel_pose\":" + B(state.hasPose) + ","
            + "\"rel_pos\":" + Vec3(state.position) + ","
            + "\"rel_rot\":" + Quat(state.rotation);

        if (includeControls)
        {
            json += ","
                + "\"trigger\":" + F(state.trigger) + ","
                + "\"grip\":" + F(state.grip) + ","
                + "\"joystick\":" + Vec2(state.joystick) + ","
                + "\"primary_button\":" + B(state.primaryButton) + ","
                + "\"secondary_button\":" + B(state.secondaryButton) + ","
                + "\"trigger_button\":" + B(state.triggerButton) + ","
                + "\"grip_button\":" + B(state.gripButton) + ","
                + "\"menu_button\":" + B(state.menuButton) + ","
                + "\"joystick_click\":" + B(state.joystickClick) + ","
                + "\"joystick_touch\":" + B(state.joystickTouch);
        }

        return json + "}";
    }

    private static string Vec2(Vector2 value)
    {
        return "[" + F(value.x) + "," + F(value.y) + "]";
    }

    private static string Vec3(Vector3 value)
    {
        return "[" + F(value.x) + "," + F(value.y) + "," + F(value.z) + "]";
    }

    private static string Quat(Quaternion value)
    {
        return "[" + F(value.x) + "," + F(value.y) + "," + F(value.z) + "," + F(value.w) + "]";
    }

    private static string B(bool value)
    {
        return value ? "true" : "false";
    }

    private T EnsureComponent<T>() where T : Component
    {
        T component = GetComponent<T>();
        if (component == null)
        {
            component = gameObject.AddComponent<T>();
        }

        return component;
    }

    private static string F(float value)
    {
        return value.ToString("F4", CultureInfo.InvariantCulture);
    }

}
