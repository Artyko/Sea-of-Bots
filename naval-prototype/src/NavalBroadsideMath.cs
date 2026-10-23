public static class NavalBroadsideMath
{
    // 1 = port, 2 = starboard. Keep firing from the exposed side while any gun survives.
    public static int CombatSide(int current,int portGuns,int starboardGuns)
    {
        if(current==1&&portGuns>0||current==2&&starboardGuns>0)return current;
        if(portGuns>0)return 1;
        if(starboardGuns>0)return 2;
        return current;
    }
    public static float Delta(float from,float to)
    {float d=(to-from)%360f;if(d>180f)d-=360f;if(d< -180f)d+=360f;return d;}
    public static float Heading(float targetBearing,int side)
    {return targetBearing+(side==1?90f:-90f);}
    public static int HelmSide(float heading,float targetBearing,int port,int starboard,int previous,int firing)
    {
        // Navigation seeks the stronger battery; CombatSide independently keeps
        // gunners shooting the currently exposed side until the turn is complete.
        if(port>starboard)return 1;
        if(starboard>port)return 2;
        if(previous==1&&port>0||previous==2&&starboard>0)return previous;
        if(firing==1&&port>0||firing==2&&starboard>0)return firing;
        if(port==0)return 2;
        if(starboard==0)return 1;
        float left=System.Math.Abs(Delta(heading,Heading(targetBearing,1)));
        float right=System.Math.Abs(Delta(heading,Heading(targetBearing,2)));
        return left<=right?1:2;
    }
    public static float OpeningHeading(float targetBearing,int side)
    {return targetBearing+(side==1?65f:-65f);}
    public static float OpeningDistance(float heading,float targetBearing,int side,float ownSpeed,float closingSpeed,float ownScale,float enemyScale)
    {
        // Allow for rudder response and the actual approach speed before reaching gun range.
        float error=System.Math.Abs(Delta(heading,OpeningHeading(targetBearing,side)));
        float rate=System.Math.Max(2f,System.Math.Abs(ownSpeed)*3f*.75f);
        float seconds=System.Math.Min(30f,error/rate+2f);
        float gunRange=NavalGunneryMath.LevelRange+2.25f*ownScale+6.45f*enemyScale;
        return gunRange+System.Math.Max(40f,System.Math.Max(0f,closingSpeed)*seconds+12f);
    }
    public static float FiringCourse(float heading,float targetBearing,int side,float correctionBearing,float distance,float clearance,bool openingTurn=false)
    {
        float broadside=Heading(targetBearing,side);
        // Finish the turn before chasing a bow/stern waypoint. Inside firing range,
        // waypoint corrections cannot rotate the guns out of their 60-degree sector.
        if(distance<=NavalGunneryMath.LevelRange*1.1f&&distance>=clearance)
        {
            if(System.Math.Abs(Delta(heading,broadside))>20f)return broadside;
            float trim=System.Math.Max(-12f,System.Math.Min(12f,Delta(broadside,correctionBearing)));
            return broadside+trim;
        }
        // Approach at the inner edge of the working gun sector, retaining forward closure.
        // A full 90-degree turn too early would orbit beyond gun range indefinitely.
        if(openingTurn&&distance>NavalGunneryMath.LevelRange*1.1f)return OpeningHeading(targetBearing,side);
        return correctionBearing;
    }
    // cross(up, towardTarget) exposes port; its negative exposes starboard.
    public static float OrbitSign(int portGuns,int starboardGuns,float preferred)
    {
        if(portGuns>starboardGuns)return 1f;
        if(starboardGuns>portGuns)return -1f;
        if(preferred>0f&&portGuns>0||preferred<0f&&starboardGuns>0)return preferred;
        if(portGuns>0)return 1f;
        if(starboardGuns>0)return -1f;
        return preferred;
    }
}
