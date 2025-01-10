using UnityEngine;

public class LanternWeakState : LanternState
{
    private float weakIntensity = 0.45f; 
    private float weakOuterRadius = 4.5f; 
    private float flickerAmount = 0.2f; 
    private float flickerSpeed = 12f; 
    private float flickerOffset;

    public LanternWeakState(LanternController lantern) : base(lantern)
    {
        flickerOffset = Random.Range(0f, 100f); // Décalage pour le bruit
    }

    public override void EnterState()
    {
        // Initialiser les paramètres 
        lantern.SetLightParameters(weakIntensity, weakOuterRadius);
    }

    public override void UpdateState()
    {
        // Logique du scintillement 
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, flickerOffset);
        float flickerValue = (noise - 0.5f) * flickerAmount;

        lantern.SetLightParameters(
            weakIntensity + flickerValue,
            weakOuterRadius + flickerValue * 0.5f
        );
    }

    public override void ExitState()
    {
        // Rien à faire pour l'instant, mais peut être utile plus tard
    }
}
