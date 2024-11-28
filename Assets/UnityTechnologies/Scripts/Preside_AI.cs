using UnityEngine;

public class PresideAI : MonoBehaviour
{
    public enum AIState { Patrol, Chase, Alert, Investigate }

    [Header("Pathfinding")]
    public Pathfinding pathfinding; // Riferimento al sistema di Pathfinding

    [Header("State Management")]
    public AIState currentState = AIState.Patrol;
    private AIState previousState;

    [Header("Movement")]
    public Transform[] patrolPoints;
    public float normalSpeed = 2f;
    public float rotationSpeed = 5f;

    [Header("Chase Settings")]
    public float chaseSpeedMultiplier = 1.5f;

    [Header("Detection")]
    public Transform player;
    public float detectionRange = 5f;
    public float alertRange = 10f;
    [Range(0, 360)] public float fieldOfViewAngle = 90f;

    [Header("Communication with Other NPCs")]
    public LayerMask npcLayer;
    public float npcAlertRadius = 10f;

    [Header("Prefab")]
    public Transform genSuit; // Riferimento diretto al GameObject genSuit con Rigidbody e Animator

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

    private bool isChasing = false; // Stato di inseguimento

    void Start()
    {
        InitializeComponents();
    }

    void InitializeComponents()
    {
        if (genSuit == null)
        {
            Debug.LogError("Il prefab non è stato assegnato nel componente PresideAI! Assicurati di assegnarlo nell'Inspector.");
            return;
        }

        rb = genSuit.GetComponent<Rigidbody>();
        animator = genSuit.GetComponent<Animator>();

        if (rb == null)
        {
            Debug.LogError($"Rigidbody non trovato nel GameObject '{genSuit.name}'! Verifica che sia presente.");
        }

        if (animator == null)
        {
            Debug.LogError($"Animator non trovato nel GameObject '{genSuit.name}'! Verifica che sia presente.");
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

        if (movementDirection != Vector3.zero)
        {
            rb.MovePosition(rb.position + movementDirection * normalSpeed * Time.fixedDeltaTime);
        }
    }

    void Update()
    {
        if (rb == null || animator == null || patrolPoints.Length == 0) return;

        DetectPlayerInRange();
        ListenForNpcAlert();

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
        isChasing = false;

        if (patrolPoints.Length == 0)
        {
            Debug.LogWarning("Nessun waypoint assegnato!");
            return;
        }

        animator.SetBool("isWalking", true);

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

        if (IsPlayerVisible())
        {
            lastKnownPlayerPosition = player.position;
        }

        animator.SetBool("isWalking", true);
        float adjustedChaseSpeed = normalSpeed * chaseSpeedMultiplier;
        MoveTowardsPosition(lastKnownPlayerPosition, adjustedChaseSpeed);

        if (Vector3.Distance(transform.position, lastKnownPlayerPosition) < 0.5f && !IsPlayerVisible())
        {
            TransitionToState(AIState.Alert);
        }
    }

    void HandleAlertState()
    {
        isChasing = false;

        animator.SetBool("isWalking", false);

        if (Time.time - lastPlayerSpottedTime > Random.Range(2f, 5f))
        {
            FindClosestPatrolPoint();
            TransitionToState(AIState.Patrol);
        }
    }

    void HandleInvestigateState()
    {
        isChasing = false;

        animator.SetBool("isWalking", true);

        MoveTowardsPosition(lastKnownPlayerPosition, normalSpeed);

        if (Vector3.Distance(transform.position, lastKnownPlayerPosition) < 0.5f)
        {
            FindClosestPatrolPoint();
            TransitionToState(AIState.Patrol);
        }
    }

    void MoveTowardsPosition(Vector3 position, float moveSpeed)
    {
        movementDirection = (position - transform.position).normalized;

        if (Vector3.Distance(transform.position, position) < 0.1f)
        {
            movementDirection = Vector3.zero;
            animator.SetBool("isWalking", false);
            return;
        }

        RotateTowards(movementDirection);
        rb.MovePosition(transform.position + movementDirection * moveSpeed * Time.deltaTime);
    }

    void RotateTowards(Vector3 targetDirection)
    {
        if (targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
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

        if (Physics.Raycast(transform.position, directionToTarget, out RaycastHit hit, detectionRange))
        {
            return hit.transform == player;
        }

        return false;
    }

    void ListenForNpcAlert()
    {
        Collider[] alertedNpcs = Physics.OverlapSphere(transform.position, npcAlertRadius, npcLayer);

        foreach (var npc in alertedNpcs)
        {
            NPC_Alert alert = npc.GetComponent<NPC_Alert>();
            if (alert != null && alert.playerDetected)
            {
                lastKnownPlayerPosition = alert.playerPosition;
                TransitionToState(AIState.Chase);
                break;
            }
        }
    }

    public void TransitionToState(AIState newState)
    {
        if (currentState == newState) return;

        previousState = currentState;
        currentState = newState;

        isChasing = (newState == AIState.Chase);
    }

    void FindClosestPatrolPoint()
    {
        float closestDistance = float.MaxValue;
        int closestIndex = currentPatrolIndex;

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            float distance = Vector3.Distance(transform.position, patrolPoints[i].position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        currentPatrolIndex = closestIndex;
    }

    public void AlertPreside(Vector3 playerPosition)
{
    lastKnownPlayerPosition = playerPosition;

    // Cambia lo stato del Preside in Chase se non è già in inseguimento
    if (currentState != AIState.Chase)
    {
        TransitionToState(AIState.Chase);
        Debug.Log($"Preside avvisato della posizione del giocatore: {playerPosition}");
    }
    else
    {
        Debug.Log("Preside è già in modalità inseguimento.");
    }
}

}
