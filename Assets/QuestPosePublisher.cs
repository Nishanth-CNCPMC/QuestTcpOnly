using System.Globalization;
using UnityEngine;

public class QuestPosePublisher : MonoBehaviour
{
    public float sendInterval = 0.05f;
    public bool recording;
    public bool rosTopicEnable = true;
    public float panelAdjustSpeedMetersPerSecond = 0.75f;
    public float panelMinDistanceMeters = 0.6f;
    public float panelMaxDistanceMeters = 5.0f;

    private QuestInputReader inputReader;
    private QuestHud hud;
    private QuestSettingsDialog settingsDialog;
    private StreamPanelManager streamPanel;
    private QuestTcpClient tcpClient;
    private QuestHapticCommandReceiver hapticCommandReceiver;
    private float lastSendTime;
    private bool previousLeftMenuButton;
    private bool previousRightPrimaryButton;
    private bool previousRightSecondaryButton;
    private bool previousRightTriggerButton;
    private bool settingsOpen;
    private bool adjustingStreamPanel;
    private int selectedSettingsOption;
    private Vector3 originalPanelLocalPosition;

    private const int AdjustPanelOption = 0;
    private const int SaveOption = 1;
    private const int DiscardOption = 2;
    private const int SettingsOptionCount = 3;

    protected virtual void Start()
    {
        inputReader = EnsureComponent<QuestInputReader>();
        hud = EnsureComponent<QuestHud>();
        settingsDialog = EnsureComponent<QuestSettingsDialog>();
        tcpClient = EnsureComponent<QuestTcpClient>();
        hapticCommandReceiver = EnsureComponent<QuestHapticCommandReceiver>();
        settingsDialog.SetVisible(false);
    }

    protected virtual void Update()
    {
        inputReader.ReadInput();
        UpdateSettingsInput();

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
            recording,
            rosTopicEnable);

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
            + "\"ros_topic_enable\":" + B(rosTopicEnable) + ","
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

    private void UpdateSettingsInput()
    {
        bool leftMenuDown = inputReader.Left.detected
            && inputReader.Left.menuButton
            && !previousLeftMenuButton;
        bool rightPrimaryDown = inputReader.Right.detected
            && inputReader.Right.primaryButton
            && !previousRightPrimaryButton;
        bool rightSecondaryDown = inputReader.Right.detected
            && inputReader.Right.secondaryButton
            && !previousRightSecondaryButton;
        bool rightTriggerDown = inputReader.Right.detected
            && inputReader.Right.triggerButton
            && !previousRightTriggerButton;

        previousLeftMenuButton = inputReader.Left.detected && inputReader.Left.menuButton;
        previousRightPrimaryButton = inputReader.Right.detected && inputReader.Right.primaryButton;
        previousRightSecondaryButton = inputReader.Right.detected && inputReader.Right.secondaryButton;
        previousRightTriggerButton = inputReader.Right.detected && inputReader.Right.triggerButton;

        if (leftMenuDown)
        {
            OpenSettings();
        }

        if (!settingsOpen)
        {
            settingsDialog.SetVisible(false);
            return;
        }

        if (rightPrimaryDown)
        {
            selectedSettingsOption = (selectedSettingsOption + 1) % SettingsOptionCount;
        }

        if (rightSecondaryDown)
        {
            selectedSettingsOption = (selectedSettingsOption + SettingsOptionCount - 1) % SettingsOptionCount;
        }

        if (rightTriggerDown)
        {
            ActivateSelectedSettingsOption();
        }

        if (adjustingStreamPanel)
        {
            AdjustStreamPanel();
        }

        UpdateSettingsDialogText();
    }

    private void OpenSettings()
    {
        EnsureStreamPanel();

        if (!settingsOpen)
        {
            originalPanelLocalPosition = streamPanel != null
                ? streamPanel.GetLocalPosition()
                : Vector3.zero;
            selectedSettingsOption = AdjustPanelOption;
            adjustingStreamPanel = false;
        }

        settingsOpen = true;
        rosTopicEnable = false;
        settingsDialog.SetVisible(true);
        UpdateSettingsDialogText();
    }

    private void ActivateSelectedSettingsOption()
    {
        if (selectedSettingsOption == AdjustPanelOption)
        {
            adjustingStreamPanel = !adjustingStreamPanel;
            return;
        }

        if (selectedSettingsOption == SaveOption)
        {
            CloseSettings(true);
            return;
        }

        if (selectedSettingsOption == DiscardOption)
        {
            CloseSettings(false);
        }
    }

    private void CloseSettings(bool saveChanges)
    {
        if (!saveChanges && streamPanel != null)
        {
            streamPanel.SetLocalPosition(originalPanelLocalPosition);
        }
        else if (saveChanges && streamPanel != null)
        {
            streamPanel.SaveLocalPosition();
        }

        settingsOpen = false;
        adjustingStreamPanel = false;
        rosTopicEnable = true;
        settingsDialog.SetVisible(false);
    }

    private void AdjustStreamPanel()
    {
        EnsureStreamPanel();
        if (streamPanel == null)
        {
            return;
        }

        Vector3 position = streamPanel.GetLocalPosition();
        Vector2 leftStick = inputReader.Left.detected ? inputReader.Left.joystick : Vector2.zero;
        Vector2 rightStick = inputReader.Right.detected ? inputReader.Right.joystick : Vector2.zero;
        float step = panelAdjustSpeedMetersPerSecond * Time.deltaTime;

        position.x += leftStick.x * step;
        position.z += leftStick.y * step;
        position.y += rightStick.y * step;
        position.z = Mathf.Clamp(position.z, panelMinDistanceMeters, panelMaxDistanceMeters);

        streamPanel.SetLocalPosition(position);
    }

    private void UpdateSettingsDialogText()
    {
        EnsureStreamPanel();
        Vector3 panelPosition = streamPanel != null ? streamPanel.GetLocalPosition() : Vector3.zero;
        settingsDialog.SetVisible(true);
        settingsDialog.SetText(
            "Settings\n"
            + "ROS Topic Enable: " + (rosTopicEnable ? "Enabled" : "Disabled") + "\n\n"
            + OptionLine(AdjustPanelOption, "Adjust Floating Stream Panel" + (adjustingStreamPanel ? "  ON" : "")) + "\n"
            + OptionLine(SaveOption, "Save") + "\n"
            + OptionLine(DiscardOption, "Discard") + "\n\n"
            + "Panel x " + F(panelPosition.x)
            + "  y " + F(panelPosition.y)
            + "  z " + F(panelPosition.z));
    }

    private string OptionLine(int option, string label)
    {
        return selectedSettingsOption == option ? "> " + label : "  " + label;
    }

    private void EnsureStreamPanel()
    {
        if (streamPanel != null)
        {
            return;
        }

        streamPanel = FindAnyObjectByType<StreamPanelManager>();
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
