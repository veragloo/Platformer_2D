using UnityEngine;

public class LanternIdleState : LanternState
{
    private float baseIntensity = 0.5f;
    private float baseOuterRadius = 5f;
    private float flickerAmount = 0.2f;
    private float flickerSpeed = 8f;
    private float flickerOffset;

    public LanternIdleState(LanternController lantern) : base(lantern)
    {
        flickerOffset = Random.Range(0f, 100f); // Décalage pour le bruit
    }

    public override void EnterState()
    {
        // Initialiser les paramètres 
        lantern.SetLightParameters(baseIntensity, baseOuterRadius);
    }

    public override void UpdateState()
    {
        // Logique du scintillement
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, flickerOffset);
        float flickerValue = (noise - 0.5f) * flickerAmount;

        lantern.SetLightParameters(
            baseIntensity + flickerValue,
            baseOuterRadius + flickerValue * 0.5f
        );
    }

    public override void ExitState()
    {
        // Rien à faire pour l'instant, mais peut être utile plus tard
    }
}
