using System;
class StrongerBroadsideChecks
{
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Main()
 {
  Check(NavalSessionSettings.PlayerShip==NavalShipDefinition.Brigantine,"New session defaults to brigantine");
  Check(NavalShipDefinition.Choices.Length==3,"Sloop and galleon remain selectable");
  int cases=0,acquisitions=0;bool turnedLeft=false,turnedRight=false;
  foreach(var ship in NavalShipDefinition.Choices)
  {
   int maximum=ship.GunCount/2;
   for(int port=0;port<=maximum;port++)for(int starboard=0;starboard<=maximum;starboard++)
   {
    if(port+starboard==0)continue;
    for(int heading=0;heading<360;heading+=30)for(int bearing=0;bearing<360;bearing+=45)
    for(int previous=0;previous<=2;previous++)for(int firing=0;firing<=2;firing++)
    {
     int side=NavalBroadsideMath.HelmSide(heading,bearing,port,starboard,previous,firing);
     if(port>starboard)Check(side==1,"More surviving port guns overrides previous/visible weak side");
     if(starboard>port)Check(side==2,"More surviving starboard guns overrides previous/visible weak side");
     if(port==starboard&&previous>0)Check(side==previous,"Equal batteries retain committed turn");
     if(firing==1&&port>0||firing==2&&starboard>0)
      Check(NavalBroadsideMath.CombatSide(firing,port,starboard)==firing,"Crew keeps exposed guns firing during helm side change");
     cases++;
    }
   }
   for(int stronger=1;stronger<=2;stronger++)for(int initial=0;initial<360;initial+=30)
   {
    int port=stronger==1?maximum:Math.Max(0,maximum-1),starboard=stronger==2?maximum:Math.Max(0,maximum-1),previous=3-stronger;
    double x=0,z=-100;float heading=initial,rudder=0;bool acquired=false;
    for(int step=0;step<2600;step++)
    {
     float bearing=(float)(Math.Atan2(-x,-z)*180/Math.PI);
     int visible=NavalBroadsideMath.Delta(heading,bearing)<0?1:2;
     int selected=NavalBroadsideMath.HelmSide(heading,bearing,port,starboard,previous,visible);
     Check(selected==stronger,"Visible weaker gun cannot cancel a committed stronger-side turn");previous=selected;
     float error=NavalBroadsideMath.Delta(heading,NavalBroadsideMath.Heading(bearing,selected));
     if(Math.Abs(error)<20){acquired=true;break;}
     turnedLeft|=error<0;turnedRight|=error>0;
     rudder=NavalRigMath.StepRudder(rudder,Math.Max(-1,Math.Min(1,error/25)),.05f);
     heading+=NavalRigMath.RudderRate(3,rudder)*.05f;
     x+=Math.Sin(heading*Math.PI/180)*3*.05;z+=Math.Cos(heading*Math.PI/180)*3*.05;
    }
    Check(acquired,"Moving ship brings stronger broadside into gun sector");acquisitions++;
   }
  }
  Check(turnedLeft&&turnedRight,"Both physical turn directions remain available");
  Check(NavalBroadsideMath.HelmSide(0,90,3,2,2,2)==1,"Restored third cannon changes preference to port");
  Check(NavalBroadsideMath.HelmSide(0,-90,1,2,1,1)==2,"Loss of guns changes preference to starboard");
  Check(NavalCrewPriorities.CanBoard(true,false,false,false),"All guns lost preserves boarding fallback");
  Console.WriteLine("PASS: brigantine default, "+cases+" side-selection states across all ship profiles, continued weaker-side fire and "+acquisitions+" moving turns in both directions.");
 }
}
