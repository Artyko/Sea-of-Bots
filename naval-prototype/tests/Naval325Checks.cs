using System;
class Naval325Checks {
 static void Check(bool v,string why){if(!v)throw new Exception(why);}
 static void Main(){Check(Math.Abs(NavalOilDriveMath.Branch(1,1,1,.93f,true)-.93f)<.001,"Damaged screw retains partial thrust");Check(NavalOilDriveMath.Branch(1,1,1,1,false)==0,"Severed shaft removes its branch");Check(Math.Abs(NavalOilDriveMath.Branch(.55f,0,1,1,true)-.275f)<.001,"Single surviving boiler retains real limited power");Check(NavalOilDriveMath.Branch(1,1,0,1,true)==0,"Broken turbine cannot drive");Console.WriteLine("PASS: partial screw damage, severed shafts, one boiler and turbine loss use separate drive rules.");}
}
