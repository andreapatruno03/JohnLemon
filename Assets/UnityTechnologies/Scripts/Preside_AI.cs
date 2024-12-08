using UnityEngine;

public class PresideAI : MonoBehaviour
{
    public enum AIState { Patrol, Chase, Alert, Investigate }

    [Header("Waypoints")]
    public Transform[] waypoints;

    [Header("Patrol Points")]
    public Transform[] patrolPoints;

    [Header("State Management")]
    public AIState currentState = AIState.Patrol;

    [Header("Movement")]
    public float normalSpeed = 2f;
    public float rotationSpeed = 5f;

    [Header("Field of View Reference")]
    public FieldOfView fieldOfView;

    [Header("Communication with Other NPCs")]
    public LayerMask npcLayer;
    public float npcAlertRadius = 10f;

    [Header("References")]
    public Transform genSuit;

    private Rigidbody rb;
    private Animator animator;

    private int currentPatrolIndex = 0;
    private Vector3 lastKnownPlayerPosition;
    private Vector3 movementDirection;

    public float teleportDistanceThreshold = 20f;

    void Start()
    {
        InitializeComponents();
    }

    void InitializeComponents()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        if (rb == null) Debug.LogError("Rigidbody non trovato! Aggiungi un Rigidbody al Preside.");
        if (animator == null) Debug.LogError("Animator non trovato! Aggiungi un Animator al Preside.");
        if (genSuit == null) Debug.LogError("genSuit non assegnato! Assicurati di assegnarlo nell'Inspector.");
        if (fieldOfView == null) Debug.LogError("FieldOfView non assegnato! Assicurati di assegnarlo nell'Inspector.");
        if (patrolPoints.Length == 0) Debug.LogWarning("Nessun Patrol Point assegnato! Aggiungili nell'Inspector.");
    }

    void Update()
    {
        if (rb == null || animator == null || patrolPoints.Length == 0) return;

        // Teletrasporto quando il giocatore è troppo lontano
        if (fieldOfView.detectedTarget != null && Vector3.Distance(transform.position, fieldOfView.detectedTarget.position) > teleportDistanceThreshold)
        {
            TeleportToNearestWaypoint();
        }

        // Controlla il campo visivo per cambiare stato
        if (fieldOfView != null && fieldOfView.CanSeeTarget)
        {
            if (fieldOfView.detectedTarget != null)
            {
                lastKnownPlayerPosition = fieldOfView.detectedTarget.position;
                TransitionToState(AIState.Chase);
            }
        }
        else if (currentState == AIState.Chase)
        {
            // Torna allo stato Patrol se il giocatore non è più visibile
            TransitionToState(AIState.Patrol);
        }

        // Gestisce il comportamento in base allo stato corrente
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
        animator.SetBool("isChasing", false);
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
            Debug.Log("Passa al prossimo patrol point: " + patrolPoints[currentPatrolIndex].name);
        }
    }

    void HandleChaseState()
    {
        if (!fieldOfView.CanSeeTarget || fieldOfView.detectedTarget == null)
        {
            TransitionToState(AIState.Patrol);
            return;
        }

        animator.SetBool("isChasing", true);
        MoveTowardsPosition(fieldOfView.detectedTarget.position, normalSpeed * 1.5f);
    }

    void HandleAlertState()
    {
        animator.SetBool("isWalking", false);

        if (Vector3.Distance(transform.position, lastKnownPlayerPosition) < 0.5f)
        {
            TransitionToState(AIState.Patrol);
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
            animator.SetBool("isWalking", false);
            return;
        }

        RotateTowards(movementDirection);
        animator.SetBool("isWalking", true);
        rb.MovePosition(rb.position + movementDirection * moveSpeed * Time.deltaTime);
    }

    void RotateTowards(Vector3 targetDirection)
    {
        if (targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    void TransitionToState(AIState newState)
    {
        if (currentState == newState) return;

        Debug.Log($"Transizione da {currentState} a {newState}");

        if (newState == AIState.Patrol)
        {
            animator.SetBool("isChasing", false);
            animator.SetBool("isWalking", true);
            HandlePatrolState();
        }
        else if (newState == AIState.Chase)
        {
            animator.SetBool("isChasing", true);
            animator.SetBool("isWalking", false);
        }

        currentState = newState;
    }

    void TeleportToNearestWaypoint()
    {
        Transform closestWaypoint = FindClosestWaypoint(transform.position);
        if (closestWaypoint != null)
        {
            TeleportToWaypoint(closestWaypoint);
            Debug.Log("Preside teletrasportato al waypoint più vicino.");

            ResetToClosestPatrolPoint(closestWaypoint);
            TransitionToState(AIState.Patrol);
        }
        else
        {
            Debug.LogWarning("Nessun waypoint trovato per il teletrasporto.");
        }
    }

    Transform FindClosestWaypoint(Vector3 position)
    {
        Transform closestWaypoint = null;
        float closestDistance = Mathf.Infinity;

        foreach (Transform waypoint in waypoints)
        {
            if (waypoint == null) continue;

            float distance = Vector3.Distance(position, waypoint.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestWaypoint = waypoint;
            }
        }

        return closestWaypoint;
    }

    void TeleportToWaypoint(Transform waypoint)
    {
        if (waypoint == null)
        {
            Debug.LogError("Waypoint nullo. Impossibile teletrasportarsi.");
            return;
        }

        if (genSuit != null)
        {
            genSuit.position = waypoint.position;
        }
        else
        {
            transform.position = waypoint.position;
        }

        RotateTowards(waypoint.position - transform.position);
    }

    void ResetToClosestPatrolPoint(Transform waypoint)
    {
        if (patrolPoints.Length == 0) return;

        float closestDistance = Mathf.Infinity;
        int closestIndex = 0;

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            float distance = Vector3.Distance(patrolPoints[i].position, waypoint.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        currentPatrolIndex = closestIndex;
        Debug.Log("Ripristinato il punto di pattuglia più vicino: " + patrolPoints[currentPatrolIndex].name);
    }

    public void ReceiveAlert(Vector3 alertPosition)
    {
        Transform closestWaypoint = FindClosestWaypoint(alertPosition);
        if (closestWaypoint != null)
        {
            TeleportToWaypoint(closestWaypoint);
            TransitionToState(AIState.Alert);
        }
        else
        {
            Debug.LogWarning("Nessun waypoint trovato.");
        }
    }
}
