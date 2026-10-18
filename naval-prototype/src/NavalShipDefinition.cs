// Shared mechanics consume a ship definition. New ship classes provide data,
// not copies of flooding, crew, weapons or damage implementations.
public sealed class NavalShipDefinition
{
    public readonly string Name;
    public readonly float Scale,LadderZ,StairWidth,HoldFloor;
    public float HoldClearance {get{return (.575f-HoldFloor)*Scale;}}
    public float HullY(float y){return y>=.575f?AboveDeckY(y):.575f+(y-.575f)*(.575f-HoldFloor)/(.575f-BoatDimensions.HoldFloor);}
    public float FixtureScale {get{return 2f/Scale;}}
    public float AboveDeckY(float y){return y<=.825f?y:.825f+(y-.825f)*FixtureScale;}
    public float RimY {get{return AboveDeckY(BoatDimensions.Rim);}}
    public readonly int GunCount,CrewCount;
    public readonly float[] MastZ;
    public readonly bool Compartments;
    public readonly bool Partitions;
    public float AreaScale {get{return Scale*Scale;}}
    public float VolumeScale {get{return Scale*Scale*Scale;}}
    public NavalShipDefinition(string name,float scale,int guns,int crew,float[] masts,bool compartments,float ladderZ=0f,float stairWidth=1.1f,float holdFloor=BoatDimensions.HoldFloor,bool partitions=false)
    {Name=name;Scale=scale;GunCount=guns;CrewCount=crew;MastZ=masts;Compartments=compartments;Partitions=partitions;LadderZ=ladderZ;StairWidth=stairWidth;HoldFloor=holdFloor;}
    public static readonly NavalShipDefinition Sloop=new NavalShipDefinition("Sloop",2f,2,2,new float[]{1.6f},true,3.8f,1.1f);
    public static readonly NavalShipDefinition Brigantine=new NavalShipDefinition("Brigantine",3f,4,4,new float[]{1.6f,-3.8f},true,0f,1.1f,.575f-Sloop.HoldClearance/3f);
    public static float GunZ(int gun){return gun<2?-1f:3.3f;}
    public static float GunSide(int gun){return gun%2==0?-1f:1f;}
    public static float HelmZ(NavalShipDefinition ship){return ship.MastZ.Length>1?-5.45f:-4.8f;}
}
