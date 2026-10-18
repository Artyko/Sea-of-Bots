using System;
using System.Diagnostics;
class RuntimeOptimizationChecks
{
 static void Check(bool ok,string label){if(!ok)throw new Exception(label);}
 static float F(Random r,float span){return (float)(r.NextDouble()*span*2-span);}
 static float sink;
 static void Main()
 {
  Random random=new Random(100);
  for(int i=0;i<5000;i++)
  {
   float origin=i%10==0?1000000:0;
   float x=origin+F(random,60),z=origin+F(random,60),yaw=F(random,360);
   float nx=x+F(random,8),nz=z+F(random,8),turn=i%2==0?0:F(random,10);
   float bx=origin+F(random,60),bz=origin+F(random,60),by=F(random,360),a=i%3==0?1:1.5f,b=i%5==0?1:1.5f;
   float expected=LegacyContactMath.SafeFraction(x,z,yaw,nx,nz,turn,bx,bz,by,a,b);
   float actual=NavalShipContactMath.SafeFraction(x,z,yaw,nx,nz,turn,bx,bz,by,a,b);
   Check(Math.Abs(expected-actual)<.000001,"Swept contact parity, including rotation, scales and large coordinates");
   Check(LegacyContactMath.Overlap(x,z,yaw,bx,bz,by,a,b)==NavalShipContactMath.Overlap(x,z,yaw,bx,bz,by,a,b),"Exact OBB parity");
  }
  NavalSwimWorkspace workspace=new NavalSwimWorkspace();
  for(int i=0;i<5000;i++)
  {
   NavalSwimPoint start=new NavalSwimPoint(F(random,12),F(random,15)),goal=new NavalSwimPoint(i%2==0?-3.7f:3.7f,F(random,6));
   NavalSwimPoint before,after,reused;
   float expected=LegacySwimPath.Route(start,goal,out before),actual=NavalSwimPath.Route(start,goal,out after);
   Check(expected==float.MaxValue?actual==expected:Math.Abs(actual-expected)<.00001,"Swim length parity");
   Check(Math.Abs(before.X-after.X)<.00001&&Math.Abs(before.Z-after.Z)<.00001,"Swim first waypoint parity");
   workspace.Route(new NavalSwimPoint(0,0),new NavalSwimPoint(3.7f,3),out reused);
   Check(Math.Abs(workspace.Route(start,goal,out reused)-actual)<.00001,"Reused workspace independent after interior recursion");
  }
  Stopwatch watch=new Stopwatch();
  for(int i=0;i<1000;i++){sink+=LegacyContactMath.SafeFraction(0,0,0,.1f,0,.2f,250,0,180);sink+=NavalShipContactMath.SafeFraction(0,0,0,.1f,0,.2f,250,0,180);}
  watch.Start();for(int i=0;i<100000;i++)sink+=LegacyContactMath.SafeFraction(0,0,0,.1f,0,.2f,250,0,180);watch.Stop();double oldContact=watch.Elapsed.TotalMilliseconds;
  watch.Restart();for(int i=0;i<100000;i++)sink+=NavalShipContactMath.SafeFraction(0,0,0,.1f,0,.2f,250,0,180);watch.Stop();double newContact=watch.Elapsed.TotalMilliseconds;
  NavalSwimPoint next;NavalSwimPoint from=new NavalSwimPoint(-5,0),to=new NavalSwimPoint(3.7f,3.8f);
  NavalSwimPath.Route(from,to,out next);LegacySwimPath.Route(from,to,out next);
  GC.Collect();int gen=GC.CollectionCount(0);watch.Restart();for(int i=0;i<50000;i++)sink+=LegacySwimPath.Route(from,to,out next);watch.Stop();double oldSwim=watch.Elapsed.TotalMilliseconds;int oldCollections=GC.CollectionCount(0)-gen;
  GC.Collect();gen=GC.CollectionCount(0);watch.Restart();for(int i=0;i<50000;i++)sink+=NavalSwimPath.Route(from,to,out next);watch.Stop();double newSwim=watch.Elapsed.TotalMilliseconds;int newCollections=GC.CollectionCount(0)-gen;
  Console.WriteLine("PASS: 5000 swept contact/OBB cases and 5000 swim/workspace cases match previous implementation.");
  Console.WriteLine("CORE MICROBENCH ONLY, not game FPS: 100000 distant contacts old/new ms: "+oldContact.ToString("F1")+" / "+newContact.ToString("F1"));
  Console.WriteLine("50000 detours old/new ms: "+oldSwim.ToString("F1")+" / "+newSwim.ToString("F1")+"; Gen0 old/new: "+oldCollections+" / "+newCollections+"; checksum="+sink);
 }
}

// Frozen pre-v100 references for behavioural parity.

public static class LegacyContactMath
{
    public const float HalfWidth=6.45f,HalfLength=12.75f;
    public static float SafeFraction(float x,float z,float yaw,float nextX,float nextZ,float turn,float otherX,float otherZ,float otherYaw,float scaleA=1f,float scaleB=1f)
    {
        double dx=nextX-x,dz=nextZ-z;
        int steps=Math.Max(1,(int)Math.Ceiling(Math.Max(Math.Sqrt(dx*dx+dz*dz)/0.25,Math.Abs(turn)/0.5)));
        float safe=0f;
        for(int i=1;i<=steps;i++)
        {
            float t=(float)i/steps;
            if(Overlap(x+(nextX-x)*t,z+(nextZ-z)*t,yaw+turn*t,otherX,otherZ,otherYaw,scaleA,scaleB))
            {
                float low=safe,high=t;
                for(int j=0;j<10;j++)
                {
                    float middle=(low+high)*0.5f;
                    if(Overlap(x+(nextX-x)*middle,z+(nextZ-z)*middle,yaw+turn*middle,otherX,otherZ,otherYaw,scaleA,scaleB))high=middle;
                    else low=middle;
                }
                return low;
            }
            safe=t;
        }
        return 1f;
    }
    public static bool Overlap(float ax,float az,float yawA,float bx,float bz,float yawB,float scaleA=1f,float scaleB=1f)
    {
        double a=yawA*Math.PI/180.0,b=yawB*Math.PI/180.0;
        double arx=Math.Cos(a),arz=-Math.Sin(a),afx=Math.Sin(a),afz=Math.Cos(a);
        double brx=Math.Cos(b),brz=-Math.Sin(b),bfx=Math.Sin(b),bfz=Math.Cos(b);
        double dx=bx-ax,dz=bz-az;
        return Axis(dx,dz,arx,arz,arx,arz,afx,afz,brx,brz,bfx,bfz,scaleA,scaleB)&&
            Axis(dx,dz,afx,afz,arx,arz,afx,afz,brx,brz,bfx,bfz,scaleA,scaleB)&&
            Axis(dx,dz,brx,brz,arx,arz,afx,afz,brx,brz,bfx,bfz,scaleA,scaleB)&&
            Axis(dx,dz,bfx,bfz,arx,arz,afx,afz,brx,brz,bfx,bfz,scaleA,scaleB);
    }
    private static bool Axis(double dx,double dz,double x,double z,double arx,double arz,double afx,double afz,
        double brx,double brz,double bfx,double bfz,float scaleA,float scaleB)
    {
        double radius=HalfWidth*(scaleA*Math.Abs(x*arx+z*arz)+scaleB*Math.Abs(x*brx+z*brz))+
            HalfLength*(scaleA*Math.Abs(x*afx+z*afz)+scaleB*Math.Abs(x*bfx+z*bfz));
        return Math.Abs(dx*x+dz*z)<radius;
    }
}


public static class LegacySwimPath
{
    private static bool Clip(float p,float d,float min,float max,ref float first,ref float last)
    {
        if(Math.Abs(d)<0.00001f)return p>min&&p<max;
        float a=(min-p)/d,b=(max-p)/d;if(a>b){float swap=a;a=b;b=swap;}
        first=Math.Max(first,a);last=Math.Min(last,b);return first<last;
    }
    public static bool Clear(NavalSwimPoint a,NavalSwimPoint b)
    {
        float first=0f,last=1f;
        return !(Clip(a.X,b.X-a.X,-3.45f,3.45f,ref first,ref last)&&Clip(a.Z,b.Z-a.Z,-6.5f,6.5f,ref first,ref last));
    }
    public static float Distance(NavalSwimPoint a,NavalSwimPoint b)
    {float dx=a.X-b.X,dz=a.Z-b.Z;return (float)Math.Sqrt(dx*dx+dz*dz);}
    public static float Route(NavalSwimPoint start,NavalSwimPoint goal,out NavalSwimPoint next)
    {
        if(Math.Abs(start.X)<3.45f&&Math.Abs(start.Z)<6.5f)
        {
            NavalSwimPoint exit=start;
            if(3.45f-Math.Abs(start.X)<6.5f-Math.Abs(start.Z))exit.X=start.X<0f?-3.7f:3.7f;
            else exit.Z=start.Z<0f?-6.8f:6.8f;
            NavalSwimPoint unused;float remaining=Route(exit,goal,out unused);
            next=exit;return remaining+Distance(start,exit);
        }
        NavalSwimPoint[] points={start,new NavalSwimPoint(-3.7f,-6.8f),new NavalSwimPoint(3.7f,-6.8f),
            new NavalSwimPoint(3.7f,6.8f),new NavalSwimPoint(-3.7f,6.8f),goal};
        float[] distance={0f,float.MaxValue,float.MaxValue,float.MaxValue,float.MaxValue,float.MaxValue};
        bool[] used=new bool[6];int[] previous={-1,-1,-1,-1,-1,-1};
        for(int step=0;step<6;step++)
        {
            int current=-1;for(int i=0;i<6;i++)if(!used[i]&&(current<0||distance[i]<distance[current]))current=i;
            if(current<0||distance[current]==float.MaxValue)break;used[current]=true;
            for(int j=0;j<6;j++)if(!used[j]&&Clear(points[current],points[j]))
            {float value=distance[current]+Distance(points[current],points[j]);if(value<distance[j]){distance[j]=value;previous[j]=current;}}
        }
        int node=5;if(previous[node]<0){next=goal;return float.MaxValue;}
        while(previous[node]>0)node=previous[node];next=points[node];return distance[5];
    }
}
