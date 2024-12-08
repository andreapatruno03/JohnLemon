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
    public FieldOfView fieldOfView; // Riferimento allo script FieldOfView

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

        // Verifica se l'NPC può inseguire il giocatore
        if (fieldOfView != null && fieldOfView.CanSeeTarget)
        {
            if (fieldOfView.detectedTarget != null && !isChasing)
            {
                isChasing = true; // Assegna questo NPC all'inseguimento
                lastKnownPlayerPosition = fieldOfView.detectedTarget.position;
                TransitionToState(AIState.Chase);
            }
        }

        // Se non vede più il giocatore, resetta lo stato
        if (isChasing && (fieldOfView.detectedTarget == null || !fieldOfView.CanSeeTarget))
        {
            isChasing = false; // Rimuove l'NPC dall'inseguimento
            lastPlayerSpottedTime = Time.time;
            TransitionToState(AIState.Alert);
        }

        // Gestisci lo stato corrente
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
        // Imposta direttamente l'animazione della corsa
        animator.SetBool("isChasing", true);
        Debug.Log("Animazione corsa attivata.");

        if (!fieldOfView.CanSeeTarget || fieldOfView.detectedTarget == null)
        {
            // Se il giocatore non è visibile, torna in Alert
            TransitionToState(AIState.Alert);
            return;
        }

        // Muovi verso il giocatore
        MoveTowardsPosition(fieldOfView.detectedTarget.position, normalSpeed * chaseSpeedMultiplier);

        
    }

    void HandleAlertState()
    {
        animator.SetBool("isWalking", false);

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

        // Gestisci le animazioni durante la transizione di stato
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
            // Reset delle animazioni per stati Alert o Investigate
            animator.SetBool("isWalking", false);
            animator.SetBool("isChasing", false);
        }

        previousState = currentState;
        currentState = newState;
    }
}
