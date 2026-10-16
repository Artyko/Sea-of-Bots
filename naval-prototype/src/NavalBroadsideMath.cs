public static class NavalBroadsideMath
{
    // 1 = port, 2 = starboard. Keep a usable current side instead of oscillating.
    public static int CombatSide(int current,int portGuns,int starboardGuns)
    {
        if(current==1&&portGuns==0&&starboardGuns>0)return 2;
        if(current==2&&starboardGuns==0&&portGuns>0)return 1;
        return current;
    }
    // cross(up, towardTarget) exposes port; its negative exposes starboard.
    public static float OrbitSign(int portGuns,int starboardGuns,float preferred)
    {
        if(portGuns>0&&starboardGuns==0)return 1f;
        if(starboardGuns>0&&portGuns==0)return -1f;
        return preferred;
    }
}
