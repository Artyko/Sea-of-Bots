using System;
class RimFloodChecks
{
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
 static float Height(float x,float y,float z,float pitch,float roll){double p=pitch*Math.PI/180,r=roll*Math.PI/180;return (float)(Math.Sin(r)*x+Math.Cos(r)*Math.Cos(p)*y-Math.Cos(r)*Math.Sin(p)*z);}
 static void Main(){foreach(var ship in NavalShipDefinition.Choices){
 foreach(int direction in new int[]{0,1,2,3}){
 float pitch=direction==0?35:direction==1?-35:0,roll=direction==2?40:direction==3?-40:0;
 float x=direction<2?0:direction==2?-3:3,z=direction<2?(direction==0?6:-6):0;
 float lip=Height(x*ship.Scale,ship.RimY*ship.Scale,z*ship.Scale,pitch,roll);
 // The galleon may need additional draft to submerge its tall upper deck.
 float sea=lip+.8f;float flow=NavalDownfloodMath.OverflowRate(sea,lip,float.NegativeInfinity,ship.Scale);
 Check(flow>0,"Submerged bow/stern/port/starboard rim admits water");
 var grid=new NavalFloodGrid(12,24,BoatDimensions.HoldWidth,BoatDimensions.HoldLength,ship.Scale,ship.RimY-ship.HoldFloor,ship.DeckCount>2);
 float accepted=grid.Add(grid.Index(x,z),flow*.05f);Check(accepted>0,"Overflow enters flood grid, not only danger pressure");
 Check(NavalDownfloodMath.OverflowRate(lip-.01f,lip,float.NegativeInfinity,ship.Scale)==0,"Dry rim does not flood");
 Check(NavalDownfloodMath.OverflowRate(sea,lip,sea,ship.Scale)==0,"Equal inner and outer level prevents extra inflow");
 }
 for(int segment=0;segment<12;segment++){
 float z=-5.5f+segment,width=1;for(int gun=0;gun<ship.GunCount;gun+=2)width-=NavalRimFlowMath.Overlap(z,1,NavalShipDefinition.GunZ(ship,gun),2.2f);
 if(ship.DeckCount>2)width-=NavalRimFlowMath.UncoveredLadder(z,ship);
 Check(width>=-.0001f&&width<=1.0001f,"Port and ladder widths are not counted twice");
 }
 }
 var urgent=new NavalDownfloodEmergency();urgent.Step(.05f,false,1,0,30);Check(urgent.Active,"Bow/stern trim flooding triggers the same response as heel");
 Console.WriteLine("PASS: 12 bow/stern/port/starboard overflow cases, accepted grid volume, dry rim, equalized pressure and opening-width conservation.");}
}
