using System;
class LoggedStairRecoveryChecks {
 static void Check(bool value,string why){if(!value)throw new Exception(why);}
 static void Main(){
 float[,] cases={{-.18f,.63f,-.04f,-.18f,.52f,-.05f},{-.18f,.37f,-.48f,-.18f,.25f,-.48f},{-.18f,.81f,.25f,-.18f,.69f,.24f},{-.18f,-.16f,-1.35f,-.18f,-.28f,-1.36f}};
 foreach(var ship in NavalShipDefinition.Choices){
 for(int i=0;i<cases.GetLength(0);i++)Check(NavalWalkRoutes.Reached(cases[i,0]-cases[i,3],cases[i,1]-cases[i,4],cases[i,2]-cases[i,5],ship.Scale),"Logged capsule height lag must advance horizontally reached tread");
 Check(!NavalWalkRoutes.Reached(0,-1f/ship.Scale,0,ship.Scale),"A metre below the tread cannot skip upwards");
 Check(!NavalWalkRoutes.Reached(0,.8f/ship.Scale,0,ship.Scale),"A high ledge cannot count as the next tread");
 Check(!NavalWalkRoutes.Reached(.5f/ship.Scale,.1f/ship.Scale,0,ship.Scale),"Cannot skip horizontally distant tread");
 }
 Console.WriteLine("PASS: exact four staircase stalls from v193 log on every profile; asymmetric capsule-height tolerance, no below-stair or horizontal shortcuts.");
 }
}
