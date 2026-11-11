using System;
class DestroyerRepairAccessChecks{
 static void Check(bool ok,string reason){if(!ok)throw new Exception(reason);}
 static void Main(){var s=NavalShipDefinition.Novik;float q=s.Scale;
  foreach(float deck in new[]{s.HoldFloor,s.WeatherDeck,NavalDestroyerLayout.Foredeck(s)}){
   float hole=deck-.1f/q;
   Check(Math.Abs(s.RepairAimY(hole,deck)-(deck+.03f/q))*q<.001f,"Repair on exposed slab face");
   if(deck>s.HoldFloor)Check(Math.Abs(s.RepairAimY(hole,s.HoldFloor)-(deck-.23f/q))*q<.001f,"Ceiling accessible from below");
  }
  float side=s.WeatherDeck-1.2f/q;Check(Math.Abs(s.RepairAimY(side,s.HoldFloor)-side)*q<.001f,"Side holes must not be shifted by legacy thick deck");
  foreach(int sign in new[]{-1,1}){
   var feet=new NavalWalkPoint(sign*3.6f/q,s.HoldFloor+.3f/q,20.5f/q);
   var goal=new NavalWalkPoint(sign*5.6f/q,s.WeatherDeck,30f/q);
   var path=NavalStairRecovery.Exit(feet,goal,s,s.HoldFloor,sign,.8f);
   Check(path[0].Y==s.HoldFloor,"Underflight exit cannot retain an unsupported elevated waypoint");
   Check(Math.Abs(path[0].X)*q>4.5f,"Exit clears side stair footprint");
  }
  Console.WriteLine("PASS: Novik slab faces, side repairs and grounded underflight exits.");
 }
}
