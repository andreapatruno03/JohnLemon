using UnityEngine;

public class SimpleAI : MonoBehaviour
{
    public enum AIState { Normal, Patrol, Chase }
    public AIState currentState = AIState.Patrol;

    public Transform[] patrolPoints;
    public float speed = 2f;
    public Transform player;
    public float detectionRange = 5f;

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
    rb.MovePosition(transform.position + direction * speed * Time.deltaTime);

    animator.SetFloat("speed", speed);

    if (Vector3.Distance(transform.position, target.position) < 0.2f)
    {
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
    }
}


    void ChasePlayer()
    {
        if (player == null) return;

        Vector3 direction = (player.position - transform.position).normalized;

        rb.MovePosition(transform.position + direction * speed * Time.deltaTime);
        animator.SetFloat("speed", speed); // Continua animazione "walk"

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
}
