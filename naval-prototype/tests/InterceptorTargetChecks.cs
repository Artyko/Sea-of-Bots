using System;
class InterceptorTargetChecks{
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static void Main(){
  Check(NavalInterceptorTargetMath.Score(500*500,false,1)<NavalInterceptorTargetMath.Score(1000*1000,true,0),"Nearby broadside ironclad must beat distant flagship");
  Check(NavalInterceptorTargetMath.Score(500*500,true,1)<NavalInterceptorTargetMath.Score(500*500,false,1),"Flagship retains a modest mission preference");
  Check(NavalInterceptorTargetMath.Score(500*500,false,1)<NavalInterceptorTargetMath.Score(500*500,false,0),"Exposed broadside is a better torpedo target");
  Check(NavalInterceptorTargetMath.Score(500*500,false,1)*.8f<NavalInterceptorTargetMath.Score(480*480,false,1),"Incumbent hysteresis prevents small-distance target churn");
  Console.WriteLine("PASS: escorts are valid interceptor targets, broadside preference and stable engagement.");
 }
}
