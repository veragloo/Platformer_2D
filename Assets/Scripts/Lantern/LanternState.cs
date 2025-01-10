using UnityEngine;

public abstract class LanternState
{
    protected LanternController lantern; 

    public LanternState(LanternController lantern)
    {
        this.lantern = lantern;
    }

    public abstract void EnterState();
    public abstract void UpdateState(); 
    public abstract void ExitState(); 
}
