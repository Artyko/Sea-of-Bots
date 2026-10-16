using System;
public static class NavalSubmersionMath
{
    public static float VisualDepth(float cameraDepth,float headDepth)
    {return Math.Max(0f,Math.Max(cameraDepth,headDepth));}
}
