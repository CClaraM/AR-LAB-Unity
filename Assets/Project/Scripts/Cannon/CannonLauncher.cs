using UnityEngine;

public class CannonLauncher : MonoBehaviour
{
    [Header("Launch Setup")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject projectilePrefab;

    [Header("Launch Power")]
    [SerializeField] private float launchPower = 2.5f;

    [Header("Optional Visual")]
    [SerializeField] private GameObject loadedProjectileVisual;

    [Header("Slow Motion")]
    [SerializeField] private bool useSlowMotionOnFire = true;
    [SerializeField] private float slowMotionScale = 0.35f;
    [SerializeField] private float slowMotionDuration = 2.5f;

    public float LaunchPower
    {
        get => launchPower;
        set => launchPower = Mathf.Max(0f, value);
    }

    public Transform FirePoint => firePoint;
    public float CurrentLaunchPower => launchPower;

    public bool Fire()
    {
        if (firePoint == null)
        {
            Debug.LogError("FirePoint no está asignado.");
            return false;
        }

        if (projectilePrefab == null)
        {
            Debug.LogError("Projectile Prefab no está asignado.");
            return false;
        }

        GameObject projectile = Instantiate(
            projectilePrefab,
            firePoint.position,
            firePoint.rotation
        );

        Rigidbody rb = projectile.GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError("El projectilePrefab no tiene Rigidbody.");
            return false;
        }

        rb.isKinematic = false;
        rb.useGravity = true;

        Vector3 launchVelocity = firePoint.forward * launchPower;

        rb.angularVelocity = Vector3.zero;

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = launchVelocity;
#else
        rb.velocity = launchVelocity;
#endif

        if (loadedProjectileVisual != null)
            loadedProjectileVisual.SetActive(false);

        if (useSlowMotionOnFire && TimeScaleController.Instance != null)
        {
            TimeScaleController.Instance.StartSlowMotion(
                slowMotionScale,
                slowMotionDuration
            );
        }

        return true;
    }

    public void SetLaunchPower(float value)
    {
        LaunchPower = value;
    }
}