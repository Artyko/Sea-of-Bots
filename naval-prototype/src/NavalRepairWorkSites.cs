using System;
// Shared immutable tables: callers keep the original deck/end/corridor/shift loop order.
public static class NavalRepairWorkSites
{
    private static readonly float[] shifts={0f,-.35f,.35f,-.7f,.7f,-1.2f,1.2f};
    private static readonly float[] corridors={2.35f,2.1f,1.65f,.95f};
    // The first steam work lane runs over the low wing tanks. It brings the
    // sailor close enough to reach the complete lower armour belt; routing
    // already climbs the tank ramps and Surface() places the feet on its lid.
    // The outer lane is the unobstructed service alley between the armour
    // shell and the longitudinal cargo/partition line.  Lower side holes can
    // be unreachable from x=1.55 when a wing tank or store occupies that
    // point, even though a full sailor capsule fits safely at x=1.90.
    private static readonly float[] steamCorridors={1.90f,1.55f,1.18f,.90f};
    public static int ShiftCount {get{return shifts.Length;}}
    public static int CorridorCount {get{return corridors.Length;}}
    public static float Shift(int index){return shifts[index];}
    public static float Corridor(int index){return corridors[index];}
    public static float Corridor(NavalShipDefinition ship,int index)
    {return ship.SteamPowered?steamCorridors[index]:corridors[index];}
    public static bool ClearOfCasemate(NavalShipDefinition ship,float floor,float z)
    {
        if(!ship.SteamPowered||Math.Abs(floor-ship.WeatherDeck)>.12f)return true;
        // The gun post itself is occupied by a gunner, but crew colliders are
        // not part of the static ClearBody query.  Never select a repair stand
        // inside that occupied bay: approach the same plate from the fore/aft
        // edge, where the sailor can work around the open-backed shield.
        for(int gun=0;gun<ship.GunCount;gun+=2)
            if(Math.Abs(z-NavalShipDefinition.GunZ(ship,gun))<.62f)return false;
        return true;
    }
    public const int SteamBowBeltCandidateCount=9;
    public static bool SteamBowBelt(NavalShipDefinition ship,float holeX,float holeY,float holeZ,int candidate,out float x,out float y,out float z)
    {
        x=y=z=0f;
        // The forward casemate, taper and main-magazine trunk leave one
        // repeatable service pocket on the middle deck. Search it before the
        // generic corridor table so a high-flow bow-belt hole cannot be
        // starved by points which are individually valid but physically
        // separated from the plate by the gun foundation or a store.
        if(!ship.SteamPowered||candidate<0||candidate>=SteamBowBeltCandidateCount||Math.Abs(holeX)<1.25f||holeZ<3.55f||holeZ>4.75f||holeY<.70f||holeY>1.65f)return false;
        float side=holeX<0f?-1f:1f;
        if(candidate==0){x=side*1.38f;y=.825f;z=4.05f;}
        else if(candidate==1){x=side*1.45f;y=.825f;z=3.70f;}
        else if(candidate==2){x=side*1.18f;y=.825f;z=3.78f;}
        else if(candidate==3){x=side*1.38f;y=ship.WeatherDeck;z=4.15f;}
        else if(candidate==4){x=side*.95f;y=.825f;z=4.05f;}
        // Independent approaches outside the main-magazine apron.  A heavy
        // magazine event can remove the original z=3.70..4.15 support cells;
        // these aft/outer points still reach the belt without standing in the
        // blast crater or in the forward casemate foundation.
        else if(candidate==5){x=side*1.70f;y=.825f;z=3.45f;}
        else if(candidate==6){x=side*1.72f;y=.825f;z=3.82f;}
        else if(candidate==7){x=side*1.55f;y=ship.WeatherDeck;z=3.72f;}
        else{x=side*1.62f;y=ship.WeatherDeck;z=4.05f;}
        return true;
    }
    public static bool SteamBowBelt(NavalShipDefinition ship,float holeX,float holeY,float holeZ,out float x,out float y,out float z)
    {return SteamBowBelt(ship,holeX,holeY,holeZ,0,out x,out y,out z);}
    public const int SteamSternBeltCandidateCount=3;
    public static bool SteamSternBelt(NavalShipDefinition ship,float holeX,float holeY,float holeZ,int candidate,out float x,out float y,out float z)
    {
        x=y=z=0f;
        // At the fine stern the generic z=-5.10 stand is squeezed between
        // the tapered shell and the two aft stores. Approach the inner face
        // from the open side aisle ahead of those racks instead. These three
        // supported points retain reach to the logged (1.67,1.11,-5.63) seam.
        if(!ship.SteamPowered||candidate<0||candidate>=SteamSternBeltCandidateCount||Math.Abs(holeX)<1.0f||holeZ> -5.35f||holeY<.70f||holeY>1.70f)return false;
        float side=holeX<0f?-1f:1f;
        if(candidate==0){x=side*1.70f;y=.825f;z=-4.85f;}
        else if(candidate==1){x=side*1.45f;y=.825f;z=-4.75f;}
        else{x=side*1.70f;y=.825f;z=-5.05f;}
        return true;
    }
    public static float Floor(float preferred,float bottom,int deck){return deck==0?preferred:preferred>.3f?bottom:.825f;}
    public static bool EndWall(float z){return Math.Abs(z)>BoatDimensions.HalfLength-.3f;}
    public static float EndZ(NavalShipDefinition ship,float z)
    {
        // At z=5.05 a full-size capsule's forward shoulder reaches the much
        // narrower z=5.25 outline. Move the steam end-wall post slightly aft;
        // it remains within hand reach of the inner stem face.
        return (z<0?-1f:1f)*(ship.SteamPowered?4.85f:5.5f);
    }
}
