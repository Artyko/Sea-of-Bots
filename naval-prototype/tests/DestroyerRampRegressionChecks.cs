using System;
class DestroyerRampRegressionChecks{
 static void Main(){var s=NavalShipDefinition.Novik;
  var p=new NavalWalkPoint(.36f,.96f,3.37f);
  if(!NavalDestroyerLayout.ForedeckStair(p,s)||!NavalShipRoutes.InStairwell(p,s))throw new Exception("Logged forecastle ramp must classify as stairs");
  if(!NavalDestroyerLayout.ReachedRamp(0,.01f,-.04f,s.Scale)||NavalDestroyerLayout.ReachedRamp(0,-.6f/s.Scale,0,s.Scale))throw new Exception("Ramp capsule arrival tolerance");
  Console.WriteLine("PASS: logged forecastle ramp recognised, normal support offset accepted, below-tread shortcut rejected.");
 }
}
