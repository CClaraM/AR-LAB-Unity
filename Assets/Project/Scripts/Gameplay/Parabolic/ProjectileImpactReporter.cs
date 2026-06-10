using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ProjectileImpactReporter : MonoBehaviour
{
    [Header("Safety")]
    [SerializeField] private float armingDelay = 0.08f;

    [Header("After Impact")]
    [SerializeField] private bool stopPhysicsAfterImpact = true;
    [SerializeField] private bool disableCollidersAfterImpact = true;
    [SerializeField] private float destroyDelayAfterImpact = 1.2f;
    [SerializeField] private bool destroyAfterReport = true;

    private ARPhysicsLabController labController;
    private AppleTarget appleTarget;
    private CannonLauncher launcher;
    private CannonAimController aimController;

    private Rigidbody projectileRigidbody;
    private Collider[] projectileColliders;

    private bool hasReported;
    private float spawnRealtime;

    public void Initialize(
        ARPhysicsLabController lab,
        AppleTarget target,
        CannonLauncher cannonLauncher,
        CannonAimController cannonAimController
    )
    {
        labController = lab;
        appleTarget = target;
        launcher = cannonLauncher;
        aimController = cannonAimController;
        spawnRealtime = Time.realtimeSinceStartup;
        hasReported = false;

        Debug.Log("ProjectileImpactReporter inicializado.");
    }

    private void Awake()
    {
        spawnRealtime = Time.realtimeSinceStartup;

        projectileRigidbody = GetComponent<Rigidbody>();
        projectileColliders = GetComponentsInChildren<Collider>(true);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!CanReport())
            return;

        if (collision.collider == null)
            return;

        if (collision.collider.isTrigger)
            return;

        Vector3 impactPoint = GetCollisionImpactPoint(collision);

        AppleTarget touchedTarget =
            collision.collider.GetComponentInParent<AppleTarget>();

        if (touchedTarget != null)
        {
            Debug.Log("ProjectileImpactReporter: impacto físico contra AppleTarget.");
            ReportImpact(ProjectileImpactType.HitTarget, impactPoint);
            return;
        }

        Debug.Log("ProjectileImpactReporter: impacto físico, pero no es AppleTarget.");
        ReportImpact(ProjectileImpactType.MissedTarget, impactPoint);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!CanReport())
            return;

        if (other == null)
            return;

        AppleTarget touchedTarget =
            other.GetComponentInParent<AppleTarget>();

        if (touchedTarget == null)
            return;

        Vector3 impactPoint = other.ClosestPoint(transform.position);

        Debug.Log("ProjectileImpactReporter: entró en trigger de AppleTarget.");
        ReportImpact(ProjectileImpactType.HitTarget, impactPoint);
    }

    public void ReportOutOfBounds()
    {
        if (hasReported)
            return;

        Vector3 fallbackPoint = transform.position;

        Debug.Log("ProjectileImpactReporter: fuera del área.");
        ReportImpact(ProjectileImpactType.OutOfBounds, fallbackPoint);
    }

    private bool CanReport()
    {
        if (hasReported)
            return false;

        if (Time.realtimeSinceStartup - spawnRealtime < armingDelay)
            return false;

        return true;
    }

    private Vector3 GetCollisionImpactPoint(Collision collision)
    {
        if (collision.contactCount > 0)
            return collision.GetContact(0).point;

        return transform.position;
    }

    private void ReportImpact(ProjectileImpactType impactType, Vector3 impactPoint)
    {
        if (hasReported)
            return;

        hasReported = true;

        FreezeProjectileAfterImpact();

        if (TimeScaleController.Instance != null)
            TimeScaleController.Instance.RestoreNormalTime();

        ProjectileImpactData data = BuildImpactData(impactType, impactPoint);

        if (labController != null)
        {
            labController.NotifyProjectileImpact(data);
        }
        else
        {
            Debug.LogWarning("ProjectileImpactReporter: labController no está asignado.");
        }

        if (destroyAfterReport)
        {
            Destroy(gameObject, destroyDelayAfterImpact);
        }
    }

    private void FreezeProjectileAfterImpact()
    {
        if (stopPhysicsAfterImpact && projectileRigidbody != null)
        {
#if UNITY_6000_0_OR_NEWER
            projectileRigidbody.linearVelocity = Vector3.zero;
#else
        projectileRigidbody.velocity = Vector3.zero;
#endif

            projectileRigidbody.angularVelocity = Vector3.zero;
            projectileRigidbody.useGravity = false;
            projectileRigidbody.isKinematic = true;
        }

        if (disableCollidersAfterImpact && projectileColliders != null)
        {
            foreach (Collider col in projectileColliders)
            {
                if (col != null)
                    col.enabled = false;
            }
        }
    }

    private ProjectileImpactData BuildImpactData(
        ProjectileImpactType impactType,
        Vector3 impactPoint
    )
    {
        Vector3 targetPoint = appleTarget != null
            ? appleTarget.transform.position
            : impactPoint;

        Vector3 flatImpact = new Vector3(impactPoint.x, 0f, impactPoint.z);
        Vector3 flatTarget = new Vector3(targetPoint.x, 0f, targetPoint.z);

        float power = launcher != null ? launcher.CurrentLaunchPower : 0f;
        float angle = aimController != null ? aimController.CurrentPitch : 0f;

        return new ProjectileImpactData
        {
            impactType = impactType,
            impactPoint = impactPoint,
            targetPoint = targetPoint,

            impactDistanceToTarget = Vector3.Distance(impactPoint, targetPoint),
            impactHorizontalDistance = Vector3.Distance(flatImpact, flatTarget),
            impactHeightDifference = targetPoint.y - impactPoint.y,

            power = power,
            angle = angle
        };
    }
}