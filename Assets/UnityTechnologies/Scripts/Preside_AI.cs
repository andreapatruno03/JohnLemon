using UnityEngine;

public class PresideAI : MonoBehaviour
{
    public enum AIState { Patrol, Chase, Alert, Investigate }

    [Header("Waypoints")]
    public Transform[] waypoints; // Lista dei waypoint per il teletrasporto

    [Header("Patrol Points")]
    public Transform[] patrolPoints; // Lista dei punti di pattuglia

    [Header("State Management")]
    public AIState currentState = AIState.Patrol;

    [Header("Movement")]
    public float normalSpeed = 2f; // Velocità di pattugliamento
    public float rotationSpeed = 5f; // Velocità di rotazione

    [Header("Field of View Reference")]
    public FieldOfView fieldOfView; // Riferimento al campo visivo

    [Header("Communication with Other NPCs")]
    public LayerMask npcLayer; // Layer per rilevare altri NPC
    public float npcAlertRadius = 10f; // Raggio di comunicazione

    [Header("References")]
    public Transform genSuit; // Riferimento all'oggetto principale del Preside

    // Components
    private Rigidbody rb;
    private Animator animator;

    // Indice del patrol point corrente
    private int currentPatrolIndex = 0;
    private Vector3 lastKnownPlayerPosition;
    private Vector3 movementDirection;

    // Distanza per il teletrasporto
    public float teleportDistanceThreshold = 20f; // Cambia il valore per regolare la distanza

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
            Debug.LogError("Rigidbody non trovato! Aggiungi un Rigidbody al Preside.");
        }
        if (animator == null)
        {
            Debug.LogError("Animator non trovato! Aggiungi un Animator al Preside.");
        }
        if (genSuit == null)
        {
            Debug.LogError("genSuit non assegnato! Assicurati di assegnarlo nell'Inspector.");
        }
        if (fieldOfView == null)
        {
            Debug.LogError("FieldOfView non assegnato! Assicurati di assegnarlo nell'Inspector.");
        }
        if (patrolPoints.Length == 0)
        {
            Debug.LogWarning("Nessun Patrol Point assegnato! Aggiungili nell'Inspector.");
        }
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

        // Movimento verso il prossimo patrol point
        Transform target = patrolPoints[currentPatrolIndex];
        MoveTowardsPosition(target.position, normalSpeed);

        // Passa al prossimo punto quando il Preside raggiunge quello attuale
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
            // Se il giocatore non è visibile, passa ad Alert
            TransitionToState(AIState.Alert);
            return;
        }

        // Attiva l'animazione di corsa
        animator.SetBool("isChasing", true);

        // Muovi verso il giocatore
        MoveTowardsPosition(fieldOfView.detectedTarget.position, normalSpeed * 1.5f); // Velocità aumentata per l'inseguimento
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
            movementDirection = Vector3.zero;
            animator.SetBool("isWalking", false); // Ferma l'animazione di camminata
            return;
        }

        // Usa Raycast per rilevare ostacoli davanti al Preside
        if (Physics.Raycast(transform.position, movementDirection, out RaycastHit hit, 1f))
        {
            if (hit.collider != null && hit.collider.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
            {
                Debug.Log("Ostacolo rilevato: " + hit.collider.name);

                // Trova una nuova direzione libera
                movementDirection = FindAlternativeDirection();
                if (movementDirection == Vector3.zero)
                {
                    // Se non trova una direzione libera, ferma il movimento
                    Debug.Log("Ostacolo bloccante, fermo.");
                    animator.SetBool("isWalking", false);
                    return;
                }
            }
        }

        RotateTowards(movementDirection);

        // Attiva l'animazione di camminata
        animator.SetBool("isWalking", true);

        // Muovi il Preside nella direzione calcolata
        rb.MovePosition(rb.position + movementDirection * moveSpeed * Time.deltaTime);
    }

    Vector3 FindAlternativeDirection()
    {
        float angleStep = 30f; // Incremento degli angoli
        int maxChecks = 12; // Numero massimo di direzioni da verificare
        for (int i = 1; i <= maxChecks; i++)
        {
            float angle = i * angleStep;

            // Prova a destra
            Vector3 rightDirection = Quaternion.Euler(0, angle, 0) * movementDirection;
            if (!Physics.Raycast(transform.position, rightDirection, 1f))
            {
                Debug.Log("Direzione alternativa trovata a destra: " + angle);
                return rightDirection.normalized;
            }

            // Prova a sinistra
            Vector3 leftDirection = Quaternion.Euler(0, -angle, 0) * movementDirection;
            if (!Physics.Raycast(transform.position, leftDirection, 1f))
            {
                Debug.Log("Direzione alternativa trovata a sinistra: " + -angle);
                return leftDirection.normalized;
            }
        }

        // Se tutte le direzioni sono bloccate, restituisci un vettore nullo
        return Vector3.zero;
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
        if (newState == AIState.Chase)
        {
            animator.SetBool("isChasing", true); 
            animator.SetBool("isWalking", false); 
        }
        else if (newState == AIState.Patrol || newState == AIState.Alert || newState == AIState.Investigate)
        {
            animator.SetBool("isChasing", false); 
            animator.SetBool("isWalking", true); 
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
        }
        RotateTowards(closestWaypoint.position - transform.position);

        // Cambia lo stato a Patrol dopo il teletrasporto
        TransitionToState(AIState.Patrol);

        // Reimposta il movimento ai Patrol Points
        ResetToClosestPatrolPoint(closestWaypoint);

        // Inizia immediatamente a seguire il Patrol Point
        HandlePatrolState();
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

        Debug.Log("Teletrasporto al waypoint: " + waypoint.name);

        // Teletrasporta al waypoint
        if (genSuit != null)
        {
            genSuit.position = waypoint.position;
        }
        else
        {
            transform.position = waypoint.position;
        }

        RotateTowards(waypoint.position - transform.position);

        // Cambia lo stato a Patrol dopo il teletrasporto
        TransitionToState(AIState.Patrol);

        // Reimposta il movimento ai Patrol Points
        ResetToClosestPatrolPoint(waypoint);

        // Inizia immediatamente a seguire il Patrol Point
        HandlePatrolState();
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
        Debug.Log("ReceiveAlert chiamato con posizione: " + alertPosition);

        Transform closestWaypoint = FindClosestWaypoint(alertPosition);
        if (closestWaypoint != null)
        {
            Debug.Log("Waypoint più vicino trovato: " + closestWaypoint.name);
            TeleportToWaypoint(closestWaypoint);
            TransitionToState(AIState.Alert);
        }
        else
        {
            Debug.LogWarning("Nessun waypoint trovato.");
        }
    }
}
