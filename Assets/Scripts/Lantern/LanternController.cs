using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class LanternController : MonoBehaviour
{
    private LanternState currentState;
    public LanternOffState offState;
    public LanternIdleState idleState;
    private LanternMoveState moveState;
    
    private LanternTurningOnState turningOnState;
    private LanternTurningOffState turningOffState;

    [SerializeField] public Light2D lanternLight; 
    [SerializeField] private Rigidbody2D playerRigidbody;
    
    [SerializeField] private float moveThreshold = 2f;

    private void Awake()
    {
        // Initialisation des états
        idleState = new LanternIdleState(this);
        moveState = new LanternMoveState(this);
        offState = new LanternOffState(this);

        turningOnState = new LanternTurningOnState(this);
        turningOffState = new LanternTurningOffState(this);

        // Définir l'état initial
        currentState = offState;
    }

    private void Update()
    {
        if (currentState is LanternTurningOnState)
        {
            currentState.UpdateState(); 
            return;
        }

        // Calculer la vitesse actuelle du joueur
        float playerSpeed = playerRigidbody.linearVelocity.magnitude;

        // Transition entre les états basées sur la vitesse 
        if (currentState != offState && currentState != turningOffState)
        {
            if (playerSpeed < moveThreshold)
            {
                if (currentState != idleState)
                    ChangeState(idleState);
            }
            else if (playerSpeed >= moveThreshold)
            {
                if (currentState != moveState)
                    ChangeState(moveState);
            }
        }
        
        currentState?.UpdateState();
    }

    public void TurnOnLantern()
    {
        if (currentState == offState || currentState is LanternTurningOffState)
        {
            ChangeState(turningOnState);
        }
    }

    public void TurnOffLantern()
    {
        if (currentState != offState && !(currentState is LanternTurningOffState))
        {
            ChangeState(turningOffState);
        }
    }


    public void ChangeState(LanternState newState)
    {
        currentState.ExitState(); 
        currentState = newState;
        currentState.EnterState(); 
    }


    public void SetLightParameters(float intensity, float outerRadius)
    {
        if (lanternLight != null)
        {
            lanternLight.intensity = intensity;
            lanternLight.pointLightOuterRadius = outerRadius;
        }
    }
}

