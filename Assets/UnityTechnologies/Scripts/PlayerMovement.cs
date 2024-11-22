using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float movementSpeed = 3f; // Velocità di movimento
    public float turnSpeed = 20f;   // Velocità di rotazione
    Animator m_Animator;
    Rigidbody m_Rigidbody;
    Vector3 m_Movement;
    Quaternion m_Rotation = Quaternion.identity;

    void Start()
    {
        m_Animator = GetComponent<Animator>();
        m_Rigidbody = GetComponent<Rigidbody>();
    }

    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        m_Movement.Set(-horizontal, 0f, -vertical);
        m_Movement.Normalize();

        bool isWalking = horizontal != 0 || vertical != 0;
        m_Animator.SetBool("isWalking", isWalking);

        Vector3 desiredForward = Vector3.RotateTowards(transform.forward, m_Movement, turnSpeed * Time.deltaTime, 0f);
        m_Rotation = Quaternion.LookRotation(desiredForward);
    }

    void OnAnimatorMove()
    {
        if (m_Animator.GetBool("isWalking"))
        {
            // Controllo collisioni con un Raycast
            if (!Physics.Raycast(transform.position, m_Movement, 0.5f))
            {
                // Applica movimento e rotazione
                m_Rigidbody.MovePosition(m_Rigidbody.position + m_Movement * movementSpeed * Time.deltaTime);
                m_Rigidbody.MoveRotation(m_Rotation);
            }
            else
            {
                Debug.Log("Obstacle detected!");
            }
        }
    }
}
