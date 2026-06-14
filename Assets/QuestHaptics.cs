using UnityEngine;
using UnityEngine.XR;

public class QuestHaptics : MonoBehaviour
{
    public float defaultAmplitude = 0.70f;
    public float defaultDuration = 0.035f;

    public void Pulse(InputDevice device)
    {
        Pulse(device, defaultAmplitude, defaultDuration);
    }

    public void Pulse(InputDevice device, float amplitude, float duration)
    {
        if (!device.isValid)
        {
            return;
        }

        device.SendHapticImpulse(0u, amplitude, duration);
    }
}
