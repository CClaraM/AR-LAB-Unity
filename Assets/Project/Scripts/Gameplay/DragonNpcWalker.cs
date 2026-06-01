using UnityEngine;

public class DragonNpcWalker : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float moveSpeed = 0.15f;
    [SerializeField] private float rotationSpeed = 6f;
    [SerializeField] private float stopDistance = 0.03f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string walkBoolName = "IsWalking";

    private int currentWaypointIndex;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void FixedUpdate()
    {
        if (waypoints == null || waypoints.Length == 0)
            return;

        Transform target = waypoints[currentWaypointIndex];
        Vector3 currentPosition = rb.position;
        Vector3 targetPosition = target.position;

        Vector3 direction = targetPosition - currentPosition;
        direction.y = 0f;

        if (direction.magnitude <= stopDistance)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            SetWalking(false);
            return;
        }

        Vector3 nextPosition = Vector3.MoveTowards(
            currentPosition,
            targetPosition,
            moveSpeed * Time.fixedDeltaTime
        );

        rb.MovePosition(nextPosition);

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        Quaternion nextRotation = Quaternion.Slerp(
            rb.rotation,
            targetRotation,
            rotationSpeed * Time.fixedDeltaTime
        );

        rb.MoveRotation(nextRotation);

        SetWalking(true);
    }

    private void SetWalking(bool walking)
    {
        if (animator != null && !string.IsNullOrWhiteSpace(walkBoolName))
            animator.SetBool(walkBoolName, walking);
    }
}