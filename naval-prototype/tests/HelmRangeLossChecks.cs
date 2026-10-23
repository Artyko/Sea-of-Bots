using System;
class HelmRangeLossChecks {
 static void Check(bool value,string why){if(!value)throw new Exception(why);}
 static void Main(){
 var pass=new NavalFirePassMath();
 Check(pass.Step(.1f,90,0,60,40,1,2,1,false,true)&&pass.Side==2,"New pass seeks stronger battery even while weaker side bears");
 pass.Reset();pass.Step(.1f,90,0,60,40,2,2,1,false,true);
 Check(pass.Side==1,"Equal batteries retain currently exposed side");
 for(int i=0;i<10;i++)Check(pass.Step(.1f,90,0,60,40,2,2,1,false,false),"Short range loss does not thrash course");
 pass.Step(.1f,90,0,60,40,2,2,1,true,true);
 Check(pass.Phase==2,"Restored reach preserves firing pass");
 for(int i=0;i<31;i++)pass.Step(.1f,90,0,150,40,2,2,0,false,false);
 Check(pass.Phase==0,"Persistent loss of range releases pass for closing course");
 Check(!pass.Step(.1f,90,0,150,40,2,2,0,false,false),"Cannot restart unreachable pass");
 pass.Step(.1f,90,0,60,40,1,0,1,false,true);
 pass.Step(.1f,90,0,60,40,0,2,2,false,true);
 Check(pass.Side==2,"Destroyed battery changes to surviving side");
 Console.WriteLine("PASS: stronger battery, equal-side stability, transient and sustained range loss, destroyed-side recovery.");
 }
}
