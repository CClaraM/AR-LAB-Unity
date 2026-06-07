[System.Serializable]
public class LabFinalResult
{
    public string exerciseId;
    public string apprenticeId;

    public float horizontalDistance;
    public float verticalDistance;
    public float straightDistance;

    public bool hitTarget;
    public int maxAttempts;
    public int usedAttempts;
    public bool completed;

    public LabAttemptResult[] attempts;
}