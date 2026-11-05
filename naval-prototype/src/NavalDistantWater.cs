using UnityEngine;
using UnityEngine.Rendering;
// Visual-only sparse annulus: no collision, flooding or hull clipping work.
public sealed class NavalDistantWater:MonoBehaviour {
 private const int Edge=100,Perimeter=400;
 private static readonly float[] radii={149.8f,180f,250f,400f,700f,1200f,2200f,4000f};
 private Mesh mesh;private Vector3[] points,normals;private NavalWater water;private float nextAt;
 public void Initialize(NavalWater owner){
  water=owner;points=new Vector3[Perimeter*radii.Length];normals=new Vector3[points.Length];
  int[] faces=new int[(radii.Length-1)*Perimeter*6];int at=0;
  for(int ring=0;ring<radii.Length-1;ring++)for(int i=0;i<Perimeter;i++){
   int a=ring*Perimeter+i,b=ring*Perimeter+(i+1)%Perimeter,c=a+Perimeter,d=b+Perimeter;
   faces[at++]=a;faces[at++]=b;faces[at++]=c;faces[at++]=b;faces[at++]=d;faces[at++]=c;
  }
  for(int i=0;i<normals.Length;i++)normals[i]=Vector3.up;
  mesh=new Mesh();mesh.name="Sparse distant ocean rings";mesh.MarkDynamic();mesh.vertices=points;mesh.triangles=faces;mesh.normals=normals;
  gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=owner.SurfaceMaterial;
  renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
 }
 public void Refresh(Vector3 focus,float time,float amplitude){
  if(mesh==null||Time.time<nextAt)return;nextAt=Time.time+1f/(NavalPerformance.LaptopMode?4f:8f);
  float cx=Mathf.Floor((focus.x-water.transform.position.x)/40f)*40f,cz=Mathf.Floor((focus.z-water.transform.position.z)/40f)*40f;
  for(int ring=0;ring<radii.Length;ring++)for(int i=0;i<Perimeter;i++){
   int side=i/Edge;float t=(i%Edge)/(float)Edge*2f-1f,r=radii[ring];
   float x=side==0?t:side==1?1:side==2?-t:-1,z=side==0?-1:side==1?t:side==2?1:-t;
   x=cx+x*r;z=cz+z*r;
   float detailed=NavalWaveField.Sample(x,z,amplitude,time);
   float swell=amplitude*(Mathf.Sin(x*.012f+z*.008f-time*.65f)+.4f*Mathf.Sin(z*.018f-time*.8f));
   float blend=Mathf.Clamp01((r-180f)/520f);
   points[ring*Perimeter+i]=new Vector3(x,Mathf.Lerp(detailed,swell,blend)-.04f,z);
  }
  mesh.vertices=points;mesh.bounds=new Bounds(new Vector3(cx,0,cz),new Vector3(8100,100+amplitude*8,8100));
 }
 private void OnDestroy(){if(mesh!=null)Destroy(mesh);}
}
