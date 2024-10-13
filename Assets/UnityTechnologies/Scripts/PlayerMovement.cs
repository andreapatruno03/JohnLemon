using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float turnSpeed = 20f; //direzione
    Animator m_Animator;
    Rigidbody m_Rigidbody;
    Vector3 m_Movement;
    Quaternion m_Rotation = Quaternion.identity; //modo per memorizzare le rotazioni

    // Start is called before the first frame update
    void Start()
    {
        m_Animator = GetComponent<Animator> (); //Get a reference to a component of type 'Animator', and assign it to the variable 
        m_Rigidbody = GetComponent<Rigidbody> (); //riferimento al RigidBody, mi serve per applicare il movimento al corpo del personaggio
    }

    // Update is called once per frame
    void Update()
    {
        float horizontal = Input.GetAxis ("Horizontal");
        float vertical = Input.GetAxis ("Vertical");

        m_Movement.Set(horizontal, 0f, vertical); //f perchè è un float
        m_Movement.Normalize (); //per mantenere la stessa direzione, cambiando il magnitudo

        bool hasHorizontalInput = !Mathf.Approximately (horizontal, 0f);//controllo se ho input orizzonale
        bool hasVerticalInput = !Mathf.Approximately (vertical, 0f); //verticale
        bool isWalking = hasHorizontalInput || hasVerticalInput;
        m_Animator.SetBool ("IsWalking", isWalking);

        Vector3 desiredForward = Vector3.RotateTowards (transform.forward, m_Movement, turnSpeed * Time.deltaTime, 0f);
        m_Rotation = Quaternion.LookRotation (desiredForward); //crea rotazione nella direzione del parametro scelto
    }

    void OnAnimatorMove() //applicare il movimento della radice come desideri
    {
        m_Rigidbody.MovePosition (m_Rigidbody.position + m_Movement * m_Animator.deltaPosition.magnitude);//posizione iniziale + movimento * cambiamento di posizione
        m_Rigidbody.MoveRotation (m_Rotation); //imposto rotazione



    }
}
