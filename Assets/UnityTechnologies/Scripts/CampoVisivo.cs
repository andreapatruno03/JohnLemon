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

    // Usa un campo pubblico per mostrare il target rilevato nell'Inspector
    [Header("State")]
    [SerializeField]
    public Transform detectedTarget; // Giocatore rilevato
    public bool CanSeeTarget { get; private set; } // Controlla se vede il target

    void Update()
    {
        FindVisibleTargets();
    }

    private void FindVisibleTargets()
    {
        Collider[] targetsInViewRadius = Physics.OverlapSphere(transform.position, viewRadius, targetMask);
        CanSeeTarget = false;
        detectedTarget = null;

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
                    detectedTarget = targetTransform; // Assegna il target rilevato
                    Debug.Log("Giocatore rilevato!");
                    return;
                }
            }
        }

        Debug.Log("Giocatore non rilevato.");
    }

    public Transform GetDetectedTarget()
    {
        return detectedTarget;
    }
}