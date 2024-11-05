using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CityPeople
{
    public class CityPeople : MonoBehaviour
    {
        private AnimationClip[] myClips;
        private Animator animator;

        public float rotationSpeed = 10f; // Velocità di rotazione
        private Vector3 lastDirection = Vector3.zero; // Direzione precedente

        void Start()
        {
            animator = GetComponent<Animator>();
            if (animator != null)
            {
                myClips = animator.runtimeAnimatorController.animationClips;
                PlayAnyClip();
                StartCoroutine(ShuffleClips());
            }
        }

        void Update()
        {
            // Ottieni la direzione di movimento dai tasti di input
            Vector3 direction = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

            // Verifica che ci sia un movimento
            if (direction.magnitude > 0.1f)
            {
                // Confronta la nuova direzione con quella precedente
                if (Vector3.Dot(direction, lastDirection) < 0)
                {
                    // Ruota di 180 gradi se la direzione è opposta
                    transform.Rotate(0, 180f, 0);
                }

                // Aggiorna la direzione precedente
                lastDirection = direction;
            }
        }

        void PlayAnyClip()
        {
            var cl = myClips[Random.Range(0, myClips.Length)];
            animator.CrossFadeInFixedTime(cl.name, 1.0f, -1, Random.value * cl.length);
        }

        IEnumerator ShuffleClips()
        {
            while (true)
            {
                yield return new WaitForSeconds(15.0f + Random.value * 5.0f);
                PlayAnyClip();
            }
        }
    }
}
