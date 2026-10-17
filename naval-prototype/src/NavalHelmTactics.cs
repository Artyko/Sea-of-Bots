using System;
public static class NavalHelmTactics
{
    // Keep a scale-aware margin around the hulls while conducting broadside combat.
    // Boarding and repair retreat override this course explicitly in the caller.
    public static float Approach(float distance,float ownScale,float otherScale)
    {
        float margin=(float)Math.Sqrt(NavalShipContactMath.HalfWidth*NavalShipContactMath.HalfWidth+NavalShipContactMath.HalfLength*NavalShipContactMath.HalfLength)*(ownScale+otherScale)*.5f+8f;
        float weight=NavalGunneryMath.ApproachWeight(distance);
        if(distance<margin)weight=Math.Min(weight,-.5f-(margin-distance)/Math.Max(1f,margin));
        return Math.Max(-1.5f,weight);
    }
}
