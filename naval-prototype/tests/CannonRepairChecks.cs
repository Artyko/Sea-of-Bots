using System;
class CannonRepairChecks {
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static void Main(){foreach(var ship in NavalShipDefinition.Choices){
 var guns=new NavalCannonRepairBudget[ship.GunCount];for(int i=0;i<guns.Length;i++)guns[i]=new NavalCannonRepairBudget();
 foreach(var gun in guns){Check(!gun.Ready(4)&&!gun.Complete(true,4),"Quick hull patch cannot complete cannon repair");
 Check(!gun.Ready(59.99f)&&!gun.Complete(true,59.99f),"Minute must be fully worked");
 Check(!gun.Complete(false,60)&&!gun.Used,"Interrupted or incomplete restoration does not consume repair allowance");
 Check(gun.Ready(60)&&gun.Complete(true,60)&&gun.Used,"One complete minute-long rebuild succeeds");
 Check(!gun.Ready(120)&&!gun.Complete(true,120),"Neither another sailor nor player can rebuild same cannon a second time");}
 for(int i=0;i<guns.Length;i++)Check(guns[i].Used,"Every cannon has its own independent one-use allowance");
 guns[0].Reset();Check(guns[0].Ready(60),"Explicit debug reset clears allowance");
 if(guns.Length>1)Check(guns[1].Used,"Resetting one gun does not reset another");}
 Console.WriteLine("PASS: 2/4/6 independent guns, 60-second threshold, no quick patch or second repair, incomplete work and explicit reset.");}
}
