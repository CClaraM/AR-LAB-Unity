using System;

[Serializable]
public class ExerciseData
{
    public string exerciseId;
    public string exerciseName;
    public string apprenticeId;
    public string exerciseMode;

    public float initialAngle;
    public float initialPower;

    public bool showTrajectory;
    public int maxAttempts;

    public string learnerId;
}
