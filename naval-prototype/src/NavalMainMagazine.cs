using System.Collections.Generic;
using UnityEngine;

public static class NavalMainMagazineLayout
{
    public const float Z=4.05f,Width=3.2f,Depth=3.6f,HitRadius=1.8f;
    public const float RouteClearance=.55f;
    // Full-size crew capsules must route around the armoured trunk, not merely
    // around its thin wall voxels. Values are returned in ship-local units.
    public static float RouteHalfWidth(NavalShipDefinition ship){return (Width*.5f+RouteClearance)/ship.Scale;}
    public static float RouteHalfDepth(NavalShipDefinition ship){return (Depth*.5f+RouteClearance)/ship.Scale;}
    public static float RouteAft(NavalShipDefinition ship){return Z-RouteHalfDepth(ship);}
    public static float RouteForward(NavalShipDefinition ship){return Z+RouteHalfDepth(ship);}
}

public static class NavalMainMagazineBlastMath
{
    public const float HullRadius=15f,RobotRadius=12f,Power=7.5f;
    public static bool HitsRobot(float squaredDistance){return squaredDistance<=RobotRadius*RobotRadius;}
}

public sealed partial class NavalFireSystem
{
    private NavalFireTarget mainBatteryMagazine;
    public bool MainBatteryMagazineFunctional
    {
        get
        {
            if(mainBatteryMagazine==null||mainBatteryMagazine.Powder.Spent||mainBatteryMagazine.Collapsed)return false;
            int intact=0;foreach(var cell in mainBatteryMagazine.PowderCells)if(cell!=null&&!cell.Missing&&!cell.Detached)intact++;
            return intact*2>=mainBatteryMagazine.PowderCells.Count;
        }
    }
    private void BuildMainBatteryMagazine()
    {
        if(!hull.Definition.SteamPowered)return;
        Material armor=magazineMaterials[0],edge=magazineMaterials[1],brass=magazineMaterials[2];
        float scale=hull.WorldScale,w=NavalMainMagazineLayout.Width/scale,d=NavalMainMagazineLayout.Depth/scale;
        float floor=hull.Definition.HoldFloor,top=hull.Definition.WeatherDeck-.04f/scale,h=top-floor,t=.14f/scale;
        Vector3 centre=new Vector3(0,floor+h*.5f,NavalMainMagazineLayout.Z);
        int first=hull.Voxels.Count;
        // Continuous armoured hoist trunk: the magazine is visibly and
        // physically joined to the turret above instead of being a loose box.
        MagazineBox(armor,centre+new Vector3(-w*.5f+t*.5f,0,0),new Vector3(t,h,d));
        MagazineBox(armor,centre+new Vector3(w*.5f-t*.5f,0,0),new Vector3(t,h,d));
        MagazineBox(armor,centre+new Vector3(0,0,-d*.5f+t*.5f),new Vector3(w,h,t));
        MagazineBox(armor,centre+new Vector3(0,0,d*.5f-t*.5f),new Vector3(w,h,t));
        MagazineBox(edge,new Vector3(0,floor+t*.5f,NavalMainMagazineLayout.Z),new Vector3(w,t,d));
        MagazineBox(edge,new Vector3(0,top-t*.5f,NavalMainMagazineLayout.Z),new Vector3(w,t,d));
        for(int level=0;level<3;level++)for(int side=-1;side<=1;side+=2)
            ReadyAmmoBox(brass,new Vector3(side*w*.22f,floor+(level+.65f)*h/3f,NavalMainMagazineLayout.Z),new Vector3(.16f/scale,.55f/scale,.65f/scale),false,false);
        Vector3 point=centre;
        GameObject warning=new GameObject("Main battery magazine fuse sound");warning.transform.SetParent(transform,false);warning.transform.localPosition=point;
        AudioSource audio=warning.AddComponent<AudioSource>();audio.clip=hiss;audio.loop=true;audio.playOnAwake=false;audio.spatialBlend=1;audio.minDistance=3;audio.maxDistance=55;audio.volume=.48f;
        mainBatteryMagazine=new NavalFireTarget{Cell=hull.Voxels[first],Powder=new NavalMagazineState(),PowderPoint=point,FuseSound=audio,
            ExplosiveHeight=h*scale,ExplosiveRadius=NavalMainMagazineLayout.HitRadius,PowderCells=hull.Voxels.GetRange(first,hull.Voxels.Count-first),MainMagazine=true};
        powder.Add(mainBatteryMagazine);
    }
}
