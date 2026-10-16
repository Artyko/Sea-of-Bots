using System;
class ShoreEscapeChecks
{
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
 static void Main(){
 var clear=new NavalShoreEscape();Check(clear.Allowed,"Clear water allows movement");
 var outwards=new NavalShoreEscape();outwards.Observe(.3f,.1f);outwards.Observe(.2f,.2f);
 Check(outwards.Allowed,"Reduce one contact without worsening another");
 var through=new NavalShoreEscape();through.Observe(.3f,.4f);Check(!through.Allowed,"Cannot push deeper through terrain");
 var otherRock=new NavalShoreEscape();otherRock.Observe(.5f,.1f);otherRock.Observe(0f,.1f);
 Check(!otherRock.Allowed,"Escaping first rock cannot enter a new rock");
 var stuck=new NavalShoreEscape();stuck.Observe(.2f,.2f);Check(!stuck.Allowed,"No teleporting tangentially through a persistent overlap");
 var outside=new NavalShoreEscape();outside.Observe(.2f,0f);Check(outside.Allowed,"Completely clear contact can exit");
 Console.WriteLine("Shore escape penetration checks passed");
 }
}
