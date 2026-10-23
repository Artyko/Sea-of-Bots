using System;
class PowderLayoutSmokeChecks
{
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static float Rise(float dt)
 {
  float time=0,y=0,speed=1.2f;
  while(time<NavalSmokePlumeMath.Lifetime-.00001f)
  {float h=Math.Min(dt,NavalSmokePlumeMath.Lifetime-time);y+=speed*h;speed+=(NavalSmokePlumeMath.RiseSpeed-speed)*NavalSmokePlumeMath.Drag(h);time+=h;}
  return y;
 }
 static void Main()
 {
  int rare=0;for(int i=0;i<100000;i++)if(NavalFireIgnition.FromCannon((i+.5f)/100000))rare++;
  Check(rare==2000,"Ordinary damage ignites only 2 percent of uniform rolls");
  Check(NavalPowderState.ImpactChance==.4f&&NavalPowderState.ImpactChance>NavalFireIgnition.CannonChance*5,"Powder is substantially more dangerous than ordinary hull damage");
  int cells=0,bottom=0,middle=0;
  for(int y=0;y<NavalPowderLayout.Layers;y++)for(int x=-2;x<=2;x++)for(int z=-2;z<=2;z++)if(NavalPowderLayout.Cell(x,y,z))
  {
   cells++;if(y==0)bottom++;if(y==3)middle++;
   Check((Math.Abs(x)+.5f)*NavalPowderLayout.CellSize<=NavalPowderLayout.Diameter*.5f+.0001f,"Voxel barrel stays within designed diameter");
   Check((y+1)*NavalPowderLayout.LayerHeight<=NavalPowderLayout.Height+.0001f,"Voxel barrel stays within designed height");
  }
  Check(cells==128&&bottom<middle,"Detailed barrel has tapered ends and a wider belly");
  foreach(var ship in NavalShipDefinition.Choices)
  {
   Check(NavalPowderLayout.Count(ship)==ship.GunCount/2,"Storage count follows ship profile");
   Check(NavalPowderLayout.Height>=1.4f&&NavalPowderLayout.Height<ship.HoldClearance,"Waist-high barrel fits beneath hold ceiling");
   for(int i=0;i<NavalPowderLayout.Count(ship);i++)
   {
    float x=NavalPowderLayout.X(ship,i),z=NavalPowderLayout.Z(ship,i),half=NavalPowderLayout.Diameter*.5f/ship.Scale;
    Check(z-half>.85f||z+half< -3.7f||Math.Abs(x)-half>ship.StairWidth*.5f+.1f,"Central stair opening stays clear");
    Check(Math.Abs(x)+half<ship.HullHalfWidth(z)-.1f,"Barrel stays inside hull with edge clearance");
    Check(Math.Abs(z)+half<5.4f,"Barrel stays inside hold and clear of stern pump pipes");
    Check(NavalPowderLayout.Floor(ship)==ship.WeatherDeck,"Powder supply stands on the weather deck");
    Check(z-half>.85f||z+half< -3.7f||Math.Abs(x)-half>ship.StairWidth*.5f+.38f/ship.Scale,"Robot clearance beside central stairs");
    foreach(float mast in ship.MastZ)Check(Math.Sqrt(x*x+(z-mast)*(z-mast))*ship.Scale>NavalPowderLayout.Diameter*.5f+.65f,"Barrels clear mast foundations");
    for(int gun=0;gun<ship.GunCount;gun++){double dx=x-NavalShipDefinition.GunSide(gun)*ship.GunPostX,dz=z-NavalShipDefinition.GunZ(ship,gun);Check(Math.Sqrt(dx*dx+dz*dz)*ship.Scale>2,"Barrels clear gunner stations");}
   }
  }
  int previous=0;for(int i=0;i<=120;i++)
  {int level=NavalSmokePlumeMath.Level(12-i*.1f,12);Check(level>=previous&&level<8,"Opacity stages only fade as smoke rises");previous=level;}
  Check(NavalSmokePlumeMath.Level(12,12)==0&&NavalSmokePlumeMath.Level(0,12)==7,"Dense base and faint final smoke stage");
  float fine=Rise(1f/60),coarse=Rise(.05f);
  Check(fine>25&&fine<30&&Math.Abs(fine-coarse)<.15f,"Smoke climbs gradually to 25-30m with consistent timestep behaviour");
  Console.WriteLine("PASS: 2% hull ignition, larger wooden 128-voxel deck barrels on all profiles, stairs/tank path clearance and fading plume rise "+fine.ToString("F1")+"m (CPU model; no Unity visual test).");
 }
}
