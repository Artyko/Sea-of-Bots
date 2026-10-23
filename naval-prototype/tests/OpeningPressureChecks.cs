using System;
class OpeningPressureChecks {
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static void Main(){try{Run();}catch(Exception e){Console.WriteLine(e);Environment.ExitCode=1;}}
 static void Run(){foreach(var ship in NavalShipDefinition.Choices){
 foreach(float angle in new float[]{35,-35,60,-60,80,-80}){
 float up=(float)Math.Cos(angle*Math.PI/180),slope=(float)Math.Abs(Math.Sin(angle*Math.PI/180));
 // Bow/stern and side edge cells have centres shifted inward from the actual opening.
 foreach(float lipLocal in new float[]{ship.RimY,ship.GunPortSill}){
 float lip=0,sea=.1f,depth=lipLocal-ship.HoldFloor-.1f;
 float correct=NavalDownfloodMath.InsideAtOpening(lip,lipLocal,ship.HoldFloor,depth,up,ship.Scale);
 float centre=correct+.25f*ship.Scale*slope;
 if(centre>=sea)Check(NavalDownfloodMath.OverflowRate(sea,lip,centre,ship.Scale)==0,"Reproduce false cell-centre seal");
 Check(NavalDownfloodMath.OverflowRate(sea,lip,correct,ship.Scale)>0,"Submerged opening with interior below lip continues flooding");
 float equalDepth=lipLocal-ship.HoldFloor+sea/(up*ship.Scale);
 float equal=NavalDownfloodMath.InsideAtOpening(lip,lipLocal,ship.HoldFloor,equalDepth,up,ship.Scale);
 Check(NavalDownfloodMath.OverflowRate(sea,lip,equal,ship.Scale)<.00001f,"Equal levels stop inflow");
 float dry=NavalDownfloodMath.InsideAtOpening(lip,lipLocal,ship.HoldFloor,0,up,ship.Scale);
 Check(float.IsNegativeInfinity(dry),"Dry floor is not water pressure");
 Check(NavalDownfloodMath.OverflowRate(-.01f,lip,dry,ship.Scale)==0,"Dry exterior does not flood");
 }
 }
 float inverted=NavalDownfloodMath.InsideAtOpening(0,ship.RimY,ship.HoldFloor,1,-.5f,ship.Scale);
 Check(float.IsNegativeInfinity(inverted),"Inverted column does not create imaginary seal");
 float flow=NavalDownfloodMath.OverflowRate(.5f,0,float.NegativeInfinity,2.2f*ship.Scale);
 var grid=new NavalFloodGrid(12,24,BoatDimensions.HoldWidth,BoatDimensions.HoldLength,ship.Scale,ship.RimY-ship.HoldFloor,ship.DeckCount>2);grid.ConfigureStorage(ship);
 Check(grid.Add(grid.Index(-ship.HullHalfWidth(0),0),flow*.05f)>0,"Opening flow accepted by actual configured flood grid");
 }
 Console.WriteLine("PASS: edge-pressure regression, bow/stern/side inclinations on all ships, dry/inverted columns, equalization, actual galleon storage acceptance.");
 }
}
