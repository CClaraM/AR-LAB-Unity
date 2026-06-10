using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class ProjectileKillZone : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private string projectileTag = "Projectile";

    [Header("Zone Size")]
    [SerializeField] private float extraRadius = 1.5f;
    [SerializeField] private float minimumRadius = 2f;

    private SphereCollider sphereCollider;

    private void Awake()
    {
        sphereCollider = GetComponent<SphereCollider>();
        sphereCollider.isTrigger = true;
    }

    public void ConfigureFromPoints(Vector3 pointA, Vector3 pointB)
    {
        Vector3 center = (pointA + pointB) * 0.5f;
        float distance = Vector3.Distance(pointA, pointB);

        transform.position = center;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        float radius = Mathf.Max(minimumRadius, (distance * 0.5f) + extraRadius);

        sphereCollider.radius = radius;

        Debug.Log($"KillZone configurada. Centro: {center}, Radio: {radius:0.00} m");
    }

    private void OnTriggerExit(Collider other)
    {
        GameObject projectileRoot = FindTaggedParent(other.transform, projectileTag);

        if (projectileRoot == null)
            return;

        ProjectileImpactReporter reporter =
        projectileRoot.GetComponent<ProjectileImpactReporter>();

        if (reporter != null)
        {
            reporter.ReportOutOfBounds();
        }
        else
        {
            Destroy(projectileRoot);

            if (TimeScaleController.Instance != null)
            {
                TimeScaleController.Instance.RestoreNormalTime();
            }
        }

        Debug.Log("Proyectil salió de la KillZone y fue destruido.");
    }

    private GameObject FindTaggedParent(Transform start, string tagToFind)
    {
        Transform current = start;

        while (current != null)
        {
            if (current.CompareTag(tagToFind))
                return current.gameObject;

            current = current.parent;
        }

        return null;
    }
}