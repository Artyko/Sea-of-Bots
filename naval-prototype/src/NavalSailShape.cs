using System;
public struct NavalSailPose {public float X,Z,SlopeX,SlopeY;}
public sealed class NavalSailShape
{
    private readonly float[] envelope,slopeX,slopeY,sinPhase,cosPhase,sinRow,cosRow;
    private float gust,phaseSin,phaseCos,rowSin,rowCos;
    public NavalSailShape(float[] x,float[] y,float minX=-2.625f,float width=5.25f,float minY=3.825f,float height=3.75f)
    {
        int n=x.Length;envelope=new float[n];slopeX=new float[n];slopeY=new float[n];sinPhase=new float[n];cosPhase=new float[n];sinRow=new float[n];cosRow=new float[n];
        for(int i=0;i<n;i++)
        {
            double u=Math.Max(0,Math.Min(1,(x[i]-minX)/width)),v=Math.Max(0,Math.Min(1,(y[i]-minY)/height));
            double su=Math.Sin(u*Math.PI),sv=Math.Sin(v*Math.PI);
            envelope[i]=(float)(su*sv);slopeX[i]=(float)(Math.Cos(u*Math.PI)*sv*Math.PI/width);slopeY[i]=(float)(su*Math.Cos(v*Math.PI)*Math.PI/height);
            sinPhase[i]=(float)Math.Sin(u*5+v*3);cosPhase[i]=(float)Math.Cos(u*5+v*3);
            sinRow[i]=(float)Math.Sin(v*4);cosRow[i]=(float)Math.Cos(v*4);
        }
    }
    public void Prepare(float time)
    {
        gust=.36f+(float)Math.Sin(time*.85f)*.12f;
        phaseSin=(float)Math.Sin(time*2.2f);phaseCos=(float)Math.Cos(time*2.2f);
        rowSin=(float)Math.Sin(time*1.3f);rowCos=(float)Math.Cos(time*1.3f);
    }
    public NavalSailPose At(int i)
    {
        return new NavalSailPose{X=envelope[i]*(rowSin*cosRow[i]+rowCos*sinRow[i])*.04f,
            Z=.02f+envelope[i]*(gust+(phaseSin*cosPhase[i]+phaseCos*sinPhase[i])*.10f),SlopeX=slopeX[i]*gust,SlopeY=slopeY[i]*gust};
    }
}
