using UnityEngine;
using UnityEngine.SceneManagement; // Per ricaricare la scena
using System.Collections; // Necessario per IEnumerator

public class PlayerHealth : MonoBehaviour
{
    public GameObject gameOverScreen; // Assegna il GameObject contenente il messaggio "Game Over"
    public float restartDelay = 5f; // Tempo prima del riavvio (opzionale)

    void Start()
    {
        // Assicurati che il GameOver screen sia inizialmente disattivato
        if (gameOverScreen != null)
        {
            gameOverScreen.SetActive(false);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Controlla se il giocatore è stato colpito da un NPC
        if (collision.gameObject.CompareTag("NPC"))
        {
            Debug.Log("Giocatore colpito da NPC!");
            GameOver(); // Termina il gioco
        }
    }

    void GameOver()
    {
        Debug.Log("Game Over!");

        // Mostra la schermata di Game Over
        if (gameOverScreen != null)
        {
            gameOverScreen.SetActive(true);
            StartCoroutine(FadeInGameOverScreen()); // Effetto dissolvenza
        }
        else
        {
            Debug.LogWarning("GameOverScreen non assegnato nel Inspector!");
        }

        // Disabilita il movimento del giocatore (opzionale)
        var playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        // Riavvia il gioco dopo il ritardo
        Invoke("RestartGame", restartDelay);
    }

    void RestartGame()
    {
        // Ricarica la scena corrente
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    IEnumerator FadeInGameOverScreen()
    {
        // Ottieni il CanvasGroup per controllare l'alpha
        CanvasGroup canvasGroup = gameOverScreen.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            Debug.LogWarning("CanvasGroup non trovato sul GameOverScreen!");
            yield break;
        }

        float duration = 1f; // Durata della dissolvenza
        float elapsed = 0f;

        // Graduale aumento dell'alpha
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / duration); // Aggiorna l'alpha
            yield return null;
        }
    }
}
