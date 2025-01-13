using UnityEngine;

public class LanternTurningOffState : LanternState
{
    private float transitionSpeed = 2f;
    private bool interrupted = false;

    public LanternTurningOffState(LanternController lantern) : base(lantern) { }

    public override void EnterState()
    {
        interrupted = false; 
    }

    public override void UpdateState()
    {
        if (interrupted)
        {
            return; 
        }

        // Réduire progressivement les paramètres de la lumière
        lantern.lanternLight.intensity = Mathf.MoveTowards(
            lantern.lanternLight.intensity, 
            0f, 
            transitionSpeed * Time.deltaTime
        );

        lantern.lanternLight.pointLightOuterRadius = Mathf.MoveTowards(
            lantern.lanternLight.pointLightOuterRadius, 
            0f, 
            transitionSpeed * Time.deltaTime
        );

        // Si la lumière est complètement éteinte, passer à l'état Off
        if (Mathf.Approximately(lantern.lanternLight.intensity, 0f) &&
            Mathf.Approximately(lantern.lanternLight.pointLightOuterRadius, 0f))
        {
            lantern.ChangeState(lantern.offState);
        }
    }

    public override void ExitState()
    {
        interrupted = true; 
    }
}
