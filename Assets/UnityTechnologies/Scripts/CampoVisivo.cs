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

    private Vector3 m_Movement;
    private Quaternion m_Rotation = Quaternion.identity;

    void Update()
    {
        // Trova i target visibili
        FindVisibleTargets();

        if (CanSeeTarget)
        {
            MoveTowardsPlayer();
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

            if (Vector3.Angle(transform.forward, directionToTarget) < viewAngle / 2)
            {
                float distanceToTarget = Vector3.Distance(transform.position, targetTransform.position);

                if (!Physics.Raycast(transform.position, directionToTarget, distanceToTarget, obstacleMask))
                {
                    CanSeeTarget = true;
                    player = targetTransform;
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
            Debug.LogWarning("Player non assegnato!");
            return;
        }

        Vector3 direction = (player.position - transform.position).normalized;
        m_Movement = direction * moveSpeed * Time.deltaTime;

        // Rotazione verso il giocatore
        Vector3 desiredForward = Vector3.RotateTowards(transform.forward, direction, turnSpeed * Time.deltaTime, 0f);
        m_Rotation = Quaternion.LookRotation(desiredForward);

        // Movimento tramite Transform
        transform.position += m_Movement;
        transform.rotation = m_Rotation;
    }
}