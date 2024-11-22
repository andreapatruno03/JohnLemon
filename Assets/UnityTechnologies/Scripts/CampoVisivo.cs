using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldOfView : MonoBehaviour
{
    [Header("Vision Settings")]
    public float viewRadius = 15f; // Raggio di visione
    [Range(0, 360)]
    public float viewAngle = 120f; // Angolo di visione

    [Header("Layer Masks")]
    public LayerMask targetMask; // Layer del giocatore o degli oggetti da rilevare
    public LayerMask obstacleMask; // Layer degli ostacoli

    [Header("Movement")]
    public float moveSpeed = 3f; // Velocità di movimento verso il giocatore
    public float turnSpeed = 5f; // Velocità di rotazione verso il giocatore

    [Header("State")]
    public Transform player; // Riferimento al giocatore
    public bool CanSeeTarget { get; private set; } // Controlla se vede il target

    private Animator m_Animator;
    private Rigidbody m_Rigidbody;
    private Vector3 m_Movement;
    private Quaternion m_Rotation = Quaternion.identity;

    void Start()
    {
        // Recupera i componenti
        m_Animator = GetComponent<Animator>();
        m_Rigidbody = GetComponent<Rigidbody>();

        // Controlla i riferimenti nulli
        if (m_Animator == null)
            Debug.LogError("Animator non trovato sul GameObject!");
        if (m_Rigidbody == null)
            Debug.LogError("Rigidbody non trovato sul GameObject!");
    }

    void Update()
    {
        // Trova i target visibili
        FindVisibleTargets();

        if (CanSeeTarget)
        {
            MoveTowardsPlayer();
        }
        else
        {
            StopMovement();
        }
    }

    private void FindVisibleTargets()
    {
        Collider[] targetsInViewRadius = Physics.OverlapSphere(transform.position, viewRadius, targetMask);
        CanSeeTarget = false;

        foreach (Collider target in targetsInViewRadius)
        {
            Transform targetTransform = target.transform;
            Vector3 directionToTarget = (targetTransform.position - transform.position).normalized;

            // Verifica se il target è nell'angolo di visione
            if (Vector3.Angle(transform.forward, directionToTarget) < viewAngle / 2)
            {
                float distanceToTarget = Vector3.Distance(transform.position, targetTransform.position);

                // Controlla gli ostacoli tra NPC e il target
                if (!Physics.Raycast(transform.position, directionToTarget, distanceToTarget, obstacleMask))
                {
                    CanSeeTarget = true;
                    player = targetTransform; // Salva il riferimento al giocatore
                    Debug.Log("Giocatore rilevato!");
                    break;
                }
            }
        }

        if (!CanSeeTarget)
        {
            Debug.Log("Giocatore non rilevato.");
        }
    }

    private void MoveTowardsPlayer()
    {
        if (player == null)
        {
            Debug.LogError("Player non assegnato!");
            return;
        }

        Vector3 direction = (player.position - transform.position).normalized;
        m_Movement = direction * moveSpeed * Time.deltaTime;

        bool isWalking = m_Movement.magnitude > 0f;
        m_Animator?.SetBool("isWalking", isWalking);

        Vector3 desiredForward = Vector3.RotateTowards(transform.forward, direction, turnSpeed * Time.deltaTime, 0f);
        m_Rotation = Quaternion.LookRotation(desiredForward);

        m_Rigidbody.MovePosition(m_Rigidbody.position + m_Movement);
        m_Rigidbody.MoveRotation(m_Rotation);
    }

    private void StopMovement()
    {
        m_Movement = Vector3.zero;
        m_Animator?.SetBool("isWalking", false);
    }

    void OnAnimatorMove()
    {
        if (CanSeeTarget)
        {
            m_Rigidbody.MovePosition(m_Rigidbody.position + m_Movement * m_Animator.deltaPosition.magnitude);
            m_Rigidbody.MoveRotation(m_Rotation);
        }
    }

    public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angleInDegrees += transform.eulerAngles.y;
        }
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        Vector3 leftBoundary = DirFromAngle(-viewAngle / 2, false) * viewRadius;
        Vector3 rightBoundary = DirFromAngle(viewAngle / 2, false) * viewRadius;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary);

        Gizmos.color = new Color(0, 1, 1, 0.2f);
        for (float i = 0; i <= 1f; i += 0.1f)
        {
            Vector3 interpLine = Vector3.Lerp(leftBoundary, rightBoundary, i);
            Gizmos.DrawLine(transform.position, transform.position + interpLine);
        }
    }
}