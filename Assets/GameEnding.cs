using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameEnding : MonoBehaviour
{
    public float fadeDuration = 1f; // Durata del fade-in
    public float displayImageDuration = 1f; // Durata della visualizzazione del messaggio
    public GameObject player; // Riferimento al giocatore
    public CanvasGroup exitBackgroundImageCanvasGroup; // Messaggio di vittoria
    public CanvasGroup caughtBackgroundImageCanvasGroup; // Messaggio di sconfitta (opzionale)

    private bool m_IsPlayerAtExit; // Flag per verificare se il giocatore è all'uscita
    private bool m_IsPlayerCaught; // Flag per verificare se il giocatore è catturato
    private float m_Timer; // Timer per gestire il fade

    void Start()
    {
        // Inizializza il timer a 0
        m_Timer = 0f;
    }

    void OnTriggerEnter(Collider other)
    {
        // Controlla se il giocatore è entrato nel trigger
        if (other.gameObject == player)
        {
            m_IsPlayerAtExit = true;
        }
    }

    public void CaughtPlayer()
    {
        // Metodo per segnare il giocatore come catturato
        m_IsPlayerCaught = true;
    }

    void Update()
    {
        // Gestisce l'uscita o la cattura del giocatore
        if (m_IsPlayerAtExit)
        {
            EndLevel(exitBackgroundImageCanvasGroup, false); // Mostra messaggio di vittoria
        }
        else if (m_IsPlayerCaught)
        {
            EndLevel(caughtBackgroundImageCanvasGroup, true); // Mostra messaggio di sconfitta
        }
    }

    void EndLevel(CanvasGroup imageCanvasGroup, bool doRestart)
    {
        // Incrementa il timer
        m_Timer += Time.deltaTime;

        // Esegui il fade-in del Canvas
        imageCanvasGroup.alpha = m_Timer / fadeDuration;

        // Controlla se il timer ha superato la durata totale
        if (m_Timer > fadeDuration + displayImageDuration)
        {
            if (doRestart)
            {
                // Riavvia la scena
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
            else
            {
                // Esci dal gioco
                Debug.Log("Hai vinto!");
                Application.Quit();
            }
        }
    }
}
