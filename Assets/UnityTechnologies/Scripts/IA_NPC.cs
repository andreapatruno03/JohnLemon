using UnityEngine;

public class IA_NPC : MonoBehaviour
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
    public float viewRadius = 15f; // Raggio di visione
    [Range(0, 360)] public float fieldOfViewAngle = 120f; // Angolo di visione
    public LayerMask targetMask; // Layer del giocatore o degli oggetti da rilevare
    public LayerMask obstacleMask; // Layer degli ostacoli
    public Transform player; // Riferimento al giocatore
    public bool CanSeeTarget { get; private set; } // Controlla se vede il target

    [Header("Layers")]
    public LayerMask obstacleLayer;

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

        FindVisibleTargets();

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

        if (CanSeeTarget)
        {
            TransitionToState(AIState.Chase);
        }
    }

    void HandleChaseState()
    {
        if (player == null)
        {
            TransitionToState(AIState.Alert);
            return;
        }

        if (!CanSeeTarget)
        {
            lastPlayerSpottedTime = Time.time;
            lastKnownPlayerPosition = player.position;
            TransitionToState(AIState.Alert);
            return;
        }

        float adjustedChaseSpeed = normalSpeed * chaseSpeedMultiplier;
        MoveTowardsPlayer(adjustedChaseSpeed);
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

    private void MoveTowardsPlayer(float moveSpeed)
    {
        if (player == null) return;

        Vector3 direction = (player.position - transform.position).normalized;
        movementDirection = direction;

        RotateTowards(direction);

        animator.SetBool("isWalking", true);
        rb.MovePosition(transform.position + movementDirection * moveSpeed * Time.fixedDeltaTime);
    }

    private void FindVisibleTargets()
    {
        Collider[] targetsInViewRadius = Physics.OverlapSphere(transform.position, viewRadius, targetMask);
        CanSeeTarget = false;

        foreach (Collider target in targetsInViewRadius)
        {
            Transform targetTransform = target.transform;
            Vector3 directionToTarget = (new Vector3(targetTransform.position.x, transform.position.y, targetTransform.position.z) - transform.position).normalized;

            if (Vector3.Angle(transform.forward, directionToTarget) < fieldOfViewAngle / 2)
            {
                float distanceToTarget = Vector3.Distance(transform.position, targetTransform.position);

                if (!Physics.Raycast(transform.position, directionToTarget, distanceToTarget, obstacleMask))
                {
                    CanSeeTarget = true;
                    player = targetTransform;
                    break;
                }
            }
        }
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
