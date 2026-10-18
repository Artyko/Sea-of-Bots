using System;
// Cheap geometric positioning shared by all ship profiles. No physics scans or allocations.
public sealed class NavalFiringPositionMath
{
    private int selected=-1;
    private static readonly float[] bearings={0f,25f,-25f,180f,155f,205f,90f,-90f};
    public void Reset(){selected=-1;}
    public static bool Threatens(float bearing,int port,int starboard,float margin)
    {
        float a=(bearing%360f+360f)%360f;
        return starboard>0&&Math.Abs(a-90f)<=30f+margin||port>0&&Math.Abs(a-270f)<=30f+margin;
    }
    public static float Range(float ownScale,float enemyScale)
    {
        float clearance=NavalHelmTactics.Clearance(ownScale,enemyScale);
        return Math.Max(clearance+12f,NavalGunneryMath.LevelRange*.75f);
    }
    // Local enemy coordinates: +Z bow, -Z stern, +X starboard.
    public void Select(float ownX,float ownZ,float radius,int enemyPort,int enemyStarboard,out float x,out float z)
    {
        float best=float.MaxValue;int chosen=0;
        for(int i=0;i<bearings.Length;i++)
        {
            float radians=bearings[i]*(float)Math.PI/180f;
            float px=(float)Math.Sin(radians)*radius,pz=(float)Math.Cos(radians)*radius;
            float dx=px-ownX,dz=pz-ownZ;
            float cost=(float)Math.Sqrt(dx*dx+dz*dz)+(Threatens(bearings[i],enemyPort,enemyStarboard,12f)?radius*2f:0f);
            if(i==selected)cost-=15f; // Avoid flipping bow/stern routes at the midpoint.
            if(cost<best){best=cost;chosen=i;}
        }
        selected=chosen;float angle=bearings[chosen]*(float)Math.PI/180f;
        x=(float)Math.Sin(angle)*radius;z=(float)Math.Cos(angle)*radius;
    }
}
