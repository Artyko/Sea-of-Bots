using System;
public struct NavalMassMoments
{
    public double Mass,MX,MY,Inertia;
    public void Add(double mass,double x,double y)
    {if(mass<=0)return;Mass+=mass;MX+=mass*x;MY+=mass*y;Inertia+=mass*(x*x+y*y);}
}
// A transverse section clips a rectangular approximation of the hull against the water plane.
// Displacement and mass share water-density units; gravity therefore needs no hidden ballast multiplier.
public sealed class NavalHydrostaticSection
{
    private readonly double[] x=new double[4],y=new double[4],cx=new double[8],cy=new double[8];
    public double Area,CenterX,CenterY,Waterline;
    private void Clip(double level,double slope,bool centroid=true)
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
        for(int i=0;i<n;i++){int j=(i+1)%n;double q=cx[i]*cy[j]-cx[j]*cy[i];twice+=q;if(centroid){mx+=(cx[i]+cx[j])*q;my+=(cy[i]+cy[j])*q;}}
        Area=Math.Abs(twice)*.5;
        if(!centroid)return;
        CenterX=Math.Abs(twice)>1e-10?mx/(3*twice):0;CenterY=Math.Abs(twice)>1e-10?my/(3*twice):0;
    }
    public double FractionBelow(double width,double height,double angleDegrees,double waterline)
    {
        double phi=angleDegrees*Math.PI/180,c=Math.Cos(phi),s=Math.Sin(phi);
        for(int i=0;i<4;i++){double px=(i==0||i==3)?-width*.5:width*.5,py=i<2?0:height;x[i]=px*c-py*s;y[i]=px*s+py*c;}
        Clip(waterline,0,false);return Math.Max(0,Math.Min(1,Area/(width*height)));
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
        // If both intersections lie on the side walls, trapezoid area gives the waterline exactly.
        double vertical=c+slope*s,horizontal=s-slope*c;
        if(Math.Abs(vertical)>1e-8)
        {
            double midHeight=vertical>0?bottom+requiredArea/width:top-requiredArea/width;
            double halfRise=Math.Abs(horizontal*width*.5/vertical);
            if(midHeight-halfRise>=bottom&&midHeight+halfRise<=top)
            {Waterline=vertical*midHeight;Clip(Waterline,slope);return;}
        }
        for(int i=0;i<18;i++){double mid=(low+high)*.5;Clip(mid,slope,false);if(Area<requiredArea)low=mid;else high=mid;}
        Waterline=(low+high)*.5;Clip(Waterline,slope);
    }
}
public sealed class NavalStabilityMath
{
    public const float DefaultGMFraction=.10f;
    private readonly NavalHydrostaticSection section=new NavalHydrostaticSection();
    public readonly double Beam,Bottom,Top,Length,DryMass,DryY,DryInertia;
    public float Angle {get;private set;}
    public float AngularVelocity {get;private set;}
    public float Waterline {get;private set;}
    public float RightingArm {get;private set;}
    public NavalStabilityMath(float scale,float floor,float rim,float initialGMFraction=DefaultGMFraction)
    {
        Beam=BoatDimensions.HalfWidth*2*scale;Bottom=floor*scale;Top=rim*scale;Length=BoatDimensions.HalfLength*2*scale;
        double draft=-Bottom;DryMass=Beam*draft*Length;
        // A low dry CG gives reserve stability against free-water motion. It is fixed, never changed by pump commands.
        DryY=Bottom+draft*.5+Beam*Beam/(12*draft)-Beam*initialGMFraction;
        DryInertia=DryMass*((Beam*Beam+(Top-Bottom)*(Top-Bottom))/12+DryY*DryY);
    }
    public void Observe(float actualAngle)
    {if(Math.Abs(NavalBroadsideMath.Delta(Angle,actualAngle))>20)AngularVelocity=0;Angle=actualAngle;}
    public double Torque(NavalMassMoments water,float angle,float waveAngle)
    {
        double mass=DryMass+water.Mass;
        // A local wave-support frame approximates wave pressure without a separate angle correction.
        double relative=angle-waveAngle;
        section.Solve(Beam,Bottom,Top,relative,mass/Length);
        double phi=relative*Math.PI/180,worldGX=(water.MX*Math.Cos(phi)-(DryMass*DryY+water.MY)*Math.Sin(phi))/mass;
        Waterline=(float)(section.Waterline/Math.Max(.5,Math.Cos(waveAngle*Math.PI/180)));RightingArm=(float)(section.CenterX-worldGX);
        return mass*9.81*RightingArm;
    }
    public float Step(float dt,NavalMassMoments water,float waveAngle)
    {
        int steps=Math.Max(1,(int)Math.Ceiling(Math.Max(0,dt)/.016));double h=Math.Max(0,dt)/steps;
        for(int i=0;i<steps;i++)
        {
            double acceleration=Torque(water,Angle,waveAngle)/Math.Max(1,DryInertia+water.Inertia);
            double velocity=AngularVelocity*Math.PI/180;
            velocity=(velocity+acceleration*h)*Math.Exp(-1.1*h);
            AngularVelocity=(float)(velocity*180/Math.PI);
            Angle=(float)(((Angle+AngularVelocity*h+540)%360)-180);
        }
        Torque(water,Angle,waveAngle);return Angle;
    }
    public void AddTank(ref NavalMassMoments mass,NavalBallastTank tank,float scale,float floor)
    {
        if(tank.Water>0)
        {
            double width=NavalBallastLayout.Width*scale,height=NavalBallastLayout.Height;
            section.Solve(width,0,height,Angle,width*height*tank.Water/tank.Capacity);
            double phi=Angle*Math.PI/180,c=Math.Cos(phi),s=Math.Sin(phi);
            double localX=section.CenterX*c+section.CenterY*s,localY=-section.CenterX*s+section.CenterY*c;
            mass.Add(tank.Water,tank.X*scale+localX,floor*scale+localY);
        }
        if(tank.SpillPending>0)mass.Add(tank.SpillPending,0,floor*scale);
    }
}
