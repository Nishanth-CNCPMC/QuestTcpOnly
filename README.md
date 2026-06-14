# Quest TCP Only

Version `1.2` is the Quest-side Unity app for the Quest2Skill VR-to-Isaac workflow.

Unity project for streaming Meta Quest 3 headset and controller data over TCP.

## Why This Exists

This app turns the Quest into the live VR front end for Isaac Sim. It sends headset and controller tracking data to the ROS bridge, receives haptic click commands back over the same TCP connection, and displays the browser/Isaac stream inside a floating VR panel.

Use this package when you want to:

- Drive Isaac headset/controller proxy prims from a Quest headset.
- Send Quest controller buttons, triggers, grips, and joysticks into ROS 2.
- See the Isaac/browser feed in VR through a WebRTC WHEP stream.
- Receive hover/grab haptic feedback from Isaac through ROS and TCP.

## Release 1.2

- Quest haptic click amplitude is set to `0.70`.
- Haptic click duration remains short at `35 ms`.
- The app receives TCP haptic commands from the ROS bridge.
- The floating WebRTC stream panel and settings flow are included.

The app reads:

- Head pose and detection
- Left and right controller poses
- Trigger and grip analog values
- Thumbstick axes, click, and touch
- Left X/Y/menu buttons
- Right A/B buttons
- Trigger and grip buttons

The main scripts live in `Assets/`:

- `QuestInputReader.cs`
- `QuestPosePublisher.cs`
- `QuestTcpClient.cs`
- `QuestHud.cs`
- `QuestHaptics.cs`
- `QuestHapticCommandReceiver.cs`
- `WebRTCStreamReceiver.cs`
- `StreamPanelManager.cs`

## How To Use

1. Open this folder as a Unity project.
2. Set `QuestTcpClient.host` in the Unity Inspector to the PC LAN IP address running the ROS bridge.
3. Build and sideload the app to the Quest.
4. Start the ROS bridge on the PC.
5. Start the Isaac extension and connect ROS from the extension UI.

Before sideloading to Quest, set `QuestTcpClient.host` in the Unity Inspector to the PC LAN IP address running the ROS bridge. Do not leave it as `127.0.0.1` for device builds.

The TCP bridge currently expects newline-delimited JSON on port `5005`.

## Runtime Controls

- Left controller menu button opens the settings dialog.
- Settings can pause ROS topic updates while adjusting the stream panel.
- The stream panel position can be adjusted and saved from VR.

## TCP Contract

The app sends newline-delimited JSON to the ROS bridge on port `5005`.

It also receives haptic commands in this shape:

```json
{"type":"haptic","side":"right","duration_ms":35,"amplitude":0.70}
```

`side` can be `left` or `right`.
