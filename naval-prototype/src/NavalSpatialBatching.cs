using System;
public static class NavalSpatialBatching
{
    // Steam hulls contain roughly twice the linear voxel density of the sail
    // profiles. Larger exact chunks reduce renderers and collider actors.
    public static float ChunkSize(NavalShipDefinition definition)
    {return definition!=null&&definition.SteamPowered?4f:2f;}
    // Physics chunks may be larger than render chunks: they contain no mesh
    // buffers and are rebuilt only after damage. Centering the steam chunk on
    // the hull removes artificial seams at x/y/z=0, allowing whole deck and
    // bulkhead rows to become one exact compound shape.
    public static float ColliderChunkSize(NavalShipDefinition definition)
    {return definition!=null&&definition.SteamPowered?4f:2f;}
    public static int ColliderChunk(float coordinate,float size,bool centered)
    {return (int)Math.Floor((coordinate+(centered?size*.5f:0f))/size);}
}
