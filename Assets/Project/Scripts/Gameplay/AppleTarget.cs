using UnityEngine;

public class AppleTarget : MonoBehaviour
{
    [Header("Impact Detection")]
    [SerializeField] private string projectileTag = "Projectile";

    [Header("Audio")]
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private float soundVolume = 1f;

    [Header("Behaviour")]
    [SerializeField] private bool destroyTargetOnHit = true;
    [SerializeField] private float destroyDelay = 0.05f;

    private bool wasHit;

    private void OnTriggerEnter(Collider other)
    {
        if (wasHit)
            return;

        GameObject projectileRoot = FindTaggedParent(other.transform, projectileTag);

        if (projectileRoot == null)
            return;

        HandleHit(projectileRoot);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (wasHit)
            return;

        GameObject projectileRoot = FindTaggedParent(collision.transform, projectileTag);

        if (projectileRoot == null)
            return;

        HandleHit(projectileRoot);
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

    private void HandleHit(GameObject projectile)
    {
        wasHit = true;

        transform.localScale *= 1.5f;

        if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, transform.position, soundVolume);
        }

        if (destroyTargetOnHit)
        {
            Destroy(gameObject, destroyDelay);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}