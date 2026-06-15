using System;

[Serializable]
public class LabBridgeInput
{
    public int schemaVersion = 1;

    public string requestId;
    public string runId;

    public LabSceneData scene;
    public LabParticipantData participant;
    public LabContextData context;
    public LabExerciseConfig exercise;
    public LabOptionsData options;
}

[Serializable]
public class LabSceneData
{
    public string labKey;
    public string unitySceneName;
    public string displayName;
}

[Serializable]
public class LabParticipantData
{
    public string participantId;
    public string displayName;
}

[Serializable]
public class LabContextData
{
    public string organizationName;
    public string courseName;
    public string groupName;
}

[Serializable]
public class LabExerciseConfig
{
    public string exerciseId;
    public int maxAttempts;
    public bool allowResume = true;
}

[Serializable]
public class LabOptionsData
{
    public string language = "es";
    public bool showProjectileCameraOption = true;
    public bool showTrajectoryPreview = true;
}