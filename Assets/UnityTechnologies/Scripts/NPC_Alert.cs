using UnityEngine;

public class NPC_Alert : MonoBehaviour
{
    [Header("Player Detection")]
    public Transform player; // Riferimento al giocatore (opzionale, usato solo come fallback)
    public float detectionRange = 5f; // Raggio di rilevamento
    [Range(0, 360)] public float fieldOfViewAngle = 90f; // Angolo di campo visivo

    [Header("Field of View")]
    public FieldOfView fieldOfView; // Riferimento al componente FieldOfView

    [Header("Preside Reference")]
    public PresideAI presideAI; // Riferimento al Preside

    [Header("Layers")]
    public LayerMask obstacleLayer; // Layer per gli ostacoli

    public bool playerDetected { get; private set; }
    public Vector3 playerPosition { get; private set; }

    void Update()
    {
        DetectPlayer();
        if (playerDetected)
        {
            AlertPreside();
        }
    }

    void DetectPlayer()
    {
        playerDetected = false;

        // Usa il FieldOfView se assegnato
        if (fieldOfView != null && fieldOfView.CanSeeTarget)
        {
            if (fieldOfView.detectedTarget != null)
            {
                playerDetected = true;
                playerPosition = fieldOfView.detectedTarget.position;
                return;
            }
        }

        // Fallback: controlla manualmente
        if (player == null) return;

        // Controlla la distanza dal giocatore
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > detectionRange) return;

        // Controlla se il giocatore è nell'angolo di campo visivo
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);
        if (angle > fieldOfViewAngle * 0.5f) return;

        // Controlla se il giocatore è visibile (non bloccato da ostacoli)
        if (Physics.Raycast(transform.position, directionToPlayer, out RaycastHit hit, detectionRange, obstacleLayer))
        {
            if (hit.transform != player) return;
        }

        // Giocatore rilevato
        playerDetected = true;
        playerPosition = player.position;
    }


    void AlertPreside()
    {
        if (presideAI != null)
        {
            // Ottieni la posizione dell'allarme (posizione dell'NPC)
            Vector3 alertPosition = transform.position;
            presideAI.ReceiveAlert(alertPosition);
        }
        else
        {
            Debug.LogWarning("Preside non trovato nella scena o riferimento non assegnato!");
        }
    }
}