using System;
// Normalized ship coordinates; both port and starboard leaves fold into their room.
public static class NavalDoorMath
{
    public static float OpenAngle(float wallX){return wallX<0f?170f:-170f;}
    public static bool Detour(float x,float z,float goalX,float goalZ,float wallX,float doorZ,out float targetX)
    {
        targetX=x;
        // Transverse passage stays centered in the opening; longitudinal travel clears the leaf.
        if(Math.Abs(goalZ-z)<.08f||Math.Abs(z-doorZ-.95f)>.9f||Math.Abs(x-wallX)>.9f)return false;
        if((x-wallX)*(goalX-wallX)<=0f)return false;
        float side=x<wallX?-1f:1f;
        targetX=wallX+side*.8f;
        return true;
    }
}
