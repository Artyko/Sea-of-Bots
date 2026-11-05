using System;
class EarlySteamRetreatChecks {
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static void Main(){var p=new NavalEarlySteamRetreat();
 Check(!p.Step(.1f,0,0,false,1,1),"Dry ship keeps fighting");
 Check(p.Step(.1f,.30f,.3f,false,1,1),"Withdraw before firebox flooding begins at .65m");
 for(int i=0;i<14;i++)Check(p.Step(1,0,0,false,1,1),"Require sustained recovery");
 Check(!p.Step(1,0,0,false,1,1),"Return after 15 calm seconds");p.Reset();
 Check(p.Step(.1f,.05f,.56f,true,1,1),"Nearby rising bilge plus live leak triggers preemptive escape");p.Reset();
 bool active=false;for(int i=0;i<25;i++)active=p.Step(1,.10f+i*.006f,.25f,false,1,1)||active;
 Check(active,"Predicted flooding within 90 seconds triggers retreat while steam is intact");
 Console.WriteLine("PASS: early local/adjacent/rising-water triggers and sustained recovery hysteresis.");}
}
