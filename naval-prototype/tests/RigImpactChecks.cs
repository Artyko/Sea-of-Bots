using System;
class RigImpactChecks
{
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
    static void Main()
    {
        float wreckRoot=-12f,mastSection=14f;
        Check(NavalRigImpactMath.TouchesHull(0,wreckRoot+mastSection,0,0.15f,0.25f,0.15f),"Protruding mast hits even with deeply submerged wreck root");
        Check(!NavalRigImpactMath.TouchesHull(0,wreckRoot+2f,0,0.15f,0.25f,0.15f),"Buried lower mast does not block surface ship");
        Check(!NavalRigImpactMath.TouchesHull(0,8f,0,0.15f,0.25f,0.15f),"Rig above ship does not collide with hull");
        Check(!NavalRigImpactMath.TouchesHull(10f,1f,0,0.15f,0.25f,0.15f),"Rig beside ship is untouched");
        Check(NavalRigImpactMath.TouchesHull(6.45f,1f,0,0.2f,0.25f,0.15f),"Actual cell extent reaches hull edge");
        float fabric=NavalRigImpactMath.RetainedSpeed(true,true),mast=NavalRigImpactMath.RetainedSpeed(false,true);
        Check(fabric>mast&&mast>0f&&fabric<1f,"Sail softer than mast, mast yields instead of full hull stop");
        Check(NavalRigImpactMath.RetainedSpeed(false,false)>mast,"Loose debris offers less resistance");
        int steps=NavalRigImpactMath.SweepSteps(8f,12f);
        Check(8f/steps<=0.20001f&&12f/steps<=0.50001f,"Translation and rotation sweep cannot skip whole frame");
        Check(NavalRigImpactMath.SweepSteps(0f,0f)==1,"Stationary geometry still has a valid sample count");
        Check(Math.Abs(NavalRigImpactMath.CannonRadius-0.4f)<0.00001f,"Smaller cannon radius");
        float area=NavalRigImpactMath.CannonRadius*NavalRigImpactMath.CannonRadius/(0.55f*0.55f);
        Check(area>0.5f&&area<0.54f,"Cannon hole area is about half of v42");
        Console.WriteLine("PASS: exposed wreck rig, vertical separation, soft resistance, swept movement and smaller cannon holes.");
    }
}
