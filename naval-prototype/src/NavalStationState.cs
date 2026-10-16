public class NavalStationState
{
    public int Active = -1;
    public void PressInteract(int nearest, bool disabled,int gunCount=2)
    {
        if (Active >= 0 || disabled) { Active = -1; return; }
        Active = nearest >= 0 && nearest <= gunCount ? nearest : -1;
    }
    public void Exit() { Active = -1; }
}
