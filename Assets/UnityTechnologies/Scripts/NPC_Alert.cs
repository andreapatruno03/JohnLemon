using UnityEngine;

public class NPC_Alert : MonoBehaviour
{
    [Header("Player Detection")]
    public Transform player; // Riferimento al giocatore
    public float detectionRange = 5f; // Raggio di rilevamento
    [Range(0, 360)] public float fieldOfViewAngle = 90f; // Angolo di visione

    [Header("Preside Reference")]
    public PresideAI presideAI; // Riferimento al PresideAI

    [Header("Layers")]
    public LayerMask obstacleLayer; // Layer degli ostacoli

    public bool playerDetected { get; private set; } // Stato: giocatore rilevato
    public Vector3 playerPosition { get; private set; } // Posizione del giocatore rilevata

    void Update()
    {
        // Rileva il giocatore e avvisa il Preside se rilevato
        DetectPlayer();
        if (playerDetected)
        {
            AlertPreside();
        }
    }

    void DetectPlayer()
    {
        playerDetected = false;

        if (player == null)
        {
            Debug.LogWarning("Riferimento al giocatore non assegnato!");
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > detectionRange) return;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);

        if (angle > fieldOfViewAngle * 0.5f) return;

        if (Physics.Raycast(transform.position, directionToPlayer, out RaycastHit hit, detectionRange))
        {
            if (hit.transform != player) return;
        }

        playerDetected = true;
        playerPosition = player.position;
    }

    public void AlertPreside()
    {
        if (presideAI != null)
        {
            presideAI.AlertPreside(playerPosition);
            Debug.Log($"NPC ha avvisato il Preside della posizione del giocatore: {playerPosition}");
        }
        else
        {
            Debug.LogWarning("Il riferimento al Preside non è stato impostato!");
        }
    }
}
