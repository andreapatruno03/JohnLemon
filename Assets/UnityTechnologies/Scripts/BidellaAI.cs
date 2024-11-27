using UnityEngine;

public class BidellaAI : MonoBehaviour
{
    [Header("Patrol Settings")]
    public Transform[] patrolPoints; // Array di punti di pattuglia
    public float patrolSpeed = 2f; // Velocità di pattuglia
    public float rotationSpeed = 5f; // Velocità di rotazione
    public float pointReachThreshold = 0.5f; // Distanza per considerare un punto raggiunto

    // State Tracking
    private int currentPatrolIndex = 0;
    private Rigidbody rb;
    private Animator animator;

    void Start()
    {
        // Inizializza i componenti
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        if (patrolPoints.Length == 0)
        {
            Debug.LogWarning("Nessun punto di pattuglia assegnato!");
        }

        if (rb == null)
        {
            Debug.LogError("Rigidbody non trovato!");
        }

        if (animator == null)
        {
            Debug.LogWarning("Animator non trovato!");
        }
    }

    void Update()
    {
        // Aggiorna il movimento
        if (patrolPoints.Length > 0)
        {
            Patrol();
        }
    }

    void Patrol()
    {
        // Ottieni il punto di destinazione attuale
        Transform targetPoint = patrolPoints[currentPatrolIndex];

        // Calcola la direzione verso il punto
        Vector3 direction = (targetPoint.position - transform.position).normalized;

        // Ruota verso il punto di destinazione
        RotateTowards(direction);

        // Muovi verso il punto
        Vector3 newPosition = transform.position + direction * patrolSpeed * Time.deltaTime;
        rb.MovePosition(newPosition);

        // Attiva l'animazione di camminata
        if (animator != null)
        {
            animator.SetBool("isWalking", true);
        }

        // Controlla se è stato raggiunto il punto
        if (Vector3.Distance(transform.position, targetPoint.position) <= pointReachThreshold)
        {
            // Passa al prossimo punto
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }
    }

    void RotateTowards(Vector3 direction)
    {
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
}
