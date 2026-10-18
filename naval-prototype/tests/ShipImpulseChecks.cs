using System;
class ShipImpulseChecks
{
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
 static bool Near(float a,float b){return Math.Abs(a-b)<.001;}
 static void Main()
 {
  float j=NavalShipImpulseMath.Impulse(4,27,27),a=4-j/27,b=j/27;
  Check(a>0&&b>a,"Equal hulls continue and separate rather than both stopping");
  Check(Near(27*a+27*b,108),"Linear momentum conserved at impact");
  Check(a*a+b*b<16,"Low restitution does not create kinetic energy");
  j=NavalShipImpulseMath.Impulse(4,8,27);
  Check(j/8>j/27,"Smaller hull changes velocity more");
  Check(NavalShipImpulseMath.Impulse(-2,8,27)==0&&NavalShipImpulseMath.Impulse(0,8,27)==0,"Separating or resting hulls receive no extra kick");
  float nx,nz;
  NavalShipImpulseMath.Normal(0,0,0,12.9f,5,0,1,1,out nx,out nz);
  Check(nx>.99&&Math.Abs(nz)<.001,"Glancing side impact uses hull-face normal, not center diagonal");
  NavalShipMomentum m=new NavalShipMomentum();float speed=2;
  m.Add(1,3);m.Step(0,1,0,ref speed);
  Check(Near(speed,5)&&Near(m.DriftX,1),"Impulse splits into forward velocity and world-space lateral drift");
  m.Step(0,1,1,ref speed);Check(m.DriftX>0&&m.DriftX<1&&Near(speed,5),"Lateral drift decays smoothly, no stop or tangential loss");
  m.Stop();m.Add(0,1);speed=0;m.Step(0,1,0,ref speed,.5f);Check(Near(speed*.5f,1),"Flood-speed attenuation cannot halve the physical impulse");
  foreach(float mass in new[]{8f,27f})
  {
   float va=4,vb=0,za=0,zb=25.5f;
   j=NavalShipImpulseMath.Impulse(va-vb,mass,mass);va-=j/mass;vb+=j/mass;
   for(int i=0;i<100;i++)
   {
    float t=NavalShipContactMath.SafeFraction(0,za,0,0,za+va*.05f,0,0,zb,0);
    za+=va*.05f*t;zb+=vb*.05f;
    Check(!NavalShipContactMath.Overlap(0,za,0,0,zb,0),"Swept hull guard remains solid during separation");
   }
   Check(za>5&&zb>30,"Both vessels move after contact");
  }
  Console.WriteLine("PASS: momentum, mass response, dissipation, glancing contact, drift, flooded speed conversion and solid swept separation.");
 }
}
