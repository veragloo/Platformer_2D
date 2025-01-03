using UnityEngine;
using TarodevController;

public class LedgeDetection : MonoBehaviour
{
    [SerializeField] private float radius;
    [SerializeField] private LayerMask WhatIsGround;
    [SerializeField] private PlayerController player; 
    [SerializeField] private bool isLeftDetector; 
    [SerializeField] private float raycastDistance = 1f; 

    private bool canDetected;

    private void Update()
    {
        Vector2 raycastOrigin = (Vector2)player.transform.position;
        RaycastHit2D raycastHit = Physics2D.Raycast(raycastOrigin, Vector2.up, raycastDistance, WhatIsGround);

        // Logique principale
        if (raycastHit.collider != null)
        {
            // Si le raycast touche quelque chose (mur ou plafond)
            canDetected = false;
        }
        else
        {
            // Si le raycast ne touche rien, c'est potentiellement un ledge
            canDetected = true;
        }

        // Si on peut détecter, effectuer la vérification du ledge
        if (canDetected)
        {
            bool ledgeDetectedThisFrame = Physics2D.OverlapCircle(transform.position, radius, WhatIsGround);

            if (isLeftDetector)
            {
                player.ledgeDetectedLeft = ledgeDetectedThisFrame;
            }
            else
            {
                player.ledgeDetected = ledgeDetectedThisFrame;
            }

            // Si aucun ledge n'est détecté, désactive `canDetected`
            if (!ledgeDetectedThisFrame)
            {
                canDetected = false;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Désactive la détection lorsqu'un objet de type "Ground" entre en collision
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            canDetected = false; 
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // Réactive la détection lorsqu'un objet de type "Ground" sort de la collision
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            canDetected = true; 
        }
    }

    private void OnDrawGizmos()
    {
        // Affichage du rayon vertical sous forme de Gizmo
        Gizmos.color = Color.red;
        Gizmos.DrawRay((Vector2)player.transform.position, Vector2.up * raycastDistance);

        // Affichage du rayon de détection de Ledge
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
