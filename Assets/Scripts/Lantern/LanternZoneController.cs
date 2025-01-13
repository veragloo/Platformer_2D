using UnityEngine;

public class LanternZoneController : MonoBehaviour
{
    [SerializeField] private LanternController lanternController; 
    // private bool playerInZone = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // playerInZone = true;
            lanternController.TurnOnLantern();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // playerInZone = false;
            lanternController.TurnOffLantern();
        }
    }
}
