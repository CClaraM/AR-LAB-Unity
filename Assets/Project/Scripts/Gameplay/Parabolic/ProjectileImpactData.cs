using UnityEngine;

public enum ProjectileImpactType
{
    HitTarget,
    MissedTarget,
    OutOfBounds
}

public class ProjectileImpactData
{
    public ProjectileImpactType impactType;

    public Vector3 impactPoint;
    public Vector3 targetPoint;

    public float impactDistanceToTarget;
    public float impactHorizontalDistance;
    public float impactHeightDifference;

    public float power;
    public float angle;
}