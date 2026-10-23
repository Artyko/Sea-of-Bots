public static class BoatDimensions
{
    public const float WorldScale = 2f;
    public const float AreaScale = WorldScale * WorldScale;
    public const float VolumeScale = AreaScale * WorldScale;
    public const float HalfWidth = 3f;
    public const float HalfLength = 6f;
    public const float Rim = 1.775f;
    public const float HoldFloor = -2.225f;
    public const float HoldWidth = 5.75f;
    public const float HoldLength = 11.75f;
    public const float HoldCenterZ = 0f;
    public const float HoldArea = HoldWidth * HoldLength;
    public const float HalfHoldArea = HoldArea / 2f;
    public const float DisplacementArea = 72f;
    public const float SinkVolume = HoldArea * 0.8f;
    public const float PumpRate = 1.6f;
    public const float CannonPortSill = .925f;
    public static bool IntersectsCannonPort(float x, float y, float z, float halfX, float halfY, float halfZ)
    {
        return System.Math.Abs(x) + halfX > 2.7f && y + halfY > CannonPortSill &&
            System.Math.Abs(z + 1f) < 1.1f + halfZ;
    }
}
