public class NavalStationState
{
    public int Active = -1;
    public void PressInteract(int nearest, bool disabled)
    {
        if (Active >= 0 || disabled) { Active = -1; return; }
        Active = nearest >= 0 && nearest <= 2 ? nearest : -1;
    }
    public void Exit() { Active = -1; }
}
