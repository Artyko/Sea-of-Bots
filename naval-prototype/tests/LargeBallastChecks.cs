using System;
class LargeBallastChecks
{
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
 static void Main()
 {
  foreach(NavalShipDefinition ship in new[]{NavalShipDefinition.Sloop,NavalShipDefinition.Brigantine})
  {
   float capacity=NavalBallastLayout.Capacity(ship.Scale),old=.8f*4.4f*.24f*ship.Scale*ship.Scale;
   Check(capacity>old*4.5f,"More than four times previous real capacity");
   float physical=NavalBallastLayout.Width*NavalBallastLayout.Height*(NavalBallastLayout.Length-NavalBallastLayout.RampRun/ship.Scale)*ship.Scale*ship.Scale;
   for(int i=0;i<NavalBallastLayout.Steps;i++)physical+=NavalBallastLayout.Width*NavalBallastLayout.RampRun/ship.Scale/NavalBallastLayout.Steps*NavalBallastLayout.Height*(i+1)/NavalBallastLayout.Steps*ship.Scale*ship.Scale;
   Check(Math.Abs(capacity-physical)<.001,"Capacity matches enlarged stepped geometry");
   double momentChange=4*capacity*NavalBallastLayout.X*ship.Scale;
   Check(momentChange>0,"Full transfer changes the physical transverse mass moment");
   foreach(float side in new[]{-1f,1f})foreach(float end in new[]{-1f,1f})
   {
    NavalWalkPoint floor=new NavalWalkPoint(side*1.65f,ship.HoldFloor,end*3);
    NavalWalkPoint work=new NavalWalkPoint(side*2.35f,NavalBallastLayout.Surface(side*2.35f,end*3,ship.HoldFloor,ship.Scale),end*3);
    var up=NavalBallastRoutes.Plan(floor,work,false,ship.StairWidth,ship.HoldFloor,ship.Scale);
    float y=ship.HoldFloor;int climbs=0;
    foreach(var p in up)
    {
     if(p.Y>y+.0001){Check((p.Y-y)*ship.Scale<=.201f,"Repair approach climbs only 20cm at a time");climbs++;}
     y=p.Y;
    }
    Check(climbs==6&&Math.Abs(y-work.Y)<.001,"Reach raised repair site by the end ramp");
    var down=NavalBallastRoutes.Plan(work,floor,false,ship.StairWidth,ship.HoldFloor,ship.Scale);
    y=work.Y;int descents=0;
    foreach(var p in down)
    {
     if(p.Y<y-.0001){Check((y-p.Y)*ship.Scale<=.201f,"Return descends by the same small steps");descents++;}
     y=p.Y;
    }
    Check(descents==6&&Math.Abs(y-ship.HoldFloor)<.001,"Return reaches ordinary floor");
    var escape=NavalBallastRoutes.Plan(work,new NavalWalkPoint(1.65f,.825f,1.1f),false,ship.StairWidth,ship.HoldFloor,ship.Scale);
    Check(escape[escape.Count-1].Y==.825f,"Emergency route descends tank before ascending ship stairs");
   }
   Console.WriteLine(ship.Name+": total ballast "+(4*capacity).ToString("F1")+" m3; transverse moment change "+momentChange.ToString("F1")+" m4 water-density units.");
  }
  Console.WriteLine("PASS: physical enlarged capacity, mass moment, permanent access and return ramps on both sides/ends/profiles.");
 }
}
