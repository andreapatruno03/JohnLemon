using UnityEngine;
using System.Collections;


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

    [Header("Path Points")]
    [SerializeField]
    public Transform[] pathPoints; // Lista dei punti per seguire i path points


    private int currentPathIndex = 0; // Indice del path point corrente


    [Header("Teleportation Settings")]
    public float teleportDistanceThreshold = 30f; // Distanza per il teletrasporto
    public float noTeleportRadius = 30f; // Raggio in cui il Preside non può teletrasportarsi
    public int maxTeleports = 4; // Numero massimo di teletrasporti
    private int currentTeleportCount = 0; // Contatore dei teletrasporti eseguiti
    private bool isPlayerNearby = false; // Variabile booleana per verificare la vicinanza del player
    private bool canTeleport = true; // Controlla se il Preside può teletrasportarsi
    private bool isMoving = false;

    // Components
    private Rigidbody rb;
    private Animator animator;
    public Transform player;

    public enum MovementMode { PathPoints, PatrolPoints }
    public MovementMode currentMovementMode = MovementMode.PathPoints;
    private int currentPatrolIndex = 0;
    private Vector3 lastKnownPlayerPosition;
    private Vector3 movementDirection;
    private float teleportCooldownTimer = 0f;


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
        if (rb == null || animator == null) return;

        // Debug per diagnosticare
        Debug.Log($"pathPoints: {(pathPoints != null ? pathPoints.Length.ToString() : "null")}");
        Debug.Log($"patrolPoints: {(patrolPoints != null ? patrolPoints.Length.ToString() : "null")}");
        Debug.Log($"currentMovementMode: {currentMovementMode}");

        // Aggiorna lo stato di vicinanza del player
        UpdatePlayerProximity();

        // Controlla se deve eseguire il teletrasporto
        if (currentTeleportCount < maxTeleports && !isPlayerNearby && canTeleport)
        {
            float playerDistance = Vector3.Distance(transform.position, fieldOfView.detectedTarget?.position ?? Vector3.zero);

            // Teletrasporto quando il giocatore è fuori dal raggio di azione
            if (playerDistance > teleportDistanceThreshold && playerDistance > noTeleportRadius)
            {
                StartCoroutine(TeleportCooldown());
            }
        }

        // Gestisce il comportamento di movimento (path points o patrol points)
        HandleMovement();

        // Controlla il campo visivo per cambiare stato
        if (fieldOfView != null && fieldOfView.CanSeeTarget)
        {
            if (fieldOfView.detectedTarget != null)
            {
                lastKnownPlayerPosition = fieldOfView.detectedTarget.position;
                TransitionToState(AIState.Chase);
            }
        }

        // Gestisce il comportamento basato sullo stato corrente
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


    void FollowPoints(Transform[] points, ref int index)
    {
        if (points == null || points.Length == 0)
        {
            Debug.LogWarning("Punti non validi per FollowPoints.");
            return;
        }

        Transform target = points[index];
        if (target == null)
        {
            Debug.LogWarning("Il punto target è nullo.");
            return;
        }

        Debug.Log($"Seguendo il punto {target.name} (indice: {index})");

        MoveToPoint(target, normalSpeed);

        if (Vector3.Distance(transform.position, target.position) < 0.5f)
        {
            index = (index + 1) % points.Length;
            Debug.Log($"Passa al prossimo punto: {points[index].name}");
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


    Vector3 FindAlternativeDirection()
    {
        float angleStep = 30f;
        int maxChecks = 12;
        for (int i = 1; i <= maxChecks; i++)
        {
            float angle = i * angleStep;

            Vector3 rightDirection = Quaternion.Euler(0, angle, 0) * movementDirection;
            if (!Physics.Raycast(transform.position, rightDirection, 1f))
            {
                Debug.Log("Direzione alternativa trovata a destra: " + angle);
                return rightDirection.normalized;
            }

            Vector3 leftDirection = Quaternion.Euler(0, -angle, 0) * movementDirection;
            if (!Physics.Raycast(transform.position, leftDirection, 1f))
            {
                Debug.Log("Direzione alternativa trovata a sinistra: " + -angle);
                return leftDirection.normalized;
            }
        }

        return Vector3.zero;
    }

    Transform FindClosestPoint(Transform[] points)
    {
        if (points == null || points.Length == 0)
        {
            Debug.LogWarning("Array di punti nullo o vuoto.");
            return null;
        }

        Transform closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (Transform point in points)
        {
            if (point == null) continue;

            float distance = Vector3.Distance(transform.position, point.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = point;
            }
        }

        return closest;
    }



    Transform FindNextPoint(Transform[] points, Vector3 currentPosition)
    {
        Transform closestPoint = null;
        float closestDistance = Mathf.Infinity;

        foreach (Transform point in points)
        {
            if (point == null) continue;

            float distance = Vector3.Distance(currentPosition, point.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPoint = point;
            }
        }

        return closestPoint;
    }




    void HandleMovement()
    {
        switch (currentMovementMode)
        {
            case MovementMode.PathPoints:
                if (pathPoints == null || pathPoints.Length == 0)
                {
                    Debug.LogWarning("PathPoints non assegnati o vuoti.");
                    return;
                }
                FollowPoints(pathPoints, ref currentPathIndex);
                break;

            case MovementMode.PatrolPoints:
                if (patrolPoints == null || patrolPoints.Length == 0)
                {
                    Debug.LogWarning("PatrolPoints non assegnati o vuoti.");
                    return;
                }
                Transform closestPatrolPoint = FindClosestPoint(patrolPoints);
                if (closestPatrolPoint != null)
                {
                    MoveToPoint(closestPatrolPoint, normalSpeed);
                }
                else
                {
                    Debug.LogWarning("Nessun Patrol Point valido trovato.");
                }
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
        if (isMoving) return; // Evita conflitti di movimento

        movementDirection = (position - transform.position).normalized;

        if (movementDirection.magnitude < 0.1f)
        {
            Debug.Log("Movimento completato: vicino al punto target.");
            animator.SetBool("isWalking", false);
            isMoving = false; // Libera il flag
            return;
        }

        rb.MovePosition(rb.position + movementDirection * moveSpeed * Time.deltaTime);
    }


    private IEnumerator MoveToPatrolPoint(Transform target)
    {
        if (target == null)
        {
            Debug.LogError("Target nullo nel movimento verso il Patrol Point.");
            yield break;
        }

        animator.SetBool("isWalking", true);

        while (Vector3.Distance(rb.position, target.position) > 0.5f)
        {
            Vector3 direction = (target.position - rb.position).normalized;

            RotateTowards(direction);

            rb.MovePosition(rb.position + direction * normalSpeed * Time.deltaTime);

            yield return null; // Aspetta il frame successivo
        }

        animator.SetBool("isWalking", false);

        // Passa al prossimo punto di pattugliamento
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        Debug.Log($"Raggiunto il Patrol Point: {target.name}");
    }



    void MoveToPoint(Transform target, float speed)
    {
        if (target == null)
        {
            Debug.LogWarning("Target nullo per MoveToPoint.");
            return;
        }

        Debug.Log($"Muovendo verso il punto: {target.name}");
        MoveTowardsPosition(target.position, speed);
    }




    void MoveTowardsNextPatrolPoint()
    {
        if (patrolPoints.Length == 0) return;

        Transform targetPatrolPoint = patrolPoints[currentPatrolIndex];
        if (targetPatrolPoint != null)
        {
            Vector3 direction = (targetPatrolPoint.position - rb.position).normalized;

            // Muovi il Preside verso il prossimo punto usando Rigidbody
            rb.MovePosition(rb.position + direction * normalSpeed * Time.deltaTime);
        }
    }


    void ResetToClosestPatrolPoint(Transform waypoint)
    {
        if (patrolPoints.Length == 0)
        {
            Debug.LogWarning("Nessun Patrol Point disponibile.");
            return;
        }

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
        Debug.Log($"Ripristinato il punto di pattugliamento più vicino: {patrolPoints[currentPatrolIndex].name}");
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

    public void ResetTeleportCount()
    {
        currentTeleportCount = 0;
        Debug.Log("Contatore dei teletrasporti resettato.");
    }
   

    void RotateTowards(Vector3 targetDirection)
    {
        if (targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    void StartMovingAfterTeleport()
    {
        if (isMoving) return; // Evita conflitti
        isMoving = true;

        if (patrolPoints.Length == 0)
        {
            Debug.LogWarning("Nessun Patrol Point disponibile per il movimento.");
            isMoving = false;
            return;
        }

        Transform targetPatrolPoint = patrolPoints[currentPatrolIndex];
        if (targetPatrolPoint == null)
        {
            Debug.LogError("Il Patrol Point target è nullo.");
            isMoving = false;
            return;
        }

        Debug.Log($"Avvio del movimento verso il Patrol Point: {targetPatrolPoint.name}");
        StartCoroutine(MoveToPatrolPoint(targetPatrolPoint));
    }




    IEnumerator TeleportCooldown()
    {
        canTeleport = false;
        teleportCooldownTimer = 3f; // Imposta il timer
        TeleportToNearestWaypoint();
        while (teleportCooldownTimer > 0)
        {
            teleportCooldownTimer -= Time.deltaTime;
            yield return null;
        }
        canTeleport = true;
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
        else if (newState == AIState.Patrol)
        {
            StartMovingAfterTeleport();
        }
        else if (newState == AIState.Alert || newState == AIState.Investigate)
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

            currentTeleportCount++;

            currentMovementMode = MovementMode.PatrolPoints;

            Debug.Log("Il Preside ora continuerà a seguire i patrol points.");
        }
        else
        {
            Debug.LogWarning("Nessun waypoint trovato per il teletrasporto.");
        }
    }


    void TeleportToWaypoint(Transform waypoint)
    {
        if (waypoint == null)
        {
            Debug.LogError("Waypoint nullo. Impossibile teletrasportarsi.");
            return;
        }

        Debug.Log($"Teletrasporto al waypoint: {waypoint.name}");

        // Teletrasporta al waypoint
        rb.position = waypoint.position; // Usa Rigidbody per impostare la posizione

        RotateTowards(waypoint.position - transform.position);

        // Cambia lo stato a Patrol dopo il teletrasporto
        TransitionToState(AIState.Patrol);

        Debug.Log("Stato cambiato a Patrol dopo il teletrasporto.");
    }



    void UpdatePlayerProximity()
    {
        // Ottieni il riferimento al Transform del giocatore (assicurati che sia assegnato correttamente)
        Transform playerTransform = fieldOfView.detectedTarget; // O qualsiasi riferimento al giocatore

        if (playerTransform != null) // Controlla che il riferimento al giocatore non sia nullo
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            if (distance <= noTeleportRadius)
            {
                isPlayerNearby = true; // Il giocatore è vicino
            }
            else
            {
                isPlayerNearby = false; // Il giocatore è fuori dall'area
            }
        }
        else
        {
            isPlayerNearby = false; // Non c'è riferimento al giocatore
        }
    }

    bool ValidatePoints(Transform[] points)
    {
        if (points == null || points.Length == 0)
        {
            Debug.LogWarning("Nessun punto valido trovato nei PathPoints.");
            return false;
        }
        Debug.Log("Tutti i PathPoints sono validi.");
        return true;
    }


}
