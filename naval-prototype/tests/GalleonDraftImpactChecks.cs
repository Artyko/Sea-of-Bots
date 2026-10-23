using System;
class GalleonDraftImpactChecks {
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static void Main(){var ship=NavalShipDefinition.Galleon;
 float oldWaterline=.825f+ship.HoldClearance/(2*ship.Scale)-.6f/ship.Scale;
 Check(Math.Abs((oldWaterline-ship.DesignWaterline)*ship.Scale-.5f)<.0001f,"Galleon floats 0.5 m higher");
 Check(Math.Abs(NavalRigImpactMath.CannonRadiusForScale(4)*4-NavalRigImpactMath.CannonRadiusForScale(3)*3)<.0001f,"Galleon impact world radius matches brigantine");
 Check(Math.Abs(NavalRigImpactMath.CannonRadiusForScale(2)-.4f)<.0001f,"Sloop holes unchanged");
 Check(Math.Abs(NavalRigImpactMath.CannonRadiusForScale(3)-.4f)<.0001f,"Brigantine holes unchanged");
 Console.WriteLine("PASS: 0.5 m higher galleon, capped world impact size, unchanged smaller ship damage.");}
}
