using System;
using System.IO;
using System.Reflection;
using UnityEngine;

class SteamV232Checks
{
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    static void Near(float actual,float expected,float tolerance,string message)
    {if(Math.Abs(actual-expected)>tolerance)throw new Exception(message+": "+actual+" != "+expected);}
    static void Main()
    {
        AppDomain.CurrentDomain.AssemblyResolve+=delegate(object sender,ResolveEventArgs args)
        {
            string folder=Environment.GetEnvironmentVariable("NAVAL_GAME_MANAGED");
            if(String.IsNullOrEmpty(folder))return null;
            string path=Path.Combine(folder,new AssemblyName(args.Name).Name+".dll");
            return File.Exists(path)?Assembly.LoadFrom(path):null;
        };
        Run();
    }
    static void Run()
    {
        NavalEra era;string error;
        Check(NavalEraMarker.TryResolve(new[]{"x",NavalEraMarker.SailWater},out era,out error)&&era==NavalEra.Sail,"Sail marker selects sail era");
        Check(NavalEraMarker.TryResolve(new[]{NavalEraMarker.SteamWater},out era,out error)&&era==NavalEra.Steam,"Steam marker selects steam era");
        Check(!NavalEraMarker.TryResolve(new string[0],out era,out error)&&!String.IsNullOrEmpty(error),"Missing marker is rejected");
        Check(!NavalEraMarker.TryResolve(new[]{NavalEraMarker.SailWater,NavalEraMarker.SteamWater},out era,out error),"Conflicting markers are rejected");
        Check(!NavalEraMarker.TryResolve(new[]{NavalEraMarker.SteamWater,NavalEraMarker.SteamWater},out era,out error),"Duplicate markers are rejected");

        var steam=new NavalSteamState();
        Near(steam.Coal,100f,.0001f,"Steam plant starts with 100 coal");
        Near(steam.Pressure,.55f,.0001f,"Steam plant starts at unattended pressure");
        for(int i=0;i<100;i++)steam.Step(.05f,1f,true,1f,1f);
        Near(steam.Pressure,1f,.001f,"Engineer raises full pressure");
        Near(steam.Coal,99.7f,.002f,"Full power burns 0.06 coal per second");
        for(int i=0;i<200;i++)steam.Step(.05f,1f,false,1f,1f);
        Near(steam.Pressure,.55f,.001f,"Unattended pressure is capped at 55 percent");
        Check(steam.NeedsEngineer(1f,1f),"Full command requests an engineer");
        Near(steam.OutputThrottle(-1f),-.65f,.0001f,"Reverse is weaker than ahead propulsion");

        var damagedBoiler=new NavalSteamState();
        damagedBoiler.Step(1f,1f,true,.5f,1f);
        Near(damagedBoiler.Pressure,.61f,.001f,"Half boiler integrity halves pressure rise");
        var damagedFunnel=new NavalSteamState();
        damagedFunnel.Step(10f,1f,true,1f,0f);
        Near(damagedFunnel.Pressure,.20f,.001f,"Destroyed funnel limits pressure");
        Near(steam.Power(.4f),.22f,.001f,"Engine integrity scales delivered power");
        var exhausted=new NavalSteamState();exhausted.Step(2000f,1f,true,1f,1f);
        Near(exhausted.Coal,0f,.001f,"Coal cannot become negative");Near(exhausted.Pressure,0f,.001f,"No coal removes pressure");
        Near(NavalBoilerFloodMath.Flooding(NavalBoilerFloodMath.GrateHeight),0f,.0001f,"Water below the grate does not quench the fire");
        Near(NavalBoilerFloodMath.Flooding(NavalBoilerFloodMath.FireboxCrown),1f,.0001f,"Water over the firebox fully quenches combustion");
        var flooded=new NavalSteamState();for(int i=0;i<100;i++)flooded.Step(.05f,1f,true,1f,1f);
        float coalBeforeFlood=flooded.Coal;bool intactBurst=false;for(int i=0;i<100;i++)intactBurst|=flooded.StepFlooded(.05f,1f,true,1f,1f,1f);
        Check(!intactBurst&&!flooded.Ruptured,"An intact flooded boiler cannot spontaneously rupture");
        Near(flooded.Coal,coalBeforeFlood,.0001f,"A drowned firebox stops consuming coal");
        Check(flooded.LastBurnRate==0f&&flooded.Combustion==0f&&!NavalSteamSmokeMath.Emits(flooded.LastBurnRate,1f,1f),"A drowned boiler creates no new funnel smoke");
        Check(NavalSteamSmokeMath.IsExternalOutlet(false,false,2.20f,2.00f),"An intact exterior funnel cell can emit smoke");
        Check(!NavalSteamSmokeMath.IsExternalOutlet(true,false,2.20f,2.00f)&&!NavalSteamSmokeMath.IsExternalOutlet(false,true,2.20f,2.00f),"A destroyed or detached funnel mouth cannot emit from its old point");
        Check(!NavalSteamSmokeMath.IsExternalOutlet(false,false,1.90f,2.00f),"An intact uptake below deck is not a phantom exterior smoke outlet");
        Near(flooded.Pressure,.20f,.002f,"Stored steam decays rapidly after the firebox floods");
        var partialFlood=new NavalSteamState();partialFlood.StepFlooded(1f,1f,true,1f,1f,.5f);
        Check(partialFlood.LastBurnRate>0f&&partialFlood.LastBurnRate<NavalSteamState.FullBurn,"Partial flooding proportionally reduces firing and smoke");
        var weakHotBoiler=new NavalSteamState();for(int i=0;i<100;i++)weakHotBoiler.Step(.05f,1f,true,1f,1f);
        bool ruptured=false;for(int i=0;i<40&&!ruptured;i++)ruptured=weakHotBoiler.StepFlooded(.05f,1f,true,.2f,1f,1f);
        Check(ruptured&&weakHotBoiler.Ruptured&&weakHotBoiler.Pressure==0f,"A severely shell-weakened hot boiler can rupture under full quench");
        weakHotBoiler.RepairRupture();Check(!weakHotBoiler.Ruptured,"Physical boiler repair clears the latched rupture state");
        var coldDamagedBoiler=new NavalSteamState();bool coldBurst=false;for(int i=0;i<100;i++)coldBurst|=coldDamagedBoiler.StepFlooded(.05f,0f,true,.2f,1f,1f);
        Check(!coldBurst,"A low-pressure damaged boiler cannot explode merely because it is flooded");

        Near(NavalArmorMath.BlastRadius(2f,.65f),1.3f,.0001f,"Armour uses 65 percent collateral radius");
        Check(NavalArmorMath.PenetrationPower(1.45f,5f)>=NavalArmorMath.BreakWear,"A super-close rifled shell can penetrate an intact armour cell");
        Check(NavalArmorMath.PenetrationPower(1.45f,NavalArmorMath.CloseRange)>=NavalArmorMath.BreakWear,"Close-range shell penetrates one intact plate");
        Check(NavalArmorMath.PenetrationPower(1.45f,150f)>=NavalArmorMath.BreakWear,"A square medium-range casemate hit can penetrate one intact plate");
        Check(NavalArmorMath.AngledPower(NavalArmorMath.PenetrationPower(1.45f,150f),.65f)<NavalArmorMath.BreakWear,"Oblique medium-range armour still requires repeated hits");
        Check(NavalArmorMath.PenetrationPower(1.45f,230f)<NavalArmorMath.BreakWear,"Distant casemate fire still requires repeated hits");
        Check(NavalArmorMath.PenetrationPower(1.45f,180f)<NavalArmorMath.PenetrationPower(1.45f,120f),"Long-range penetration falls continuously with travelled distance");
        float wear=NavalArmorMath.AddWear(0,1);Check(!NavalArmorMath.Broken(wear),"First direct hit dents armour");
        wear=NavalArmorMath.AddWear(wear,1);Check(NavalArmorMath.Broken(wear),"Second direct hit breaks armour");

        var magazine=new NavalMagazineState();
        Check(!magazine.Impact(.5f)&&!magazine.Lit,"Sealed magazine ignores an ordinary non-penetrating impact roll");
        Check(magazine.Impact(.02f)&&magazine.Spent,"Rare direct magazine penetration detonates immediately");
        magazine.Reset();for(int i=0;i<15;i++)Check(!magazine.Step(.25f,true,false),"Magazine cannot cook off before four seconds of sustained heat");
        magazine.Step(.25f,true,false);Check(magazine.Lit&&Math.Abs(magazine.Fuse-NavalMagazineState.FuseSeconds)<.001f,"Sustained compartment fire starts an 18-second magazine warning");
        magazine.Douse();Check(magazine.Wet&&!magazine.Lit,"Flooding the magazine cancels cook-off");

        var ship=NavalShipDefinition.Ironclad;
        Check(ship.HelmDeck>ship.WeatherDeck,"Steam helmsman stands in a raised conning tower");
        Check(NavalConningTowerLayout.Inside(new NavalWalkPoint(0,ship.HelmDeck,NavalConningTowerLayout.CenterZ),ship),"Helm post is inside the conning tower");
        Check(NavalConningTowerLayout.DoorLane(new NavalWalkPoint(NavalConningTowerLayout.EntryX,ship.WeatherDeck,NavalConningTowerLayout.FrontZ+.09f),ship),"Forward tower doorway remains a traversable lane");
        Check(NavalConningTowerLayout.DoorLane(new NavalWalkPoint(-NavalConningTowerLayout.EntryX,ship.WeatherDeck,NavalConningTowerLayout.FrontZ+.09f),ship),"Both forward tower doorways are traversable lanes");
        Check(!NavalConningTowerLayout.DoorLane(new NavalWalkPoint(0,ship.WeatherDeck,NavalConningTowerLayout.FrontZ+.09f),ship),"Central armoured pier is not treated as a route");
        Check(!NavalConningTowerLayout.DoorLane(new NavalWalkPoint(NavalConningTowerLayout.HalfWidth,ship.WeatherDeck,NavalConningTowerLayout.FrontZ+.09f),ship),"Solid armour beside the doorway is not treated as a route");
        Check((NavalConningTowerLayout.DoorOuterX-NavalConningTowerLayout.DoorInnerX-.8f/ship.Scale*2f)*ship.Scale>.5f,"Each conning-tower doorway leaves generous clearance around a full sailor capsule");
        Check((NavalConningTowerLayout.SlitTop-NavalConningTowerLayout.PlatformRise)*ship.Scale>2.5f,"Conning-tower doorway clears the full runtime crew-controller height");
        Near(NavalConningTowerLayout.StairRise*ship.Scale,.12f,.0001f,"Conning-tower threshold is split into four low world-height steps");
        Check(NavalConningTowerLayout.LowerWallTop<NavalConningTowerLayout.SlitTop&&NavalConningTowerLayout.SlitTop<NavalConningTowerLayout.RoofBottom,"Segmented vision ports lie between lower and upper armour");
        Near(NavalConningTowerLayout.RoofBottom-NavalConningTowerLayout.SlitTop,.42f,.0001f,"A substantial upper belt physically supports the higher roof");
        Check(NavalConningTowerLayout.HalfWidth>=1.3f&&NavalConningTowerLayout.HalfDepth>=.7f&&NavalConningTowerLayout.RoofBottom>=1.3f,"Conning tower is enlarged in width, depth and height");
        Near(NavalConningTowerLayout.PlatformY(ship)-ship.WeatherDeck,NavalConningTowerLayout.PlatformRise,.0001f,"Command post feet and physical platform top share one source of truth");
        Check(NavalConningTowerLayout.SideDoorHalfDepth*2f*ship.Scale>2f,"Port and starboard side entrances clear a standing sailor");
        var helmRoute=NavalShipRoutes.Plan(new NavalWalkPoint(1.5f,ship.WeatherDeck,-2.8f),NavalConningTowerLayout.HelmPost(ship),ship);
        Check(helmRoute.Count>=4,"Raised conning tower route contains an exterior approach and entrance steps");
        bool usedOffsetDoor=false,reachedBehindWheel=false;
        foreach(var point in helmRoute){if(Math.Abs(Math.Abs(point.X)-NavalConningTowerLayout.EntryX)<.001f)usedOffsetDoor=true;if(Math.Abs(point.X)>.5f&&point.Z<NavalConningTowerLayout.CenterZ)reachedBehindWheel=true;}
        Check(usedOffsetDoor&&reachedBehindWheel,"Helm route uses one of the offset doors and rounds the wheel before crossing the tower");
        var helmEnd=helmRoute[helmRoute.Count-1];Near(helmEnd.X,0,.0001f,"Conning route reaches helm X");Near(helmEnd.Y,ship.HelmDeck,.0001f,"Conning route reaches raised platform");Near(helmEnd.Z,NavalConningTowerLayout.CenterZ,.0001f,"Conning route reaches helm Z");
        Check(ship.SteamPowered&&ship.Armored&&ship.Era==NavalEra.Steam,"Ironclad selects armoured steam propulsion");
        float shellWear=0;Check(!NavalArmorMath.Penetrates(shellWear,ship.ProjectilePower),"First real rifled-shell hit cannot reach the magazine through intact armour");
        shellWear=NavalArmorMath.AddWear(shellWear,ship.ProjectilePower);Check(NavalArmorMath.Penetrates(shellWear,ship.ProjectilePower),"Second real rifled-shell hit penetrates one armour cell");
        shellWear=NavalArmorMath.AddWear(shellWear,ship.ProjectilePower);Check(NavalArmorMath.Broken(shellWear),"Penetrating hit opens the armoured cell");
        Check(NavalImpactMaterial.IsMetal(false,true,false)&&NavalImpactMaterial.IsMetal(true,false,false)&&!NavalImpactMaterial.IsMetal(false,false,false),"Rifled gun and armour hits use metal marks while wood remains wood");
        Check(NavalMagazineSupport.Stable(2,5)&&!NavalMagazineSupport.Stable(1,5)&&NavalMagazineSupport.Stable(0,0),"Magazine support removes an unsupported locker without false spawn collapse");
        Check(ship.Scale==4f&&ship.GunCount==6&&ship.CrewCount==10&&ship.MastZ.Length==0,"Ironclad has six broadside guns, two main-battery sailors and two added universal hands");
        Check(ship.CrewCount-2==ship.GunCount+2,"Steam crew has one universal reserve after manning helm and all six casemates");
        Near(ship.MaxSailingSpeed,5.2f,.0001f,"Ironclad maximum speed");Near(ship.ProjectileSpeed,55f,.0001f,"Rifled gun projectile speed");
        Check(ship.RifledWeapons,"Ironclad uses long rifled naval guns");Near(ship.ProjectilePower,1.55f,.0001f,"Shell impact power");Near(ship.GunMuzzleZ,2.15f,.0001f,"Long gun muzzle position");
        Near(NavalMainBatteryMath.ReloadSeconds,20f,.0001f,"Main battery reload cycle");
        Check(NavalMainBatteryMath.Ammunition==100,"Main battery carries a full 100-round battle allowance");
        Check(NavalMainBatteryMath.WithinArc(70f)&&NavalMainBatteryMath.WithinArc(-70f)&&!NavalMainBatteryMath.WithinArc(70.1f),"Main battery has a 140-degree forward traverse sector");
        Check(NavalMainBatteryMath.MuzzleSmokeCount>=40,"Main battery emits a substantially larger muzzle cloud");
        var aimZones=new System.Collections.Generic.HashSet<string>();
        for(int zone=0;zone<NavalMainBatteryMath.AimZoneCount;zone++)
        {
            Vector3 aim=NavalMainBatteryMath.AimPoint(ship,zone,1);aimZones.Add(aim.x.ToString("F2")+":"+aim.y.ToString("F2")+":"+aim.z.ToString("F2"));
            Check(aim.x>0&&aim.x<=ship.HullHalfWidth(aim.z),"Main-battery aim zone remains on the near armour face");
        }
        Check(aimZones.Count==NavalMainBatteryMath.AimZoneCount,"Successive main-battery rounds use eight distinct hull zones");
        float[] zoneYaws={-73f,-68f,-76f,-71f,-65f,-74f,-69f,-72f};
        Check(NavalMainBatteryMath.SelectAvailableAimZone(zoneYaws,3,3)==4,"Main battery abandons a locked inaccessible zone for a visible zone inside traverse");
        zoneYaws[3]=-40f;Check(NavalMainBatteryMath.SelectAvailableAimZone(zoneYaws,3,3)==3,"An accessible locked zone stays stable while the mount trains");
        Check(NavalMainBatteryMath.AimZoneAvailable(69.5f)&&!NavalMainBatteryMath.AimZoneAvailable(69.6f),"Aim availability preserves a bounded reserve at the physical traverse stop");
        Check(NavalMainBatteryMath.CanReload(1)&&!NavalMainBatteryMath.CanReload(0),"One surviving main-battery sailor can reload");
        Check(!NavalMainBatteryMath.CanTraverse(1,10f)&&NavalMainBatteryMath.CanTraverse(1,0f),"A lone sailor cannot traverse while reloading");
        Check(NavalMainBatteryMath.CanTraverse(2,10f),"Two sailors can traverse while the second reloads");
        Check(NavalMainBatteryCrewPolicy.ReleasedHands(2,8)==0,"A healthy ten-person ironclad uses its general damage-control pool before abandoning the main battery");
        Check(NavalMainBatteryCrewPolicy.ReleasedHands(2,2)==1&&NavalMainBatteryCrewPolicy.ReleasedHands(2,1)==2,"Main-battery hands are borrowed progressively only after general-crew casualties");
        Check(NavalMainBatteryCrewPolicy.EmergencyDemand(true,false,false,false,false)==2&&NavalMainBatteryCrewPolicy.EmergencyDemand(false,false,false,false,true)==1,"Critical and urgent emergencies request the intended responder count");
        Check(!NavalMainBatteryCrewPolicy.MansMount(false)&&NavalMainBatteryCrewPolicy.MansMount(true),"A disabled main battery releases its tracked crew for repair and recalls them after restoration");
        Check(!NavalPropulsionCrewPolicy.NeedsSeparateOperator(true)&&NavalPropulsionCrewPolicy.NeedsSeparateOperator(false),"Steam helm integrates the telegraph while sailing ships retain a separate sail hand");
        Check(!NavalPropulsionCrewPolicy.ManualDismissal(true,true)&&NavalPropulsionCrewPolicy.ManualDismissal(true,false),"Manual steam orders remain at the combined helm without changing sailing-rig player takeover");
        Check(!NavalCrewToolPolicy.HammerVisible(true,true)&&NavalCrewToolPolicy.HammerVisible(false,true)&&!NavalCrewToolPolicy.HammerVisible(false,false),"Steam sailors never reveal inherited hammer models while sail-era repair tools remain compatible");
        Near(NavalFireWorkDeck.ForTarget(1.90f,ship),ship.WeatherDeck,.0001f,"Logged weather-deck fire is never claimed from the middle deck");
        Near(NavalFireWorkDeck.ForTarget(.70f,ship),.825f,.0001f,"Middle-deck fire retains a middle-deck work point");
        Check(NavalMainBatteryMath.FireproofDeckCell(1.30f,ship.WeatherDeck-.125f,3.75f,.25f,ship.WeatherDeck),"Wooden plank logged beneath the main battery belongs to the iron fireproof apron");
        Check(!NavalMainBatteryMath.FireproofDeckCell(1.80f,ship.WeatherDeck-.125f,3.75f,.25f,ship.WeatherDeck),"Fireproof apron does not suppress ordinary side-deck fires");
        Check(!NavalMainBatteryMath.FireproofDeckCell(1.0f,.70f,3.75f,.25f,ship.WeatherDeck),"Fireproof apron does not suppress fires on a lower deck");
        Check(NavalMainBatteryMath.CrewApproachZ<NavalMainBatteryMath.CrewZ,"Main-battery crew enters through the open rear before moving to its internal seat");
        Check(NavalMainBatteryMath.SteeringPriority(true,4,10f,1)&&!NavalMainBatteryMath.SteeringPriority(true,4,10.01f,1),"Helm begins a bounded main-battery alignment before reload completes");
        Check(!NavalMainBatteryMath.SteeringPriority(false,4,0,2)&&!NavalMainBatteryMath.SteeringPriority(true,0,0,2)&&!NavalMainBatteryMath.SteeringPriority(true,4,0,0),"Broken, empty or unmanned main battery cannot divert the helm");
        foreach(int batterySide in new[]{1,2})
        {
            float course=NavalMainBatteryMath.JointBatteryCourse(20f,batterySide);
            float localTarget=NavalBroadsideMath.Delta(course,20f);
            float broadsideAxis=batterySide==1?-90f:90f;
            Check(NavalMainBatteryMath.WithinArc(localTarget)&&NavalGunneryMath.WithinTraverse(NavalBroadsideMath.Delta(broadsideAxis,localTarget),0),"Joint helm course keeps main and selected casemate battery on target");
            float loadingCourse=NavalMainBatteryMath.MainBatteryCourse(20f,batterySide,NavalMainBatteryMath.SteeringLeadSeconds);
            float loadingLocalTarget=NavalBroadsideMath.Delta(loadingCourse,20f);
            float readyCourse=NavalMainBatteryMath.MainBatteryCourse(20f,batterySide,0f);
            float readyLocalTarget=NavalBroadsideMath.Delta(readyCourse,20f);
            Check(NavalMainBatteryMath.WithinArc(loadingLocalTarget)&&NavalMainBatteryMath.WithinArc(readyLocalTarget),"Dynamic main-battery courses remain inside traverse");
            Check(NavalMainBatteryMath.TraverseArc-Math.Abs(loadingLocalTarget)>=12f,"Loading course reserves traverse for target lead and hull lag");
            Check(NavalMainBatteryMath.TraverseArc-Math.Abs(readyLocalTarget)>=25f,"Ready course turns farther toward the target for a reliable shot");
            Check(Math.Abs(readyLocalTarget)<Math.Abs(loadingLocalTarget),"Helm progressively turns the bow toward the target as reload completes");
        }
        Check(NavalMainBatteryMath.RecoverySide(100f,1)==2&&NavalMainBatteryMath.RecoverySide(-100f,2)==1,
            "Main-battery recovery chooses the shorter turn when a target leaves the forward arc");
        Check(NavalMainBatteryMath.RecoverySide(179f,1)==1&&NavalMainBatteryMath.RecoverySide(-179f,2)==2,
            "Dead-astern recovery preserves its committed side across the signed-angle seam");
        float sternRecovery=NavalMainBatteryMath.MainBatteryHelmCourse(0f,180f,2,0f);
        Near(Math.Abs(NavalBroadsideMath.Delta(sternRecovery,180f)),NavalMainBatteryMath.MainBatteryReadyBearing,.0001f,
            "Dead-astern recovery demands a course well inside the traverse stop instead of stalling at its edge");
        float mainCrewRadius=.8f/ship.Scale;
        Check(NavalMainBatteryMath.SideShieldX-NavalMainBatteryMath.SideShieldThickness*.5f-NavalMainBatteryMath.CrewX>mainCrewRadius,
            "Main-battery crew posts clear the solid side shields at full native capsule radius");
        Check(NavalMainBatteryMath.CrewX*2f>mainCrewRadius*2f,"Both main-battery crew capsules fit beside one another");
        float boreClearance=ship.WeatherDeck+NavalMainBatteryMath.MountHeight+NavalMainBatteryMath.TrunnionOffset-ship.RimY;
        Check(boreClearance>.35f,"Raised main-caliber bore clears the bow bulwark: "+boreClearance);
        Check(NavalMainBatteryMath.ProjectileSpeed>ship.ProjectileSpeed&&NavalMainBatteryMath.ProjectilePower>ship.ProjectilePower*3.3f,"Main caliber uses a faster heavy armour-piercing shell");
        Check(NavalArmorMath.PenetrationPower(NavalMainBatteryMath.ProjectilePower,NavalMainBatteryMath.MaximumRange)>=NavalArmorMath.BreakWear,"Main caliber retains armour penetration at its maximum range");
        Check(NavalArmorMath.Resistance(NavalPenetrationMaterial.Armor)>NavalArmorMath.Resistance(NavalPenetrationMaterial.Iron)&&NavalArmorMath.Resistance(NavalPenetrationMaterial.Iron)>NavalArmorMath.Resistance(NavalPenetrationMaterial.Wood),"Penetration consumes energy according to struck material");
        float ordinaryArmor=NavalArmorMath.ResidualPower(ship.ProjectilePower,150f,NavalPenetrationMaterial.Armor);
        float woodenDeck=NavalArmorMath.ResidualPower(ship.ProjectilePower,150f,NavalPenetrationMaterial.Wood);
        Check(ordinaryArmor>0f&&woodenDeck>ordinaryArmor,"A square casemate AP hit perforates ordinary armour at combat range and retains more energy through a wooden deck");
        Check(NavalArmorMath.Perforates(ship.ProjectilePower,150f,NavalPenetrationMaterial.Armor)&&NavalArmorMath.Perforates(ship.ProjectilePower,150f,NavalPenetrationMaterial.Wood),"Both ordinary armour and wooden deck use the same post-perforation fuze path");
        float closeResidual=NavalArmorMath.ResidualPower(NavalMainBatteryMath.ProjectilePower,25f,NavalPenetrationMaterial.Armor);
        float longResidual=NavalArmorMath.ResidualPower(NavalMainBatteryMath.ProjectilePower,250f,NavalPenetrationMaterial.Armor);
        Check(closeResidual>longResidual&&longResidual>0f,"Heavy AP residual penetration falls with distance but still perforates at battle range");
        Check(NavalArmorMath.InternalFuseDepth(closeResidual)>NavalArmorMath.InternalFuseDepth(longResidual),"Higher residual energy carries the shell deeper before detonation");
        Check(NavalArmorMath.ArmsInternalFuse(NavalPenetrationMaterial.Armor,longResidual)&&NavalArmorMath.ArmsInternalFuse(NavalPenetrationMaterial.Wood,longResidual),"Every perforating AP shell arms after passing armour, iron, machinery or a wooden repair patch");
        Check(NavalArmorMath.ExitVelocityFactor(3f,1f,NavalPenetrationMaterial.Armor)<NavalArmorMath.ExitVelocityFactor(3f,1f,NavalPenetrationMaterial.Wood),"Denser material removes more post-penetration velocity");
        Check(NavalRepairMaterials.NeedsPlanks(true,false,false,false)&&NavalRepairMaterials.NeedsPlanks(false,true,false,false)&&NavalRepairMaterials.NeedsPlanks(false,false,true,false),"Hull, barrier and non-gun machinery repairs consume planks");
        Check(!NavalRepairMaterials.NeedsPlanks(false,false,true,true),"Gun restoration keeps its dedicated spare-parts workflow");
        Check(NavalArmorMath.InternalFuseDepth(closeResidual,NavalPenetrationMaterial.Armor)>NavalArmorMath.InternalFuseDepth(closeResidual,NavalPenetrationMaterial.Iron),"Armour resistance gives the base fuze a deeper deliberate delay than thin iron");
        Check(NavalArmorMath.MechanismArmorPower(2f,true)>NavalArmorMath.MechanismArmorPower(2f,false),"A direct main-mount hit transfers extra shock into the compact mechanism");
        Check(NavalArmorMath.InternalBlastPower(NavalMainBatteryMath.ProjectilePower,closeResidual)>NavalArmorMath.InternalBlastPower(NavalMainBatteryMath.ProjectilePower,longResidual),"Internal blast strength follows residual penetration energy");
        Check(NavalArmorMath.InternalBlastRadiusMultiplier(NavalMainBatteryMath.ProjectilePower,closeResidual)>NavalArmorMath.InternalBlastRadiusMultiplier(ship.ProjectilePower,.1f),"Main-caliber internal burst wrecks a larger compartment than a low-residual casemate shell");
        Check(Math.Abs(NavalArmorMath.InternalEncounterDistance(0f,.72f,.22f)-.50f)<.001f,"Entry sweep counts only travel behind the perforated surface");
        Check(Math.Abs(NavalArmorMath.InternalEncounterDistance(.8f,.35f,0f)-1.15f)<.001f,"Later obstruction sweeps accumulate internal travel");
        Check(NavalArmorMath.IsEntryPlateSeam(.02f,true,false)&&NavalArmorMath.IsEntryPlateSeam(.60f,true,false),"Neighbouring exterior belt cells through the full entry tolerance are not false internal armour");
        Check(!NavalArmorMath.IsEntryPlateSeam(.02f,true,true)&&!NavalArmorMath.IsEntryPlateSeam(.61f,true,false),"Internal barriers and genuinely separated armour remain real obstructions");
        Check(NavalMainMagazineBlastMath.HullRadius>NavalPowderBlastMath.HullRadius*2f&&NavalMainMagazineBlastMath.Power>NavalPowderBlastMath.Power*2f,"Main magazine detonation is catastrophic");
        Check(ship.Compartments&&ship.Partitions,"Ironclad retains compartment and partition systems");
        Near(ship.ArmorDamageScale,.65f,.0001f,"Ironclad collateral damage scale");
        Check(Math.Abs(ship.PropulsionPost.x)<BoatDimensions.HalfWidth&&Math.Abs(ship.PropulsionPost.z)<BoatDimensions.HalfLength,"Engine station is inside the hull");
        Near(ship.PropulsionPost.y,ship.HelmDeck,.0001f,"Engine telegraph shares the raised protected command platform");
        Near(ship.PropulsionDeck,ship.HelmDeck,.0001f,"Engine approach validates against the raised platform rather than the old weather-deck height");
        Check(NavalConningTowerLayout.Inside(new NavalWalkPoint(ship.PropulsionPost.x,ship.PropulsionPost.y,ship.PropulsionPost.z),ship),"Engine telegraph remains inside the armoured conning tower");
        Check(Math.Abs(ship.PropulsionPost.x)+.8f/ship.Scale<NavalConningTowerLayout.HalfWidth-.07f,"Combined helm capsule clears the conning-tower side armour");
        Check((NavalConningTowerLayout.FrontZ-ship.PropulsionPost.z)*ship.Scale>1.8f,"Engine operator stands clear of the forward door jamb");
        double commandSeparation=Math.Sqrt(ship.PropulsionPost.x*ship.PropulsionPost.x+(ship.PropulsionPost.z-NavalConningTowerLayout.CenterZ)*(ship.PropulsionPost.z-NavalConningTowerLayout.CenterZ))*ship.Scale;
        Near((float)commandSeparation,0,.0001f,"Telegraph control is integrated into the helmsman's standing post");
        Check(NavalSteamLayout.HelmWheelForward*ship.Scale>1.6f,"Ship wheel is in front of, not inside, the helmsman's capsule");
        Check(Math.Abs(NavalSteamLayout.ControlOffset(ship.Scale).x)*ship.Scale>=NavalSteamLayout.MinimumOperatorClearance,"Engine console leaves full capsule clearance for a sailor");
        Check(ship.StandbyDeck==.825f,"Steam standby duty is based below the crowded weather deck");
        int holdWatch=0,middleWatch=0;
        for(int sailor=0;sailor<8;sailor++)
        {
            Vector3 post=NavalShipDefinition.StandbyPost(ship,sailor);
            if(Math.Abs(post.y-ship.HoldFloor)<.001f)holdWatch++;if(Math.Abs(post.y-.825f)<.001f)middleWatch++;
            Check(Math.Abs(post.x)<NavalCompartmentMath.WallX-.2f,"Steam standby post remains in the clear central compartment");
            Check(post.z<NavalMainMagazineLayout.RouteAft(ship),"Steam standby post remains aft of the main magazine trunk");
        }
        Check(holdWatch==4&&middleWatch==4,"Universal steam hands are split evenly between lower and middle deck watches");
        float funnelHalf=.26f,bodyRadius=.8f/ship.Scale;
        Check(Math.Abs(NavalSteamLayout.FunnelX)-funnelHalf-bodyRadius>ship.StairWidth*.5f,"Offset funnel clears the central maintenance staircase");
        Near(NavalSteamLayout.FunnelX,NavalSteamLayout.BoilerX,.0001f,"Funnel uptake is vertically aligned with boiler room");Near(NavalSteamLayout.FunnelZ,NavalSteamLayout.BoilerZ,.0001f,"Funnel uptake reaches boiler longitudinally");
        Check(ship.HullHalfWidth(0)<2.5f&&ship.HullHalfWidth(0)/BoatDimensions.HalfLength<.42f,"Ironclad has a slender corvette beam instead of a box hull");
        Check(ship.WeatherDeck<2.1f,"Steam corvette uses lower freeboard");Near(ship.UpperStairFloor+ship.UpperDeckOffset,.825f,.0001f,"Lower freeboard retains the original middle-to-weather-deck stair flight");
        Check(ship.HullHalfWidth(ship.LadderZ)-.35f>0,"Steam sea ladder attaches to the tapered hull instead of the old sloop beam");
        Check(NavalCargoLayout.Boxes(ship,0)+NavalCargoLayout.Boxes(ship,1)+NavalCargoLayout.Boxes(ship,2)==22,"Steam hold uses 22 spaced stores instead of the former 38-crate wall");
        Check(NavalCargoLayout.IntegratedReadyUse(ship),"Steam guns use integrated ready-use lockers with no loose deck crates");
        Check(NavalFurnishingLayout.Build(ship).Count==0,"Steam holds omit solid decorative clutter from crew corridors");
        Check(NavalPowderLayout.Count(ship)==0&&NavalMagazineLayout.Count(ship)==2,"Steam ship replaces exposed powder barrels with two enclosed magazines");
        Check(NavalReadyAmmoLayout.Count(ship)==ship.GunCount,"Each steam gun has its own ready-use ammunition locker");
        for(int gun=0;gun<ship.GunCount;gun++)
        {
            float x=Math.Abs(NavalReadyAmmoLayout.X(ship,gun)),z=NavalReadyAmmoLayout.Z(ship,gun);
            Check(x+NavalReadyAmmoLayout.Width*.5f/ship.Scale<ship.HullHalfWidth(z),"Ready ammunition fits inside the armour shell");
            Check((x-NavalShipRoutes.SteamBatteryLane)*ship.Scale>NavalReadyAmmoLayout.Width*.5f+.8f,"Ready ammunition leaves a full-body battery passage");
            Check(Math.Abs(z-NavalShipDefinition.GunZ(ship,gun))*ship.Scale<3f,"Ready ammunition remains beside its assigned gun");
        }
        var ballast=NavalBallastLayout.For(ship);
        Check(ballast.Height<1f&&ballast.Width<.4f,"Steam ballast is a low double-bottom wing tank rather than a wooden deck chest");
        foreach(float z in new[]{ballast.InnerZ,ballast.Z,ballast.Z+ballast.Length*.5f})
            Check(ballast.X+ballast.Width*.5f<ship.HullHalfWidth(z),"Steam ballast remains inside the tapered armour shell");
        for(int store=0;store<NavalMagazineLayout.Count(ship);store++)
        {
            float x=NavalMagazineLayout.X(store),z=NavalMagazineLayout.Z(store);
            Check(Math.Abs(x)+NavalMagazineLayout.Width*.5f/ship.Scale<ship.HullHalfWidth(z),"Magazine locker fits inside armour shell");
            Check(x>NavalCompartmentMath.WallX,"Magazine explosive volume is behind the starboard longitudinal bulkhead");
            Check((x-NavalCompartmentMath.WallX)*ship.Scale<=.9f,"Firefighter can douse the magazine through its armoured door");
            Near(NavalMagazineLayout.Floor(ship),ship.HoldFloor,.0001f,"Magazine is on the protected hold floor");
        }
        Check(ship.HullHalfWidth(6f)<.5f&&ship.HullHalfWidth(5f)<ship.HullHalfWidth(4.5f),"Ironclad bow narrows smoothly to a fine stem");
        float gunZ=NavalShipDefinition.GunZ(ship,0),side=ship.HullHalfWidth(gunZ);
        Check(!ship.IsOpening(side,ship.GunPortSill+.1f,gunZ,.05f,.05f,.05f),"Ironclad has no window-like side opening behind the raised gun");
        for(int crate=0;crate<NavalCargoLayout.Boxes(ship,0)+NavalCargoLayout.Boxes(ship,1)+NavalCargoLayout.Boxes(ship,2);crate++)
        {
            var cargo=NavalCargoLayout.Position(ship,crate);var pickup=NavalCargoLayout.Pickup(ship,cargo);
            Check(Math.Abs(Math.Abs(pickup.X)-NavalCompartmentMath.WallX)>bodyRadius,"Steam cargo pickup stays a full body radius clear of longitudinal bulkhead");
            Check(Math.Abs(Math.Abs(cargo.X)-NavalCompartmentMath.WallX)>.525f/ship.Scale,"Steam crate no longer straddles a longitudinal bulkhead");
            if(cargo.Z< -4f)Check(Math.Abs(cargo.X)>.675f/ship.Scale+bodyRadius,"Aft stores leave the centreline pump approach open");
            Check(Math.Abs(cargo.X)<ballast.X-ballast.Width*.5f-.10f,"Shell racks do not intersect ballast tanks");
            if(cargo.Z< -4f)Check(Math.Abs(cargo.Z+3.65f)*ship.Scale>2.4f,"Aft stores leave a full straight approach to the upper stair landing");
            if(Math.Abs(cargo.Z-NavalMainMagazineLayout.Z)<=NavalMainMagazineLayout.RouteHalfDepth(ship)+.2f)
            {
                Check(Math.Abs(pickup.X)>=NavalMainMagazineLayout.RouteHalfWidth(ship)+.10f,"Magazine-side stores keep the pickup capsule clear of the armoured trunk");
                Check(NavalCrewDeckSafety.Inside(pickup.X,pickup.Z,ship,.8f),"Magazine pickup remains inside the tapered bow with a full sailor capsule");
            }
        }
        Check(NavalAmmoDispatch.BetterCandidate(2,2,8,0,true,false),"Ammo dispatch selects the emptiest working gun instead of the first gun");
        Check(!NavalAmmoDispatch.BetterCandidate(0,2,8,0,true,true),"A second runner cannot reserve the same gun");
        Check(NavalCargo.MaxAmmoRunners==2,"Two independent runners may serve different guns without exhausting the crew");
        for(int gun=0;gun<ship.GunCount;gun++)Check(ship.GunMountX<ship.HullHalfWidth(NavalShipDefinition.GunZ(ship,gun)),"Every raised gun pedestal remains inside the finer hull");
        Check((NavalShipRoutes.SteamBatteryLane-ship.GunPostX)*ship.Scale>1.9f,"Middle gunner leaves a two-body bypass to the aft battery");
        Check((ship.GunMountX-NavalShipRoutes.SteamBatteryLane)*ship.Scale>2f,"Battery bypass also clears the fixed casemate pedestal");
        for(int sailor=0;sailor<ship.CrewCount;sailor++)
        {
            Vector3 rest=NavalShipDefinition.StandbyPost(ship,sailor);
            var start=new NavalWalkPoint(rest.x,rest.y,rest.z);
            var route=NavalSupplyRoutes.Plan(start,new NavalWalkPoint(ship.PropulsionPost.x,ship.PropulsionPost.y,ship.PropulsionPost.z),ship);
            Check(route.Count>0,"Every steam sailor receives an engine-room route");
            bool reachesWeather=false;foreach(var point in route)if(point.Y>ship.WeatherDeck-.05f)reachesWeather=true;
            Check(reachesWeather,"Lower-deck steam watch can reach the weather-deck telegraph");
            var end=route[route.Count-1];Near(end.X,ship.PropulsionPost.x,.0001f,"Engine route X");Near(end.Y,ship.PropulsionPost.y,.0001f,"Engine route deck");Near(end.Z,ship.PropulsionPost.z,.0001f,"Engine route Z");
        }
        Check(NavalSteamSmokeMath.Density(NavalSteamState.FullBurn,1f)>NavalSteamSmokeMath.Density(NavalSteamState.FullBurn,.25f),"Smoke density responds to pressure");
        Check(NavalGunneryMath.LevelRangeFor(ship.ProjectileSpeed)>NavalGunneryMath.LevelRange*2f,"Rifled gun range exceeds round-shot range");
        Check(NavalGunneryMath.FlightTime(100f,10f,ship.ProjectileSpeed)<NavalGunneryMath.FlightTime(100f,10f),"Rifled shell lead uses its actual velocity");
        for(int gun=0;gun<ship.GunCount;gun+=2)Check(NavalShipDefinition.GunSide(gun)<0&&NavalShipDefinition.GunSide(gun+1)>0,"Each casemate position has a port/starboard pair");
        NavalSessionSettings.Configure(NavalEra.Steam);
        Check(NavalSessionSettings.PlayerShip==ship&&NavalSessionSettings.EnemyShip==ship&&NavalSessionSettings.Choices.Length==1,"Steam map selects mirrored ironclads");
        Near(NavalSessionSettings.EnemySpawnDistance,800f,.0001f,"Steam ironclads start about eight hundred metres apart");
        Check(NavalSessionSettings.EnemySpawnDistance>NavalMainBatteryMath.MaximumRange,
            "Steam start requires an approach before either main battery can fire");
        NavalSessionSettings.Configure(NavalEra.Sail);
        Check(!NavalSessionSettings.PlayerShip.SteamPowered&&!NavalSessionSettings.EnemyShip.SteamPowered&&NavalSessionSettings.Choices.Length==3,"Sail map restores sail roster");
        Near(NavalSessionSettings.EnemySpawnDistance,250f,.0001f,"Sail-era starting distance remains unchanged");

        Console.WriteLine("PASS: v232 era markers, steam state, machinery damage, armour, enclosed magazines and mirrored ironclad profile.");
    }
}
