using System;
// Separable sine sums: O(N) trigonometry for an NxN surface. The analytic
// sampler used by buoyancy and flooding shares precisely these coefficients.
public sealed class NavalWaveField
{
    private static readonly double[] X={.47,-.16,1.9},Z={.21,.83,1.3},Speed={1.35,1.8,2.6},Weight={.62,.28,.10};
    private readonly int count;
    private readonly double[] sinX,cosX,sinZ,cosZ;
    private float amplitude;
    public NavalWaveField(int size)
    {count=size;sinX=new double[size*3];cosX=new double[size*3];sinZ=new double[size*3];cosZ=new double[size*3];}
    public static float Sample(float x,float z,float amplitude,float time)
    {
        double sum=0;for(int wave=0;wave<3;wave++)sum+=Weight[wave]*Math.Sin(x*X[wave]+z*Z[wave]-time*Speed[wave]);
        return (float)(amplitude*sum);
    }
    public void Prepare(float firstX,float firstZ,float spacing,float height,float time)
    {
        amplitude=height;
        for(int i=0;i<count;i++)for(int wave=0;wave<3;wave++)
        {
            double a=(firstX+i*spacing)*X[wave],b=(firstZ+i*spacing)*Z[wave]-time*Speed[wave];int index=i*3+wave;
            sinX[index]=Math.Sin(a);cosX[index]=Math.Cos(a);sinZ[index]=Math.Sin(b);cosZ[index]=Math.Cos(b);
        }
    }
    public float At(int x,int z)
    {
        double sum=0;int a=x*3,b=z*3;
        for(int wave=0;wave<3;wave++)sum+=Weight[wave]*(sinX[a+wave]*cosZ[b+wave]+cosX[a+wave]*sinZ[b+wave]);
        return (float)(amplitude*sum);
    }
}
