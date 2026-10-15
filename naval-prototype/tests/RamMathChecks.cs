using System;
class RamMathChecks
{
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
    static void Main()
    {
        Check(NavalRamMath.DamageCells(0f,false,false)==0,"Resting contact causes no damage");
        Check(NavalRamMath.DamageCells(0.7f,false,false)==0,"Gentle touch causes no damage");
        int bow=NavalRamMath.DamageCells(4f,true,false),side=NavalRamMath.DamageCells(4f,false,false),stern=NavalRamMath.DamageCells(4f,false,true);
        Check(bow<side&&bow<stern,"Bow stronger than side and stern");
        Check(side<=5&&stern<=6,"Collision damage is small");
        Check(NavalRamMath.DamageCells(1f,false,false)<side,"Higher closing speed increases damage");
        Check(NavalRamMath.DamageCells(100f,false,false)<=5,"High speed damage is bounded");
        Check(NavalRamMath.Assault(false,false),"No working guns triggers assault");
        Check(!NavalRamMath.Assault(true,false)&&!NavalRamMath.Assault(false,true)&&!NavalRamMath.Assault(true,true),"One working gun keeps naval combat");
        Console.WriteLine("PASS: gentle contact, bounded impact damage, bow armour and assault trigger.");
    }
}
