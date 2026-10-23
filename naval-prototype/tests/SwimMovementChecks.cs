using System;
class SwimMovementChecks {
 static void Check(bool value,string why){if(!value)throw new Exception(why);}
 static void Main(){
 Check(!NavalSwimMovement.InWater(.5f,2.6f,false),"Knees preserve normal walking");
 Check(!NavalSwimMovement.InWater(1.3f,2.6f,false)&&NavalSwimMovement.InWater(1.31f,2.6f,false),"Waist threshold");
 Check(NavalSwimMovement.InWater(1.2f,2.6f,true)&&!NavalSwimMovement.InWater(1f,2.6f,true),"Wave hysteresis prevents speed flicker");
 Check(!NavalSwimMovement.InWater(float.NegativeInfinity,2.6f,true),"Dry hold remains dry despite external sea");
 Check(NavalSwimMovement.Speed(5.5f,false)==5.5f&&NavalSwimMovement.Speed(5.5f,true)==2.8f,"Player and sailor water cap identical");
 Check(Math.Abs(NavalSwimMovement.Speed(2.75f,true)-1.7875f)<.0001f,"Injured legs remain slower in water");
 Check(NavalSwimMovement.Speed(.05f,true)<=.05f,"Swimming cannot accelerate crawling");
 Console.WriteLine("PASS: knee/waist depth, wave hysteresis, dry hold, 2.8m/s cap and injury-aware slowdown.");
 }
}
