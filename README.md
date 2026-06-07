# Quest TCP Only

Unity project for streaming Meta Quest 3 headset and controller data over TCP.

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

Before sideloading to Quest, set `QuestTcpClient.host` in the Unity Inspector to the PC LAN IP address running the ROS bridge. Do not leave it as `127.0.0.1` for device builds.

The TCP bridge currently expects newline-delimited JSON on port `5005`.
