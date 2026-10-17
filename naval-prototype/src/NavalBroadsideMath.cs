public static class NavalBroadsideMath
{
    // 1 = port, 2 = starboard. Prefer the stronger working side; ties keep the current side.
    public static int CombatSide(int current,int portGuns,int starboardGuns)
    {
        if(portGuns>starboardGuns)return 1;
        if(starboardGuns>portGuns)return 2;
        return current;
    }
    // cross(up, towardTarget) exposes port; its negative exposes starboard.
    public static float OrbitSign(int portGuns,int starboardGuns,float preferred)
    {
        if(portGuns>starboardGuns)return 1f;
        if(starboardGuns>portGuns)return -1f;
        return preferred;
    }
}
