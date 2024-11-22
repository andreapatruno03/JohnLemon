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

    [Header("Detection")]
    public Transform player;
    public float detectionRange = 5f;
    public float alertRange = 7f;
    [Range(0, 360)] public float fieldOfViewAngle = 90f;

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

        if (player == null)
        {
            Debug.LogWarning("Il riferimento al giocatore non è stato impostato!");
        }

        if (patrolPoints.Length > 0)
        {
            currentPatrolIndex = 0;
        }
    }

    void FixedUpdate()
    {
        if (rb == null || animator == null) return;

        // Esegui il movimento in FixedUpdate
        if (movementDirection != Vector3.zero)
        {
            Vector3 newPosition = rb.position + movementDirection * normalSpeed * Time.fixedDeltaTime;
            rb.MovePosition(newPosition);
        }
    }

    void Update()
    {
        if (rb == null || animator == null || patrolPoints.Length == 0) return;

        DetectPlayerInRange();

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
        if (player == null)
        {
            TransitionToState(AIState.Alert);
            return;
        }

        if (!IsPlayerVisible())
        {
            lastPlayerSpottedTime = Time.time;
            lastKnownPlayerPosition = player.position;
            TransitionToState(AIState.Alert);
            return;
        }

        float adjustedChaseSpeed = normalSpeed * chaseSpeedMultiplier;
        MoveTowardsPosition(player.position, adjustedChaseSpeed);
    }

    void HandleAlertState()
    {
        animator.SetFloat("speed", 0f);

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
        animator.SetBool("isWalking", false); // Ferma l'animazione di camminata
        return;
    }

    RotateTowards(movementDirection);

    // Attiva l'animazione di camminata
    animator.SetBool("isWalking", true);

    // Muovi il personaggio
    rb.MovePosition(transform.position + movementDirection * moveSpeed * Time.fixedDeltaTime);
}


    bool CheckForObstacle()
    {
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 1f, obstacleLayer))
        {
            Debug.DrawRay(transform.position, transform.forward * 1f, Color.red);
            return true;
        }

        Debug.DrawRay(transform.position, transform.forward * 1f, Color.green);
        return false;
    }

    void DetectPlayerInRange()
    {
        if (player != null && IsPlayerVisible())
        {
            TransitionToState(AIState.Chase);
        }
    }

    bool IsPlayerVisible()
    {
        if (player == null) return false;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > detectionRange) return false;

        Vector3 directionToTarget = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToTarget);

        if (angle < fieldOfViewAngle * 0.5f)
        {
            if (!Physics.Raycast(transform.position, directionToTarget, out RaycastHit hit, detectionRange, obstacleLayer))
            {
                return true;
            }
        }

        return false;
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

        previousState = currentState;
        currentState = newState;
    }
}
