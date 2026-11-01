using System;
public static class NavalSpatialBatching
{
    // Steam hulls contain roughly twice the linear voxel density of the sail
    // profiles. Larger exact chunks reduce renderers and collider actors.
    public static float ChunkSize(NavalShipDefinition definition)
    {return definition!=null&&definition.SteamPowered?4f:2f;}
    // Keep dense rendering chunks large, but bound physics rebuild work. The
    // v265 late-battle log measured a 28.10 ms rebuild of one 4-unit damaged
    // chunk and 9.03 ms/frame in ColliderChunks. Two-unit physics chunks keep
    // the same exact boxes while making each damage rebuild much smaller.
    public static float ColliderChunkSize(NavalShipDefinition definition)
    {return 2f;}
    public static int ColliderChunk(float coordinate,float size,bool centered)
    {return (int)Math.Floor((coordinate+(centered?size*.5f:0f))/size);}
}
