using System;
using System.Collections.Generic;
using UnityEngine;

// One physical access description drives shell openings, stairs and crew motion.
public static class NavalTurretAccessLayout
{
    public const float RootLift=.14f,Floor=.04f,RoofBottom=.54f,RoofTop=.60f;
    public const float BarrelAxis=.32f,MaximumPitch=25f;
    public const float DoorWidth=.36f,DoorHeight=.48f,Wall=.06f;
    public static Vector3 Seat {get{return new Vector3(0,Floor,-.13f);}}
    // A and X: right-side hatch. P/Q: rear hatch. Y: left-side hatch.
    public static int Side(int slot){return slot==0||slot==3?1:slot==4?-1:0;}
    public static Vector3 Out(int slot){return Side(slot)==0?Vector3.back:new Vector3(Side(slot),0,0);}
    public static Vector3 Door(int slot){return Side(slot)==0?new Vector3(0,Floor,-.36f):new Vector3(Side(slot)*.43f,Floor,0);}
    public static float GroundDistance(int slot){return slot==3?1.40f:slot==0?.90f:1.05f;}
    public static Vector3 ToShip(NavalShipDefinition ship,int slot,Vector3 p){Vector3 mount=NavalCapitalLayout.Mount(slot);mount.y=ship.WeatherDeck+NavalCapitalLayout.TurretRise(slot)+RootLift;float angle=NavalCapitalLayout.BaseYaw(slot)*(float)Math.PI/180f;float sn=(float)Math.Sin(angle),cs=(float)Math.Cos(angle);return mount+new Vector3(p.x*cs+p.z*sn,p.y,-p.x*sn+p.z*cs);}
    public static Vector3 Approach(NavalShipDefinition ship,int slot){Vector3 p=Out(slot)*(GroundDistance(slot)+.20f);p.y=-NavalCapitalLayout.TurretRise(slot)-RootLift;return ToShip(ship,slot,p);}
    public static List<Vector3> Entry(NavalShipDefinition ship,int slot){
        var points=new List<Vector3>();Vector3 axis=Out(slot),door=Door(slot);float edge=Side(slot)==0?.36f:.43f,outer=GroundDistance(slot);
        float rise=NavalCapitalLayout.TurretRise(slot)+RootLift+Floor;int count=Math.Max(4,(int)Math.Ceiling(rise*ship.Scale/.14f));
        for(int i=1;i<=count;i++){float t=(float)i/count;Vector3 p=axis*(outer+(edge-outer)*t);p.y=-NavalCapitalLayout.TurretRise(slot)-RootLift+rise*t;points.Add(ToShip(ship,slot,p));}
        Vector3 inside=door-axis*.20f;points.Add(ToShip(ship,slot,inside));points.Add(ToShip(ship,slot,Seat));return points;
    }
    public static bool FitsCapsule(float radius,float height,float scale){return DoorWidth*scale>2*radius+.12f&&(RoofBottom-Floor)*scale>height+.15f;}
}
