using System;
public struct NavalSurfaceNormal {public float X,Y,Z;}
// Height-gradient normals, shared by the unchanged grid and its clipped seam vertices.
public sealed class NavalOceanNormals
{
    private readonly int count;
    private readonly NavalSurfaceNormal[] normals;
    public NavalOceanNormals(int size){count=size;normals=new NavalSurfaceNormal[size*size];}
    private static NavalSurfaceNormal Unit(float x,float y,float z)
    {float length=(float)Math.Sqrt(x*x+y*y+z*z);return new NavalSurfaceNormal{X=x/length,Y=y/length,Z=z/length};}
    public void Build(float[] heights,float spacing)
    {
        for(int z=0;z<count;z++)for(int x=0;x<count;x++)
        {
            int left=Math.Max(0,x-1),right=Math.Min(count-1,x+1),back=Math.Max(0,z-1),front=Math.Min(count-1,z+1);
            normals[z*count+x]=Unit(-(heights[z*count+right]-heights[z*count+left])/((right-left)*spacing),1,
                -(heights[front*count+x]-heights[back*count+x])/((front-back)*spacing));
        }
    }
    public NavalSurfaceNormal At(int index){return normals[index];}
    public NavalSurfaceNormal Sample(float x,float z)
    {
        x=Math.Max(0,Math.Min(count-1,x));z=Math.Max(0,Math.Min(count-1,z));
        int left=(int)x,back=(int)z,right=Math.Min(count-1,left+1),front=Math.Min(count-1,back+1);float u=x-left,v=z-back;
        NavalSurfaceNormal a=normals[back*count+left],b=normals[back*count+right],c=normals[front*count+left],d=normals[front*count+right];
        float aa=(1-u)*(1-v),bb=u*(1-v),cc=(1-u)*v,dd=u*v;
        return Unit(a.X*aa+b.X*bb+c.X*cc+d.X*dd,a.Y*aa+b.Y*bb+c.Y*cc+d.Y*dd,a.Z*aa+b.Z*bb+c.Z*cc+d.Z*dd);
    }
}
