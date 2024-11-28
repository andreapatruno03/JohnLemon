using UnityEngine;

public class NPC_Alert : MonoBehaviour
{
    [Header("Player Detection")]
    public Transform player;
    public float detectionRange = 5f;
    [Range(0, 360)] public float fieldOfViewAngle = 90f;

    [Header("Preside Reference")]
    public PresideAI presideAI;

    [Header("Layers")]
    public LayerMask obstacleLayer;

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

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > detectionRange) return;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);
        if (angle > fieldOfViewAngle * 0.5f) return;

        if (Physics.Raycast(transform.position, directionToPlayer, out RaycastHit hit, detectionRange, obstacleLayer))
        {
            if (hit.transform != player) return;
        }

        playerDetected = true;
        playerPosition = player.position;
    }

    void AlertPreside()
{
    if (presideAI != null)
    {
        // Avvisa il Preside della posizione del giocatore
        presideAI.AlertPreside(playerPosition);
        Debug.Log($"NPC ha avvisato il Preside della posizione del giocatore: {playerPosition}");
    }
    else
    {
        Debug.LogWarning("Il riferimento al Preside non è stato impostato!");
    }
}


}