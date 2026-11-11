public static class NavalRepairPatchMath
{
 public const float RadiusMetres=3f;
 public static bool Within(float dx,float dy,float dz,float scale){return (dx*dx+dy*dy+dz*dz)*scale*scale<=RadiusMetres*RadiusMetres;}
 public static int Face(float x,float y,float z){return x<=y&&x<=z?0:y<=z?1:2;}
 public static bool SameSurface(float dx,float dy,float dz,float ax,float ay,float az,float bx,float by,float bz,float scale){
  int a=Face(ax,ay,az),b=Face(bx,by,bz);
  return a==b&&System.Math.Abs(a==0?dx:a==1?dy:dz)*scale<=.35f&&Within(dx,dy,dz,scale);
 }
}
