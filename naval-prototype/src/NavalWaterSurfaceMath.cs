public static class NavalWaterSurfaceMath{
 public static float Triangle(float a,float b,float c,float d,float u,float v){return u+v<=1f?a+(b-a)*u+(c-a)*v:d+(c-d)*(1-u)+(b-d)*(1-v);}
}
