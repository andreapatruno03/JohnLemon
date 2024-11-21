using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CityPeople
{
    public class CityPeople : MonoBehaviour
    {
        private Animator animator;
        public float rotationSpeed = 10f; // Velocità di rotazione
        public float movementSpeed = 3f; // Velocità di movimento
        private Vector3 lastDirection = Vector3.zero; // Direzione precedente

        void Start()
        {
            animator = GetComponent<Animator>();
        }

        void Update()
        {
            // Ottieni la direzione di movimento dai tasti di input
            Vector3 direction = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

            // Controlla se c'è movimento
            if (direction.magnitude > 0.1f)
            {
                // Movimento del personaggio
                transform.Translate(Vector3.forward * movementSpeed * Time.deltaTime);

                // Rotazione verso la direzione del movimento
                Quaternion targetRotation = Quaternion.LookRotation(-direction, Vector3.up);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

                // Attiva l'animazione di camminata
                if (animator != null)
                {
                    animator.SetBool("isWalking", true);
                }

                // Aggiorna la direzione precedente
                lastDirection = direction;
            }
            else
            {
                // Ferma l'animazione di camminata
                if (animator != null)
                {
                    animator.SetBool("isWalking", false);
                }
            }
        }
    }
}
