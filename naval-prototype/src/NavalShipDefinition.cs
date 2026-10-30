// Shared mechanics consume a ship definition. New ship classes provide data,
// not copies of flooding, crew, weapons or damage implementations.
using UnityEngine;

public sealed class NavalShipDefinition
{
    public readonly string Name;
    public readonly float Scale,LadderZ,StairWidth,HoldFloor,MaxSailingSpeed,ProjectileSpeed,ProjectilePower,ArmorDamageScale,GunMuzzleZ;
    public readonly NavalEra Era;
    public readonly NavalPropulsionKind PropulsionKind;
    public readonly bool Armored;
    public readonly bool RifledWeapons;
    public bool SteamPowered {get{return PropulsionKind==NavalPropulsionKind.Steam;}}
    // The long, heavy ironclad cannot pivot like a short wooden sloop. At full
    // speed this limits it to roughly seven degrees per second instead of 15.6.
    public float SteeringRateScale {get{return SteamPowered?.45f:1f;}}
    public float HoldClearance {get{return (.575f-HoldFloor)*Scale;}}
    public float HullY(float y){return y>=.575f?AboveDeckY(y):.575f+(y-.575f)*(.575f-HoldFloor)/(.575f-BoatDimensions.HoldFloor);}
    public float FixtureScale {get{return 2f/Scale;}}
    public float AboveDeckY(float y){return y<=.825f?y:.825f+(y-.825f)*FixtureScale;}
    public float MiddleRimY {get{return AboveDeckY(BoatDimensions.Rim);}}
    public float GunDeck {get{return DeckCount>2?WeatherDeck:.825f;}}
    public float GunDeckOffset {get{return GunDeck-.825f;}}
    public float GunPortSill {get{return AboveDeckY(BoatDimensions.CannonPortSill)+GunDeckOffset;}}
    public float GunDeckRimY {get{return MiddleRimY+GunDeckOffset;}}
    public float DesignWaterline {get{return DeckCount>2?.825f+HoldClearance/(2*Scale)-1.1f/Scale:0;}}
    // Height above the lower hold floor. Taller rigs and top-deck guns raise the dry weight centre.
    // These are design assumptions for our fictional hulls, not historical measured ship data.
    public int MaintenanceDeckCount {get{return DeckCount>2?2:1;}}
    public float MaintenanceFloor(int index){return index==0?HoldFloor:.825f;}
    public bool MaintenanceFloorAt(float y){return System.Math.Abs(y-HoldFloor)<.15f||DeckCount>2&&System.Math.Abs(y-.825f)<.15f;}
    public float DryCenterHeight {get{return HoldClearance*(DeckCount>2?.65f:GunCount>2?.55f:.50f);}}
    public NavalStabilityMath CreateStability()
    {return new NavalStabilityMath(Scale,HoldFloor,RimY,NavalStabilityMath.DefaultGMFraction,DesignWaterline,HoldFloor*Scale+DryCenterHeight);}
    public float HullHalfWidth(float z){return NavalHullOutline.HalfWidth(z,DeckCount>2,SteamPowered);}
    public float RimY {get{return MiddleRimY+UpperDeckOffset;}}
    public readonly int DeckCount;
    public float UpperDeckOffset {get{return SteamPowered?1.20f:DeckCount>2?HoldClearance/Scale+.25f:0;}}
    // Artificial lower coordinate used by the reusable stair generator before
    // UpperDeckOffset is applied.  It keeps the upper flight exactly between
    // the middle and weather decks even when their spacing differs.
    public float UpperStairFloor {get{return .825f-UpperDeckOffset;}}
    public float MastFootY {get{return WeatherDeck-.25f/Scale;}}
    public float MastFootWidth {get{return .65f;}}
    public float WeatherDeck {get{return .825f+UpperDeckOffset;}}
    public float GunFixtureScale {get{return DeckCount>2?FixtureScale:1f;}}
    // Raised broadside mounts sit close to the armour belt.  This leaves a
    // genuine two-body passage between each gun and its operator instead of
    // forcing aft-bound crew through the middle gunner's capsule.
    public float GunMountX {get{return SteamPowered?2.05f:DeckCount>2?3f-.75f*FixtureScale:2.25f;}}
    public float GunPostX {get{return SteamPowered?.92f:DeckCount>2?GunMountX-.9f*FixtureScale:1.35f;}}
    public float GunMountY {get{return RifledWeapons?RimY-.18f:DeckCount>2?AboveDeckY(1f)+GunDeckOffset:1f;}}
    public float BoardingY {get{return DeckCount>2?WeatherDeck+.875f:AboveDeckY(2.55f);}}
    public float HelmDeck {get{return SteamPowered?NavalConningTowerLayout.PlatformY(this):WeatherDeck;}}
    public float PropulsionDeck {get{return SteamPowered?HelmDeck:WeatherDeck;}}
    public Vector3 PropulsionPost {get{return SteamPowered?NavalSteamLayout.OperatorPost(this):new Vector3(NavalSailWorkLayout.PostX,WeatherDeck,NavalSailWorkLayout.PostZ);}}
    public float StandbyDeck {get{return SteamPowered?.825f:HoldFloor;}}
    public float SailSize(int index){return DeckCount>2?(index==0?1f:index==1?.82f:.65f):1f;}
    public float RepairFloor(float preferred,int index)
    {if(preferred>.825f+.1f)return index==0?WeatherDeck:index==1?.825f:HoldFloor;return index<2?NavalRepairWorkSites.Floor(preferred,HoldFloor,index):WeatherDeck;}
    public float RepairAimY(float holeY,float standY)
    {
        float aim=RepairDeckEdge(holeY,standY,.825f);
        return DeckCount>2?RepairDeckEdge(aim,standY,WeatherDeck):aim;
    }
    private float RepairDeckEdge(float holeY,float standY,float deck)
    {
        // Aim at the exposed face of a seam, on the sailor's side of the slab.
        float margin=.15f/Scale;
        if(holeY<deck-.25f-margin||holeY>deck+margin)return holeY;
        return standY>=deck-margin?System.Math.Max(holeY,deck+.25f/Scale):
            System.Math.Min(holeY,deck-.25f-.25f/Scale);
    }
    public readonly int GunCount,CrewCount;
    public readonly float[] MastZ;
    public readonly bool Compartments;
    public readonly bool Partitions;
    // Three-deck hull has reserve volume up to its raised intact rim, not the old sloop limit.
    public float SinkingVolume {get{return DeckCount>2?(RimY-DesignWaterline)*BoatDimensions.DisplacementArea*VolumeScale:BoatDimensions.SinkVolume*VolumeScale;}}
    public float AreaScale {get{return Scale*Scale;}}
    public float VolumeScale {get{return Scale*Scale*Scale;}}
    public NavalShipDefinition(string name,float scale,int guns,int crew,float[] masts,bool compartments,float ladderZ=0f,float stairWidth=1.1f,float holdFloor=BoatDimensions.HoldFloor,bool partitions=false,int decks=2,float maxSpeed=4f,NavalPropulsionKind propulsion=NavalPropulsionKind.Sail,bool armored=false,float projectileSpeed=32f,float armorDamageScale=1f,bool rifledWeapons=false,float projectilePower=1f,float gunMuzzleZ=1.38f)
    {Name=name;Scale=scale;MaxSailingSpeed=maxSpeed;GunCount=guns;CrewCount=crew;MastZ=masts;Compartments=compartments;Partitions=partitions;DeckCount=decks;LadderZ=ladderZ;StairWidth=stairWidth;HoldFloor=holdFloor;PropulsionKind=propulsion;Era=propulsion==NavalPropulsionKind.Steam?NavalEra.Steam:NavalEra.Sail;Armored=armored;ProjectileSpeed=projectileSpeed;ArmorDamageScale=armorDamageScale;RifledWeapons=rifledWeapons;ProjectilePower=projectilePower;GunMuzzleZ=gunMuzzleZ;}
    public static readonly NavalShipDefinition Sloop=new NavalShipDefinition("Sloop",2f,2,4,new float[]{1.6f},true,3.8f,1.1f,maxSpeed:4.8f);
    public static readonly NavalShipDefinition Brigantine=new NavalShipDefinition("Brigantine",3f,4,5,new float[]{1.6f,-3.8f},true,0f,1.1f,.575f-Sloop.HoldClearance/3f,maxSpeed:4.2f);
    public static readonly NavalShipDefinition Galleon=new NavalShipDefinition("Galleon",4f,6,6,new float[]{1.6f,4.6f,-4.3f},true,2f,1.1f,.575f-Brigantine.HoldClearance/4f,false,3,3.6f);
    // Ten hands: two remain the dedicated main-battery crew and eight are
    // universal.  At full action the helm plus six casemates still leave only
    // one reserve sailor, so damage control remains a real staffing choice.
    public static readonly NavalShipDefinition Ironclad=new NavalShipDefinition("Ironclad Corvette",4f,6,10,new float[0],true,2f,1.1f,.575f-Brigantine.HoldClearance/4f,true,3,5.2f,NavalPropulsionKind.Steam,true,55f,.65f,true,1.55f,2.15f);
    public static readonly NavalShipDefinition[] Choices={Sloop,Brigantine,Galleon};
    public static readonly NavalShipDefinition[] SteamChoices={Ironclad};
    public static float GunZ(NavalShipDefinition ship,int gun){return ship.SteamPowered?(gun/2==0?-3.2f:gun/2==1?0f:3.0f):ship.GunCount>4?(gun/2==0?-3.8f:gun/2==1?0f:3.8f):GunZ(gun);}
    public static float GunZ(int gun){return gun<2?-1f:3.3f;}
    public static float GunSide(int gun){return gun%2==0?-1f:1f;}
    public float GunPortTop {get{return GunDeckRimY+(DeckCount>2?.4f:0);}}
    public bool IsOpening(float x,float y,float z,float hx,float hy,float hz)
    {
        if(DeckCount>2)
        {
            if(System.Math.Abs(x)+hx>2.7f&&System.Math.Abs(z-LadderZ)<.65f+hz&&y+hy>WeatherDeck&&y-hy<WeatherDeck+1.4f)return true;
            if(y-hy>GunPortTop||y+hy<=GunPortSill||System.Math.Abs(x)+hx<=2.7f)return false;
            if(RifledWeapons)return false;
            float portHalf=1.1f;
            for(int gun=0;gun<GunCount;gun+=2)if(System.Math.Abs(z-GunZ(this,gun))<portHalf+hz)return true;
            return false;
        }
        for(int gun=0;gun<GunCount;gun+=2)if(BoatDimensions.IntersectsCannonPort(x,y,z-GunZ(this,gun)-1,hx,hy,hz))return true;
        return false;
    }
    public static float StandbyX(int sailor){return sailor%2==0?-1.65f:1.65f;}
    public static float StandbyX(NavalShipDefinition ship,int sailor){if(!ship.SteamPowered)return ship.Scale<=2?(sailor%2==0?-.1f:.1f):StandbyX(sailor);return sailor%2==0?-.58f:.58f;}
    public static float StandbyZ(int sailor){return 4.9f-sailor/2*.9f;}
    public static float StandbyZ(NavalShipDefinition ship,int sailor){if(!ship.SteamPowered)return StandbyZ(sailor);return sailor%4<2?1.72f:2.47f;}
    // Idle steam hands wait in two machinery-deck watches instead of forming
    // a crowd between the casemate crews. The centre aisle positions lie
    // between shell-rack rows and stay clear of the armoured magazine trunk.
    public static float StandbyY(NavalShipDefinition ship,int sailor)
    {return !ship.SteamPowered?ship.StandbyDeck:(sailor%8<4?ship.HoldFloor:.825f);}
    public static Vector3 StandbyPost(NavalShipDefinition ship,int sailor)
    {return new Vector3(StandbyX(ship,sailor),StandbyY(ship,sailor),StandbyZ(ship,sailor));}
    public static float HelmZ(NavalShipDefinition ship){return ship.SteamPowered?NavalConningTowerLayout.CenterZ:ship.MastZ.Length>1?-5.45f:-4.8f;}
}
