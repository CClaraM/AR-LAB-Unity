using UnityEngine;

public class ProjectileLifetime : MonoBehaviour
{
    [Header("Lifetime")]
    [SerializeField] private float maxLifetimeRealtime = 8f;

    [Header("Fall Limit")]
    [SerializeField] private bool destroyBelowY = true;
    [SerializeField] private float minY = -5f;

    private float spawnRealtime;

    private void Awake()
    {
        spawnRealtime = Time.realtimeSinceStartup;
    }

    private void Update()
    {
        if (Time.realtimeSinceStartup - spawnRealtime >= maxLifetimeRealtime)
        {
            Destroy(gameObject);
            return;
        }

        if (destroyBelowY && transform.position.y < minY)
        {
            Destroy(gameObject);
        }
    }
}