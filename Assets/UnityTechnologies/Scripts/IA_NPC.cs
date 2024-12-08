using UnityEngine;

public class AdvancedAIFSM : MonoBehaviour
{
    public enum AIState { Patrol, Chase, Alert, Investigate }

    [Header("State Management")]
    public AIState currentState = AIState.Patrol;
    private AIState previousState;

    [Header("Movement")]
    public Transform[] patrolPoints;
    public float normalSpeed = 2f;
    public float rotationSpeed = 5f;

    [Header("Speed Adjustment")]
    public float chaseSpeedMultiplier = 1.2f;

    [Header("Field of View Reference")]
    public FieldOfView fieldOfView;

    [Header("Layers")]
    public LayerMask obstacleLayer;
    public LayerMask playerLayer;

    // Components
    private Rigidbody rb;
    private Animator animator;

    // State Tracking
    private int currentPatrolIndex = 0;
    private float lastPlayerSpottedTime;
    private Vector3 lastKnownPlayerPosition;
    private Vector3 movementDirection;

    // Flag per l'inseguimento
    private bool isChasing = false;

    void Start()
    {
        InitializeComponents();
    }

    void InitializeComponents()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        if (rb == null)
        {
            Debug.LogError("Rigidbody non trovato!");
        }
        else
        {
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        if (animator == null)
        {
            Debug.LogError("Animator non trovato!");
        }

        if (fieldOfView == null)
        {
            Debug.LogError("FieldOfView non assegnato! Assicurati di assegnarlo nell'Inspector.");
        }

        if (patrolPoints.Length > 0)
        {
            currentPatrolIndex = 0;
        }
    }

    void FixedUpdate()
    {
        if (rb == null || animator == null) return;

        if (movementDirection != Vector3.zero)
        {
            Vector3 newPosition = rb.position + movementDirection * normalSpeed * Time.fixedDeltaTime;
            rb.MovePosition(newPosition);
        }
    }

    void Update()
    {
        if (rb == null || animator == null || patrolPoints.Length == 0) return;

        if (fieldOfView != null && fieldOfView.CanSeeTarget)
        {
            if (fieldOfView.detectedTarget != null && !isChasing)
            {
                isChasing = true;
                lastKnownPlayerPosition = fieldOfView.detectedTarget.position;
                TransitionToState(AIState.Chase);
            }
        }

        if (isChasing && (fieldOfView.detectedTarget == null || !fieldOfView.CanSeeTarget))
        {
            isChasing = false;
            lastPlayerSpottedTime = Time.time;
            TransitionToState(AIState.Alert);
        }

        switch (currentState)
        {
            case AIState.Patrol:
                HandlePatrolState();
                break;
            case AIState.Chase:
                HandleChaseState();
                break;
            case AIState.Alert:
                HandleAlertState();
                break;
            case AIState.Investigate:
                HandleInvestigateState();
                break;
        }
    }

    void HandlePatrolState()
    {
        if (patrolPoints.Length == 0) return;

        Transform target = patrolPoints[currentPatrolIndex];
        MoveTowardsPosition(target.position, normalSpeed);

        if (Vector3.Distance(transform.position, target.position) < 0.5f)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }
    }

    void HandleChaseState()
    {
        animator.SetBool("isChasing", true);

        if (!fieldOfView.CanSeeTarget || fieldOfView.detectedTarget == null)
        {
            animator.SetBool("isChasing", false);
            movementDirection = Vector3.zero;
            TransitionToState(AIState.Alert);
            return;
        }

        MoveTowardsPosition(fieldOfView.detectedTarget.position, normalSpeed * chaseSpeedMultiplier);
    }

    void HandleAlertState()
    {
        movementDirection = Vector3.zero;
        animator.SetBool("isWalking", false);
        animator.SetBool("isChasing", false);

        if (Time.time - lastPlayerSpottedTime > Random.Range(2f, 5f))
        {
            TransitionToState(AIState.Investigate);
        }
    }

    void HandleInvestigateState()
    {
        MoveTowardsPosition(lastKnownPlayerPosition, normalSpeed);

        if (Vector3.Distance(transform.position, lastKnownPlayerPosition) < 0.5f)
        {
            TransitionToState(AIState.Patrol);
        }
    }

    void MoveTowardsPosition(Vector3 position, float moveSpeed)
    {
        movementDirection = (position - transform.position).normalized;

        if (movementDirection.magnitude < 0.1f)
        {
            movementDirection = Vector3.zero;
            animator.SetBool("isWalking", false);
            return;
        }

        RotateTowards(movementDirection);
        animator.SetBool("isWalking", true);
    }

    void RotateTowards(Vector3 targetDirection)
    {
        if (targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    void TransitionToState(AIState newState)
    {
        if (currentState == newState) return;

        movementDirection = Vector3.zero;

        if (newState == AIState.Patrol)
        {
            animator.SetBool("isWalking", true);
            animator.SetBool("isChasing", false);
        }
        else if (newState == AIState.Chase)
        {
            animator.SetBool("isWalking", false);
            animator.SetBool("isChasing", true);
        }
        else
        {
            animator.SetBool("isWalking", false);
            animator.SetBool("isChasing", false);
        }

        previousState = currentState;
        currentState = newState;
    }
}
