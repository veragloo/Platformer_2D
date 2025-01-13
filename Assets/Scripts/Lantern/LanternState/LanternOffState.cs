using UnityEngine;

public class LanternOffState : LanternState
{
    public LanternOffState(LanternController lantern) : base(lantern) { }

    public override void EnterState()
    {
        lantern.SetLightParameters(0f, 0f);
    }

    public override void UpdateState()
    {
        // Rien à faire ici, car la lumière reste éteinte
    }

    public override void ExitState()
    {
        // peut être utile plus tard
    }
}





