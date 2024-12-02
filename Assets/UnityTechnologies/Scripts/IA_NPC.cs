using UnityEngine;

public class IA_NPC : MonoBehaviour, IMovementController
{
    public enum AIState { Patrol, Chase, Alert, Investigate }

    [Header("Movement")]
    public Transform[] patrolPoints;
    public float normalSpeed = 2f;
    public float rotationSpeed = 5f;
    public float chaseSpeedMultiplier = 1.5f;

    [Header("State Management")]
    public AIState currentState = AIState.Patrol;

    // Components
    private Transform player;
    private Rigidbody rb;
    private Animator animator;

    // State Tracking
    private int currentPatrolIndex = 0;
    private Vector3 lastKnownPlayerPosition;
    private bool canSeePlayer = false;

    private float alertTimer = 0f; // Timer per lo stato di alert
    private float alertDuration = 3f; // Durata dello stato di alert

    private void Start()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        if (rb == null)
            Debug.LogError("Rigidbody non trovato!");

        if (animator == null)
            Debug.LogError("Animator non trovato!");

        if (patrolPoints.Length == 0)
            Debug.LogWarning("Nessun waypoint assegnato per la pattuglia!");
    }

    private void Update()
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
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            animator.SetBool("isWalking", false);
            return;
        }

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

        MoveTowardsPosition(lastKnownPlayerPosition, normalSpeed * chaseSpeedMultiplier);

        if (Vector3.Distance(transform.position, lastKnownPlayerPosition) < 0.5f && !canSeePlayer)
        {
            TransitionToState(AIState.Alert);
        }
    }

    private void HandleAlertState()
    {
        alertTimer += Time.deltaTime;

        if (alertTimer >= alertDuration)
        {
            alertTimer = 0f;
            TransitionToState(AIState.Patrol);
        }
    }

    private void HandleInvestigateState()
    {
        MoveTowardsPosition(lastKnownPlayerPosition, normalSpeed);

        if (Vector3.Distance(transform.position, lastKnownPlayerPosition) < 0.5f)
        {
            TransitionToState(AIState.Patrol);
        }
    }

    private void MoveTowardsPosition(Vector3 position, float speed)
    {
        Vector3 direction = (position - transform.position).normalized;

        if (direction.magnitude < 0.1f)
        {
            animator.SetBool("isWalking", false);
            return;
        }

        RotateTowards(direction);

        rb.MovePosition(transform.position + direction * speed * Time.deltaTime);
    }

    private void RotateTowards(Vector3 direction)
    {
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    public void TransitionToState(AIState newState)
    {
        if (currentState == newState) return;

        // Aggiorna lo stato corrente
        currentState = newState;

        // Resetta i parametri dell'Animator
        switch (newState)
        {
            case AIState.Patrol:
                animator.SetBool("isWalking", true);
                animator.SetBool("isChasing", false);
                break;
            case AIState.Chase:
                animator.SetBool("isWalking", true);
                animator.SetBool("isChasing", true);
                break;
            case AIState.Alert:
                animator.SetBool("isWalking", false);
                animator.SetBool("isChasing", false);
                break;
            case AIState.Investigate:
                animator.SetBool("isWalking", true);
                animator.SetBool("isChasing", false);
                break;
        }

        // Resetta il timer dello stato di Alert
        if (newState != AIState.Alert)
        {
            alertTimer = 0f;
        }
    }
}
