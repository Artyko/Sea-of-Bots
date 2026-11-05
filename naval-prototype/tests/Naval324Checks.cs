using System;
class Naval324Checks {
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static void Main(){var s=NavalShipDefinition.Novik;float q=s.Scale;
 for(int i=0;i<4;i++)Check(Math.Abs(NavalTorpedoSalvoMath.MountOffset(i)+NavalTorpedoSalvoMath.MountOffset(3-i))<.001,"Balanced four mount lanes");
 Check(NavalTorpedoMath.CanAttack(true,false)&&NavalTorpedoMath.CanAttack(false,true)&&!NavalTorpedoMath.CanAttack(false,false),"One loaded tube can attack without waiting for second");
 Check(NavalTorpedoMath.BlastRadius>2.8f*1.5f&&NavalTorpedoMath.BlastPower>2.6f,"Stronger common torpedo blast");
 Console.WriteLine("PASS: symmetric fan lanes, single ready tube launch and increased blast.");}
}
