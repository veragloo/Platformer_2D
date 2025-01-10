using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LanternController : MonoBehaviour
{
    private LanternState currentState; 
    private LanternIdleState idleState;
    private LanternWeakState weakState;

    [SerializeField] private Light2D lanternLight;

    private void Awake()
    {
        // Initialisation des états
        idleState = new LanternIdleState(this);
        weakState = new LanternWeakState(this);

        // Définir l'état initial
        currentState = idleState;
    }

    private void Start()
    {
        // Si la lumière n'est pas assignée dans l'inspecteur
        if (lanternLight == null)
        {
            lanternLight = GetComponent<Light2D>();
        }

        // Entrer dans l'état initial
        currentState.EnterState();
    }

    private void Update()
    {
        // Appeler l'état actuel
        currentState.UpdateState();

        // Tests : Changer d'état
        if (Input.GetKeyDown(KeyCode.Alpha1)) 
        {
            ChangeState(idleState);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2)) 
        {
            ChangeState(weakState);
        }
    }

    // Méthode pour changer d'état
    public void ChangeState(LanternState newState)
    {
        currentState.ExitState(); 
        currentState = newState; 
        currentState.EnterState(); 
    }

    // Méthode pour définir les paramètres
    public void SetLightParameters(float intensity, float outerRadius)
    {
        if (lanternLight != null)
        {
            lanternLight.intensity = intensity;
            lanternLight.pointLightOuterRadius = outerRadius;
        }
    }
}
