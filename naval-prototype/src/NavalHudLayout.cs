using System;
public static class NavalHudLayout
{
    public static float Scale(float width,float height){return Math.Min(1f,Math.Min(height/820f,width/1000f));}
    public static float ContextWidth(float width,float height,float requested)
    {return Math.Max(100f,Math.Min(requested,width-364f*Scale(width,height)-24f));}
    public static float ContextX(float width,float height,float requested)
    {
        float size=ContextWidth(width,height,requested);
        return Math.Max(364f*Scale(width,height)+12f,(width-size)*.5f);
    }
}
