using UnityEngine;

public class PresideAI : MonoBehaviour, IMovementController
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
    public float detectionRange = 5f;

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
    private Transform player; // Riferimento al giocatore
    private Vector3 lastKnownPlayerPosition;
    private Vector3 movementDirection;
    private bool canSeePlayer; // Indica se il giocatore è visibile
    private int currentPatrolIndex = 0;

    private bool isChasing = false;

    // Alert Timer
    private float alertTimer = 0f;
    private float alertDuration = 3f; // Durata dello stato Alert

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

        if (rb == null) Debug.LogError($"Rigidbody non trovato nel GameObject '{genSuit.name}'!");
        if (animator == null) Debug.LogError($"Animator non trovato nel GameObject '{genSuit.name}'!");

        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            Debug.LogWarning("Nessun waypoint di pattuglia assegnato!");
        }
    }

    void Update()
    {
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

    public void AlertPreside(Vector3 playerPosition)
    {
        lastKnownPlayerPosition = playerPosition;

        // Cambia lo stato del Preside in Chase se non è già in inseguimento
        if (currentState != AIState.Chase)
        {
            TransitionToState(AIState.Chase);
            Debug.Log($"Preside avvisato della posizione del giocatore: {playerPosition}");
        }
    }

    public void UpdateVisionState(bool canSeeTarget, Transform target)
    {
        canSeePlayer = canSeeTarget;

        if (canSeeTarget)
        {
            player = target;
            lastKnownPlayerPosition = target.position;
            TransitionToState(AIState.Chase);
        }
        else if (currentState == AIState.Chase)
        {
            TransitionToState(AIState.Alert);
        }
    }

    private void HandlePatrolState()
    {
        isChasing = false;

        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            animator.SetBool("isWalking", false);
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

    private void HandleChaseState()
    {
        if (player == null)
        {
            TransitionToState(AIState.Alert);
            return;
        }

        animator.SetBool("isWalking", true);
        float adjustedChaseSpeed = normalSpeed * chaseSpeedMultiplier;

        MoveTowardsPosition(lastKnownPlayerPosition, adjustedChaseSpeed);

        if (Vector3.Distance(transform.position, lastKnownPlayerPosition) < 0.5f && !canSeePlayer)
        {
            TransitionToState(AIState.Alert);
        }
    }

    private void HandleAlertState()
    {
        isChasing = false;

        animator.SetBool("isWalking", false);

        // Incrementa il timer
        alertTimer += Time.deltaTime;

        // Dopo un certo periodo, torna in stato di Patrol
        if (alertTimer >= alertDuration)
        {
            alertTimer = 0f; // Resetta il timer
            TransitionToState(AIState.Patrol);
        }
    }

    private void HandleInvestigateState()
    {
        isChasing = false;
        animator.SetBool("isWalking", true);

        MoveTowardsPosition(lastKnownPlayerPosition, normalSpeed);

        if (Vector3.Distance(transform.position, lastKnownPlayerPosition) < 0.5f)
        {
            TransitionToState(AIState.Patrol);
        }
    }

    private void MoveTowardsPosition(Vector3 position, float moveSpeed)
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

    private void RotateTowards(Vector3 targetDirection)
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

        isChasing = (newState == AIState.Chase);

        // Resetta il timer quando si cambia stato
        if (newState != AIState.Alert)
        {
            alertTimer = 0f;
        }
    }
}
