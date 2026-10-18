using System;
using System.Diagnostics;
// Frozen v110 solver for numerical parity and an isolated CPU comparison.
public sealed class ReferenceHydrostaticSection
{
    private readonly double[] x=new double[4],y=new double[4],cx=new double[8],cy=new double[8];
    public double Area,CenterX,CenterY,Waterline;
    private void Clip(double level,double slope)
    {
        int n=0;
        for(int i=0;i<4;i++)
        {
            int j=(i+1)%4;double da=level+slope*x[i]-y[i],db=level+slope*x[j]-y[j];
            bool a=da>=0,b=db>=0;
            if(a){cx[n]=x[i];cy[n++]=y[i];}
            if(a!=b){double t=da/(da-db);cx[n]=x[i]+(x[j]-x[i])*t;cy[n++]=y[i]+(y[j]-y[i])*t;}
        }
        double twice=0,mx=0,my=0;
        for(int i=0;i<n;i++){int j=(i+1)%n;double q=cx[i]*cy[j]-cx[j]*cy[i];twice+=q;mx+=(cx[i]+cx[j])*q;my+=(cy[i]+cy[j])*q;}
        Area=Math.Abs(twice)*.5;
        CenterX=Math.Abs(twice)>1e-10?mx/(3*twice):0;CenterY=Math.Abs(twice)>1e-10?my/(3*twice):0;
    }
    public double FractionBelow(double width,double height,double angleDegrees,double waterline)
    {
        double phi=angleDegrees*Math.PI/180,c=Math.Cos(phi),s=Math.Sin(phi);
        for(int i=0;i<4;i++){double px=(i==0||i==3)?-width*.5:width*.5,py=i<2?0:height;x[i]=px*c-py*s;y[i]=px*s+py*c;}
        Clip(waterline,0);return Math.Max(0,Math.Min(1,Area/(width*height)));
    }
    public void Solve(double width,double bottom,double top,double angleDegrees,double requiredArea,double waveDegrees=0)
    {
        double phi=angleDegrees*Math.PI/180,c=Math.Cos(phi),s=Math.Sin(phi),slope=Math.Tan(waveDegrees*Math.PI/180);
        double low=double.PositiveInfinity,high=double.NegativeInfinity;
        for(int i=0;i<4;i++)
        {
            double px=(i==0||i==3)?-width*.5:width*.5,py=i<2?bottom:top;
            x[i]=px*c-py*s;y[i]=px*s+py*c;double h=y[i]-slope*x[i];low=Math.Min(low,h);high=Math.Max(high,h);
        }
        requiredArea=Math.Max(0,Math.Min(width*(top-bottom),requiredArea));
        for(int i=0;i<18;i++){double mid=(low+high)*.5;Clip(mid,slope);if(Area<requiredArea)low=mid;else high=mid;}
        Waterline=(low+high)*.5;Clip(Waterline,slope);
    }
}
class HydrostaticOptimizationChecks
{
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
 static double Consume;
 static void Main()
 {
  var random=new Random(111);var old=new ReferenceHydrostaticSection();var current=new NavalHydrostaticSection();
  for(int i=0;i<12000;i++)
  {
   double width=4+random.NextDouble()*20,bottom=-1-random.NextDouble()*7,top=.2+random.NextDouble()*6;
   double angle=-180+random.NextDouble()*360,wave=-15+random.NextDouble()*30,area=width*(top-bottom)*(.01+.98*random.NextDouble());
   old.Solve(width,bottom,top,angle,area,wave);current.Solve(width,bottom,top,angle,area,wave);
   Check(Math.Abs(old.Area-current.Area)<width*(top-bottom)*.00002,"Area parity over arbitrary immersion and roll");
   Check(Math.Abs(old.Waterline-current.Waterline)<.0002,"Waterline parity");
   Check(Math.Abs(old.CenterX-current.CenterX)<.002&&Math.Abs(old.CenterY-current.CenterY)<.002,"Buoyancy centroid parity");
  }
  foreach(double angle in new[]{0d,90,180,-90,45})foreach(double fill in new[]{0d,1d})
  {current.Solve(12,-4.45,3.55,angle,96*fill);Check(Math.Abs(current.Area-96*fill)<.002,"Empty/full and vertical limits");}
  for(int i=0;i<1000;i++){old.Solve(12,-4.45,3.55,8,60);current.Solve(12,-4.45,3.55,8,60);}
  var watch=Stopwatch.StartNew();for(int i=0;i<100000;i++){old.Solve(12,-4.45,3.55,8,60);Consume+=old.CenterX;}watch.Stop();double oldMs=watch.Elapsed.TotalMilliseconds;
  watch.Restart();for(int i=0;i<100000;i++){current.Solve(12,-4.45,3.55,8,60);Consume+=current.CenterX;}watch.Stop();
  Console.WriteLine("PASS: 12000 reference parity cases, all roll quadrants/waves/immersions and empty/full limits. 100000 normal-section solves: old "+oldMs.ToString("F1")+" -> new "+watch.Elapsed.TotalMilliseconds.ToString("F1")+" ms (core only, not Unity FPS).");
 }
}
