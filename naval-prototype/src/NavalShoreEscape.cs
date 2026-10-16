// A recovery step must not deepen any terrain contact or enter a new one.
public struct NavalShoreEscape
{
    private bool worse,improved,inside;
    public void Observe(float before,float after)
    {
        worse|=after>before+.00001f;
        improved|=before-after>.00001f;
        inside|=after>0f;
    }
    public bool Allowed {get{return !worse&&(!inside||improved);}}
}
