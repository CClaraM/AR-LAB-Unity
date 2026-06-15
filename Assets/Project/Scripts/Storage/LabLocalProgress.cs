using System;

[Serializable]
public class LabLocalProgress
{
    public int schemaVersion = 1;

    public string requestId;
    public string runId;

    public string labKey;
    public string exerciseId;

    public string participantId;
    public string participantName;

    public int maxAttempts;
    public int usedAttempts;
    public int remainingAttempts;

    public bool completed;
    public bool hitTarget;

    public LabAttemptResult[] attempts;

    public string startedAt;
    public string updatedAt;
}