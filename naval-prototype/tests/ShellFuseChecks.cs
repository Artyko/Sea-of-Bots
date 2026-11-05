using System;
class ShellFuseChecks {
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static void Main(){
  foreach(bool main in new[]{false,true})foreach(bool prior in new[]{false,true}){
   Check(!NavalShellFuse.Arms(main,NavalPenetrationMaterial.Wood,10,prior),"Wood never arms");
   Check(!NavalShellFuse.Arms(main,NavalPenetrationMaterial.Armor,0,prior),"No perforation, no fuze");
   Check(NavalShellFuse.Arms(main,NavalPenetrationMaterial.Armor,1,prior),"Armour arms all AP");
  }
  foreach(var material in new[]{NavalPenetrationMaterial.Iron,NavalPenetrationMaterial.Machinery}){
   Check(NavalShellFuse.Arms(false,material,1,false),"Small AP arms on first metal");
   Check(!NavalShellFuse.Arms(true,material,1,false),"Main AP crosses first metal");
   Check(NavalShellFuse.Arms(true,material,1,true),"Main AP arms on next metal or mechanism");
  }
  Check(!NavalShellFuse.SeparateMetal(.1f,2,false),"Coplanar seam cannot count twice");
  Check(!NavalShellFuse.SeparateMetal(.8f,.8f,true),"One curved shell plate cannot count twice");
  Check(NavalShellFuse.SeparateMetal(.8f,.8f,false),"Real internal mechanism counts");
  Check(NavalShellFuse.SeparateMetal(2,2,true),"Next shell plate counts");
  Console.WriteLine("PASS: calibre-specific AP arming, wood exclusion and same-plate seam rejection.");
 }
}
