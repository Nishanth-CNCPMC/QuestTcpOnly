using UnityEngine;

public class QuestOriginManager : MonoBehaviour
{
    private Vector3 originPosition;
    private Quaternion originRotation = Quaternion.identity;

    public bool OriginSet { get; private set; }

    public void SetOrigin(Vector3 position, Quaternion rotation)
    {
        originPosition = position;
        originRotation = rotation;
        OriginSet = true;
    }

    public void ClearOrigin()
    {
        originPosition = Vector3.zero;
        originRotation = Quaternion.identity;
        OriginSet = false;
    }

    public bool TryGetRelativePose(
        Vector3 currentPosition,
        Quaternion currentRotation,
        out Vector3 relativePosition,
        out Quaternion relativeRotation)
    {
        if (!OriginSet)
        {
            relativePosition = Vector3.zero;
            relativeRotation = Quaternion.identity;
            return false;
        }

        relativePosition = currentPosition - originPosition;
        relativeRotation = Quaternion.Inverse(originRotation) * currentRotation;
        return true;
    }
}
