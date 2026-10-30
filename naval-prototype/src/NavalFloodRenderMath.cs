using System;

// Conservative visual clipping for rectangular hydraulic columns inside a
// tapered hull.  It affects only the mesh; hydraulic mass remains untouched.
public static class NavalFloodRenderMath
{
    public static bool ClipCell(float centreX,float width,float centreZ,float length,NavalShipDefinition ship,out float visibleCentre,out float visibleWidth)
    {
        float half=Math.Min(ship.HullHalfWidth(centreZ),Math.Min(ship.HullHalfWidth(centreZ-length*.5f),ship.HullHalfWidth(centreZ+length*.5f)));
        half=Math.Max(0f,half-.08f/ship.Scale);
        float left=Math.Max(centreX-width*.5f,-half),right=Math.Min(centreX+width*.5f,half);
        visibleWidth=Math.Max(0f,right-left);visibleCentre=(left+right)*.5f;
        return visibleWidth>.0001f;
    }
}
