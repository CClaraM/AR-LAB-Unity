using System;
using UnityEngine;

[Serializable]
public class LabAttemptResult
{
    public int attempt;
    public bool hit;

    public float power;
    public float angle;

    public float impactDistanceToTarget;
    public float impactHorizontalDistance;
    public float impactHeightDifference;

    public string impactType;

    public Vector3Serializable impactPoint;
    public Vector3Serializable targetPoint;
}

[Serializable]
public class Vector3Serializable
{
    public float x;
    public float y;
    public float z;

    public Vector3Serializable(Vector3 value)
    {
        x = value.x;
        y = value.y;
        z = value.z;
    }
}