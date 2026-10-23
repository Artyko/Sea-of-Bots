using System;
class GalleonV192Checks {
 static void Check(bool value,string why){if(!value)throw new Exception(why);}
 static void Main(){
 var ship=NavalShipDefinition.Galleon;
 Check(ship.MaintenanceDeckCount==2&&NavalShipDefinition.Sloop.MaintenanceDeckCount==1&&NavalShipDefinition.Brigantine.MaintenanceDeckCount==1,"Only three-deck hull duplicates maintenance posts");
 Check(ship.MaintenanceFloor(0)==ship.HoldFloor&&ship.MaintenanceFloor(1)==.825f,"Post floors match deck surfaces");
 Check(NavalPowderLayout.Count(ship)==4,"Four powder barrels");
 for(int i=0;i<4;i+=2){Check(NavalPowderLayout.X(ship,i)<0&&NavalPowderLayout.X(ship,i+1)>0,"Two barrels per side");Check(NavalPowderLayout.Z(ship,i)==NavalPowderLayout.Z(ship,i+1),"Symmetric pairs between guns");}
 var grid=new NavalFloodGrid(12,24,BoatDimensions.HoldWidth,BoatDimensions.HoldLength,ship.Scale,ship.RimY-ship.HoldFloor,true);
 grid.ConfigureStorage(ship);
 var mass=new NavalMassMoments();for(int i=0;i<grid.Water.Length;i++)if(grid.IsActive(i)){
 float volume=grid.VolumeAtDepth(i,ship.HoldClearance/ship.Scale);grid.Add(i,volume);mass.Add(volume,grid.X(i)*ship.Scale,(ship.HoldFloor+grid.CenterDepth(i))*ship.Scale);
 }
 var model=ship.CreateStability();model.Torque(mass,0,0);
 float freeboard=ship.GunPortSill*ship.Scale-model.Waterline;
 Console.WriteLine("Full lower hold: "+grid.Total.ToString("F0")+" m3; gunport freeboard "+freeboard.ToString("F2")+" m");
 Check(grid.Total<ship.SinkingVolume,"Full first hold must not set scripted sunk state using sloop volume limit");
 Check(freeboard>.1f,"A symmetrically full lower hold leaves positive gunport reserve without artificial lift");
 var dry=ship.CreateStability();dry.Torque(new NavalMassMoments(),0,0);Check(Math.Abs(dry.Waterline-ship.DesignWaterline*ship.Scale)<.001f,"Dry displacement remains physical");
 Check(NavalRepairAim.InnerEnd(6,ship.Scale)<5.875f,"Logged bow-seam repair aims at inner face");
 foreach(float deck in new float[]{.825f,ship.WeatherDeck}){
 float targetY=ship.RepairAimY(deck-.1f,deck);Check(targetY>deck,"Upper seam target clears deck slab");
 float lower=deck==.825f?ship.HoldFloor:.825f;
 targetY=ship.RepairAimY(deck-.1f,lower);Check(targetY<deck-.25f,"Lower seam target clears ceiling slab");
 }
 var clock=new NavalSailPoseClock();int close=0,far=0;var distant=new NavalSailPoseClock();
 for(int i=0;i<600;i++){float time=i/60f;if(clock.Due(time,1,false))close++;if(distant.Due(time,1,true))far++;}
 Check(close<=301&&far<=151,"Pose writes bounded at 30/15 Hz");Check(clock.Due(9.99f,.5f,false),"Sail control changes update immediately");
 Console.WriteLine("PASS: maintenance floors, four barrels, full lower hold reserve, real dry displacement, seam targets and bounded sail pose work.");
 }
}
