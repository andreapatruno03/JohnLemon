using UnityEngine;

public class SimpleAI : MonoBehaviour
{
    public enum AIState { Normal, Patrol, Chase }
    public AIState currentState = AIState.Patrol;

    public Transform[] patrolPoints;
    public float speed = 2f;
    public Transform player;
    public float detectionRange = 5f;
    public float rotationSpeed = 5f; // Velocità di rotazione

    private int currentPatrolIndex = 0;
    private Rigidbody rb;
    private Animator animator;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        animator.applyRootMotion = false; // Disabilita Root Motion
    }

    void Update()
    {
        switch (currentState)
        {
            case AIState.Normal:
                Wander();
                DetectPlayer();
                break;

            case AIState.Patrol:
                Patrol();
                DetectPlayer();
                break;

            case AIState.Chase:
                ChasePlayer();
                break;
        }
    }

    void Wander()
    {
        animator.SetFloat("speed", 0f); // Torna allo stato di "start" fermo
    }

    void Patrol()
    {
        if (patrolPoints.Length == 0) return;

        Transform target = patrolPoints[currentPatrolIndex];
        Vector3 direction = (target.position - transform.position).normalized;

        // Ruota verso la direzione del target
        RotateTowards(direction);

        // Muovi il personaggio
        rb.MovePosition(transform.position + direction * speed * Time.deltaTime);

        animator.SetFloat("speed", speed);

        // Passa al prossimo punto se vicino all'attuale
        if (Vector3.Distance(transform.position, target.position) < 0.2f)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }
    }

    void ChasePlayer()
    {
        if (player == null) return;

        Vector3 direction = (player.position - transform.position).normalized;

        // Ruota verso il giocatore
        RotateTowards(direction);

        // Muovi il personaggio
        rb.MovePosition(transform.position + direction * speed * Time.deltaTime);
        animator.SetFloat("speed", speed); // Continua animazione "walk"

        // Torna in stato di Patrol se il giocatore è fuori dal raggio
        if (Vector3.Distance(transform.position, player.position) > detectionRange)
        {
            currentState = AIState.Patrol;
        }
    }

    void DetectPlayer()
    {
        if (player != null && Vector3.Distance(transform.position, player.position) < detectionRange)
        {
            currentState = AIState.Chase;
        }
    }

    // Funzione per ruotare verso una direzione
    void RotateTowards(Vector3 targetDirection)
    {
        if (targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
}
