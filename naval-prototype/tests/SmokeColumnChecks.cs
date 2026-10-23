using System;
class SmokeColumnChecks
{
 static void Check(bool value,string text){if(!value)throw new Exception(text);}
 static void Main(){
 Check(NavalCubeSmokeMath.Capacity==576,"Particle cap increases by exactly fifty percent");
 Check(Math.Abs(.25f/NavalSmokePlumeMath.ColumnInterval-1.5f)<.001f,"Emission rate increases by fifty percent");
 float gap=NavalSmokePlumeMath.ColumnInterval*NavalCubeSmokeMath.RiseSpeed;
 Check(gap<NavalSmokePlumeMath.ColumnSize,"Successive puffs overlap vertically instead of isolated cubes");
 Check(NavalSmokePlumeMath.MaxColumnPuffsPerTick*NavalSmokePlumeMath.Lifetime/NavalSmokePlumeMath.ColumnInterval<=NavalCubeSmokeMath.Capacity+.001f,"All fire columns fit within bounded particle budget");
 foreach(float fps in new float[]{30,60,120}){
 float emittedAt=0,clock=0;int count=0;float maxGap=0;
 for(int i=0;i<fps*12;i++){
 clock+=1/fps;
 if(clock>=NavalSmokePlumeMath.ColumnInterval){clock-=NavalSmokePlumeMath.ColumnInterval;float t=i/fps;if(count>0)maxGap=Math.Max(maxGap,(t-emittedAt)*NavalCubeSmokeMath.RiseSpeed);emittedAt=t;count++;}
 }
 Check(maxGap<NavalSmokePlumeMath.ColumnSize,"Emission timing keeps column connected at supported frame rates");
 }
 foreach(float age in new float[]{.1f,.25f,1,3,10}){
  Check(Math.Abs(NavalSmokePlumeMath.RelativeDrift(0,age))<.00001,"Stationary ship leaves vertical smoke");
  Check(NavalSmokePlumeMath.RelativeDrift(2,age)<0,"Moving source leaves smoke behind from the first instant");
  Check(NavalSmokePlumeMath.RelativeDrift(5,age)<NavalSmokePlumeMath.RelativeDrift(2,age),"Higher speed produces stronger backward sweep");
 }
 int lastCount=0;float lastSize=0;
 for(int fire=0;fire<=32;fire++){
  int count=NavalSmokePlumeMath.Puffs(fire);float size=NavalSmokePlumeMath.FireSize(fire);
  Check(count>=lastCount&&size>=lastSize,"Larger fire never reduces smoke density or width");lastCount=count;lastSize=size;
 }
 Check(NavalSmokePlumeMath.Puffs(12)>NavalSmokePlumeMath.Puffs(1),"Clustered large fire produces extra puffs");
 Check(NavalSmokePlumeMath.Alpha(0,12)==0,"New smoke starts transparent instead of flashing an opaque cube");
 Check(NavalSmokePlumeMath.Alpha(12,12)==0,"Expired smoke fades to zero opacity");
 Check(NavalSmokePlumeMath.BirthScale(0)<NavalSmokePlumeMath.BirthScale(.15f)&&NavalSmokePlumeMath.BirthScale(.3f)==1,"Smoke grows gradually at the source");
 float previousAlpha=0;
 for(int i=0;i<=25;i++){float alpha=NavalSmokePlumeMath.Alpha(i*.01f,12);Check(alpha>=previousAlpha-.001&&alpha<=.65f,"Birth alpha changes continuously within safe limits");previousAlpha=alpha;}
 Console.WriteLine("PASS: connected plume spacing at 30/60/120 FPS, bounded 576-puff budget (CPU model, no Unity render).");
 }
}
