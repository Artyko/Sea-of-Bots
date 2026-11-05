using System;
public static class NavalHullAimZones
{
    public static bool Facing(float viewX,float viewZ,float x,float z,float halfWidth)
    {
        // End-on views target the exposed transom/bow; beam views target the facing side.
        return Math.Abs(viewZ)>Math.Abs(viewX)?z*(viewZ<0?-1:1)>5.25f:
            x*(viewX<0?-1:1)>=halfWidth-.35f;
    }
    // Macro plate normal, not the tiny end face of a voxel in a side belt.
    // Side slope follows the same outline used to build the hydraulic shell.
    public static float Incidence(float vx,float vy,float vz,float x,float y,float z,float widthSlope,bool endPlate,bool deck)
    {
        float nx=0,ny=0,nz=0;
        if(deck)ny=1;
        else if(endPlate)nz=z<0?-1:1;
        else {nx=x<0?-1:1;nz=-widthSlope;}
        float dx=vx-x,dy=vy-y,dz=vz-z;
        float length=(float)Math.Sqrt((dx*dx+dy*dy+dz*dz)*(nx*nx+ny*ny+nz*nz));
        return length<.0001f?0:Math.Max(0,(dx*nx+dy*ny+dz*nz)/length);
    }
}
