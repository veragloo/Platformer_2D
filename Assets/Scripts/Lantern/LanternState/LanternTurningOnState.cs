using UnityEngine;

public class LanternTurningOnState : LanternState
{
    private float targetIntensity = 0.5f; 
    private float targetOuterRadius = 5f; 
    private float transitionSpeed = 8f; 
    private bool interrupted = false;

    private float flickerAmount = 1f;
    private float flickerSpeed = 30f;
    private float flickerOffset;

    private float timer = 0f;

    public LanternTurningOnState(LanternController lantern) : base(lantern) { }

    public override void EnterState()
    {
        flickerOffset = Random.Range(0f, 100f); 
        lantern.SetLightParameters(0f, 0f);
        interrupted = false;
        timer = 0f;
    }

    public override void UpdateState()
    {
        if (interrupted)
        {
            return; 
        }

        // Logique du bruit pendant l'allumage
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, flickerOffset);
        float flickerValue = (noise - 0.5f) * flickerAmount;

        // Interpolation de la lumière avec ajout de bruit
        lantern.lanternLight.intensity = Mathf.MoveTowards(
            lantern.lanternLight.intensity, 
            targetIntensity + flickerValue,  
            transitionSpeed * Time.deltaTime
        );

        lantern.lanternLight.pointLightOuterRadius = Mathf.MoveTowards(
            lantern.lanternLight.pointLightOuterRadius, 
            targetOuterRadius + flickerValue * 0.5f,  
            transitionSpeed * Time.deltaTime
        );

        // Mise à jour du timer
        timer += Time.deltaTime;

        // Si le timer atteint la durée on passe à l'état idle
        if (timer >= 0.5f)  
        {
            lantern.ChangeState(lantern.idleState);
        }
    }

    public override void ExitState()
    {
        interrupted = true; 
    }
}
