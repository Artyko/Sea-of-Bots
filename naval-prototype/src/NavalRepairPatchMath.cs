public static class NavalRepairPatchMath
{
 public const float RadiusMetres=1.2f;
 public static bool Within(float dx,float dy,float dz,float scale){return (dx*dx+dy*dy+dz*dz)*scale*scale<=RadiusMetres*RadiusMetres;}
}
