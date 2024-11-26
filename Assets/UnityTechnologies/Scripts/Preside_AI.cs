using UnityEngine;

public class PresideAI : MonoBehaviour
{
    public enum AIState { Patrol, Chase, Alert, Investigate }

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

    [Header("References")]
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
    public Vector3 lastKnownPlayerPosition;
    private Vector3 movementDirection;

    void Start()
    {
        InitializeComponents();
    }

    void InitializeComponents()
    {
        if (genSuit == null)
        {
            Debug.LogError("genSuit non è stato assegnato nel componente PresideAI! Assicurati di assegnarlo nell'Inspector.");
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
        Debug.Log("Movimento verso nuova posizione: " + (rb.position + movementDirection * normalSpeed * Time.fixedDeltaTime));
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
    if (patrolPoints.Length == 0)
    {
        Debug.LogWarning("Nessun waypoint assegnato!");
        return;
    }

    animator.SetBool("isWalking", true); // Attiva l'animazione di camminata

    Transform target = patrolPoints[currentPatrolIndex];
    Debug.Log("Movimento verso waypoint: " + target.position);

    MoveTowardsPosition(target.position, normalSpeed);

    if (Vector3.Distance(transform.position, target.position) < 0.5f)
    {
        Debug.Log("Waypoint raggiunto: " + target.position);
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length; // Passa al prossimo waypoint
        Debug.Log("Prossimo waypoint: " + patrolPoints[currentPatrolIndex].position);
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

        animator.SetBool("isWalking", false);
        animator.SetBool("isChasing", true);

        float adjustedChaseSpeed = normalSpeed * chaseSpeedMultiplier;
        MoveTowardsPosition(player.position, adjustedChaseSpeed);
    }

    void HandleAlertState()
    {
        animator.SetBool("isWalking", false);
        animator.SetBool("isChasing", false);

        if (Time.time - lastPlayerSpottedTime > Random.Range(2f, 5f))
        {
            FindClosestPatrolPoint();
            TransitionToState(AIState.Patrol);
        }
    }

    void HandleInvestigateState()
    {
        animator.SetBool("isWalking", true);
        animator.SetBool("isChasing", false);

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

    Debug.Log("Target Posizione: " + position);
    Debug.Log("NPC Posizione Corrente: " + transform.position);
    Debug.Log("Direzione Calcolata: " + movementDirection);

    if (Vector3.Distance(transform.position, position) < 0.1f)
    {
        movementDirection = Vector3.zero;
        animator.SetBool("isWalking", false);
        return;
    }

    RotateTowards(movementDirection);

    Vector3 newPosition = transform.position + movementDirection * moveSpeed * Time.deltaTime;
    Debug.Log("Nuova Posizione Calcolata: " + newPosition);

    rb.MovePosition(newPosition);
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

    void ListenForNpcAlert()
    {
        Collider[] alertedNpcs = Physics.OverlapSphere(transform.position, npcAlertRadius, npcLayer);

        foreach (var npc in alertedNpcs)
        {
            NPC_Alert alert = npc.GetComponent<NPC_Alert>();
            if (alert != null && alert.playerDetected)
            {
                Debug.Log("Preside avvisato da NPC: " + npc.name);
                lastKnownPlayerPosition = alert.playerPosition;
                TransitionToState(AIState.Chase);
                break;
            }
        }
    }

    public void AlertPreside(Vector3 playerPosition)
    {
        lastKnownPlayerPosition = playerPosition;
        TransitionToState(AIState.Chase);
        Debug.Log("Preside avvisato della posizione del giocatore: " + playerPosition);
    }

    void RotateTowards(Vector3 targetDirection)
    {
        if (targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    public void TransitionToState(AIState newState)
    {
        if (currentState == newState) return;

        previousState = currentState;
        currentState = newState;
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
}




