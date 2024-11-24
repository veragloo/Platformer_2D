using UnityEngine;
using TarodevController;

public class LedgeDetection : MonoBehaviour
{
    [SerializeField] private float radius;
    [SerializeField] private LayerMask WhatIsGround;
    [SerializeField] private PlayerController player; 
    [SerializeField] private bool isLeftDetector; 

    private bool canDetected;

    private void Update()
    {
        if (canDetected)
        {
            if (isLeftDetector)
            {
                // Met à jour la variable pour la détection de gauche
                player.ledgeDetectedLeft = Physics2D.OverlapCircle(transform.position, radius, WhatIsGround);
            }
            else
            {
                // Met à jour la variable pour la détection de droite
                player.ledgeDetected = Physics2D.OverlapCircle(transform.position, radius, WhatIsGround);
            }

            // Réinitialise si aucune détection
            if (!player.ledgeDetected && !player.ledgeDetectedLeft)
            {
                canDetected = false; // Désactive la détection
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            canDetected = false;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            canDetected = true;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}

