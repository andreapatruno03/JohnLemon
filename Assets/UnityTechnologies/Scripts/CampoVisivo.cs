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

    [Header("State")]
    public Transform player; // Riferimento al giocatore
    public bool CanSeeTarget { get; private set; } // Controlla se vede il target

    private IMovementController movementController; // Generico controller del movimento

    private void Awake()
    {
        // Disattiva la sincronizzazione automatica dei trasformatori per evitare effetti di debug impliciti
        Physics.autoSyncTransforms = false;

        // Cerca un componente che implementi IMovementController
        movementController = GetComponent<IMovementController>();

        if (movementController == null)
        {
            Debug.LogError($"Il GameObject '{gameObject.name}' non ha un componente che implementi IMovementController. Verifica la configurazione e l'implementazione dell'interfaccia.");
        }
        else
        {
            Debug.Log($"Trovato componente che implementa IMovementController sul GameObject '{gameObject.name}'.");
        }
    }

    void Update()
    {
        if (movementController == null)
        {
            Debug.LogError($"movementController è null per il GameObject '{gameObject.name}'. Verifica la configurazione.");
            return;
        }

        FindVisibleTargets();

        // Aggiorna lo stato al controller di movimento
        movementController.UpdateVisionState(CanSeeTarget, player);
    }

    private void FindVisibleTargets()
    {
        // Usa OverlapSphere per calcolare i target nel raggio senza mostrare cerchi
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
                    Debug.Log($"{gameObject.name} ha rilevato il giocatore: {player.name}");
                    return;
                }
            }
        }

        CanSeeTarget = false;
        player = null;
    }

    private void OnDrawGizmos()
    {
        // Disegna il campo visivo (senza cerchi gialli)
        Vector3 viewAngleA = DirFromAngle(-viewAngle / 2, false);
        Vector3 viewAngleB = DirFromAngle(viewAngle / 2, false);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + viewAngleA * viewRadius);
        Gizmos.DrawLine(transform.position, transform.position + viewAngleB * viewRadius);

        // Disegna una linea verso il giocatore, se visibile
        if (CanSeeTarget && player != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, player.position);
        }
    }

    // Funzione per calcolare una direzione da un angolo
    private Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angleInDegrees += transform.eulerAngles.y;
        }
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }
}
