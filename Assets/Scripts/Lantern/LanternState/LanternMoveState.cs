using UnityEngine;

public class LanternMoveState : LanternState
{
    private float baseIntensity = 0.45f; 
    private float baseOuterRadius = 4.8f; 
    private float flickerAmount = 0.3f; 
    private float flickerSpeed = 12f; 
    private float flickerOffset;

    public LanternMoveState(LanternController lantern) : base(lantern)
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
        // peut être utile plus tard
    }
}
