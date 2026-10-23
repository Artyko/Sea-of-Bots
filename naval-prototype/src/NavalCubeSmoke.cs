using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
// Actual depth-writing cubes; no billboard, transparent face sorting or per-puff GameObjects.
public sealed class NavalCubeSmoke : MonoBehaviour
{
 private struct Cube {public Vector3 Position,Velocity;public float OriginY,Life,Total,Size;}
 private readonly Cube[] cubes=new Cube[NavalCubeSmokeMath.Capacity];
 private readonly List<Vector3> vertices=new List<Vector3>(NavalCubeSmokeMath.Capacity*24);
 private readonly List<Vector3> normals=new List<Vector3>(NavalCubeSmokeMath.Capacity*24);
 private readonly List<Vector2> uv=new List<Vector2>(NavalCubeSmokeMath.Capacity*24);
 private readonly List<Color> colors=new List<Color>(NavalCubeSmokeMath.Capacity*24);
 private readonly List<List<int>> indices=new List<List<int>>();
 private readonly List<Material> materials=new List<Material>();
 private static readonly Vector3[] faces={Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back};
 private Mesh mesh;
 private MeshRenderer visual;
 private bool vertexColour;
 private int cursor,active;
 private void Initialize()
 {
  GameObject child=new GameObject("Naval solid cube smoke");child.transform.SetParent(transform,false);
  mesh=new Mesh();mesh.name="Naval solid smoke cubes";mesh.MarkDynamic();
  child.AddComponent<MeshFilter>().sharedMesh=mesh;visual=child.AddComponent<MeshRenderer>();
  visual.shadowCastingMode=ShadowCastingMode.Off;visual.receiveShadows=false;visual.enabled=false;
  Shader shader=Shader.Find("Particles/Standard Unlit");vertexColour=shader!=null;
  if(vertexColour)
  {
   Material material=new Material(shader);material.SetFloat("_Mode",0);
   material.SetInt("_SrcBlend",1);material.SetInt("_DstBlend",0);material.SetInt("_ZWrite",1);
   material.DisableKeyword("_ALPHATEST_ON");material.DisableKeyword("_ALPHABLEND_ON");material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
   material.DisableKeyword("_FADING_ON");material.SetOverrideTag("RenderType","Opaque");material.renderQueue=2000;
   if(material.HasProperty("_Color"))material.SetColor("_Color",Color.white);
   if(material.HasProperty("_ColorMode"))material.SetFloat("_ColorMode",0);
   materials.Add(material);indices.Add(new List<int>(NavalCubeSmokeMath.Capacity*36));
  }
  else
  {
   // Builds which stripped the particle shader still get opaque, shaded cubes.
   shader=Shader.Find("Unlit/Color");if(shader==null)shader=Shader.Find("Standard");
   for(int shade=0;shade<NavalCubeSmokeMath.ShadeLevels;shade++)for(int face=0;face<3;face++)
   {
    float grey=Mathf.Lerp(.12f,.60f,shade/(float)(NavalCubeSmokeMath.ShadeLevels-1))*(face==0?1:face==1?.84f:.68f);
    Material material=new Material(shader);material.color=new Color(grey,grey,grey,1);material.renderQueue=2000;
    materials.Add(material);indices.Add(new List<int>());
   }
  }
  visual.SetSharedMaterials(materials);
 }
 public void Emit(Vector3 point,Vector3 sourceVelocity,float lifetime,float size)
 {
  if(mesh==null)Initialize();
  int slot=cursor;for(int n=0;n<cubes.Length;n++){int probe=(cursor+n)%cubes.Length;if(cubes[probe].Life<=0){slot=probe;break;}}
  if(cubes[slot].Life<=0)active++;
  Vector3 jitter=Random.insideUnitSphere*.12f;jitter.y=0;
  cubes[slot]=new Cube{Position=point+jitter,OriginY=point.y,Velocity=sourceVelocity*NavalSmokePlumeMath.SourceCarry+Vector3.up*NavalCubeSmokeMath.RiseSpeed,
   Life=lifetime,Total=lifetime,Size=size};cursor=(slot+1)%cubes.Length;
 }
 private void LateUpdate()
 {
  if(mesh==null||active==0)return;
  using(NavalPerformance.Measure(NavalPerfArea.Effects))
  {
   vertices.Clear();normals.Clear();uv.Clear();colors.Clear();foreach(var list in indices)list.Clear();
   float dt=Time.deltaTime,drag=NavalSmokePlumeMath.Drag(dt);
   Matrix4x4 local=transform.worldToLocalMatrix,normalMatrix=transform.localToWorldMatrix.transpose;
   for(int i=0;i<cubes.Length;i++)
   {
    Cube cube=cubes[i];if(cube.Life<=0)continue;cube.Life-=dt;
    if(cube.Life<=0){cubes[i]=cube;active--;continue;}
    cube.Position+=cube.Velocity*dt;cube.Velocity=Vector3.Lerp(cube.Velocity,Vector3.up*NavalCubeSmokeMath.RiseSpeed,drag);
    float half=cube.Size*NavalCubeSmokeMath.Size(cube.Total-cube.Life,cube.Total)*.5f;
    float grey=NavalCubeSmokeMath.Grey(cube.Position.y-cube.OriginY,cube.Total);
    for(int face=0;face<6;face++)
    {
     Vector3 normal=faces[face],u=face<2?Vector3.up:Vector3.right,v=Vector3.Cross(normal,u);
     Vector3 middle=cube.Position+normal*half;int start=vertices.Count;
     float shade=NavalCubeSmokeMath.Face(face);
     for(int corner=0;corner<4;corner++)
     {
      float x=corner==0||corner==3?-1:1,y=corner<2?-1:1;
      Vector3 world=middle+(u*x+v*y)*half;vertices.Add(local.MultiplyPoint3x4(world));
      normals.Add(normalMatrix.MultiplyVector(normal).normalized);uv.Add(new Vector2((x+1)*.5f,(y+1)*.5f));
      float tone=NavalCubeSmokeMath.Grey(world.y-cube.OriginY,cube.Total)*shade;colors.Add(new Color(tone,tone,tone,1));
     }
     int palette=vertexColour?0:NavalCubeSmokeMath.Shade(grey)*3+(face==2?0:face<2?1:2);
     var tris=indices[palette];tris.Add(start);tris.Add(start+1);tris.Add(start+2);tris.Add(start);tris.Add(start+2);tris.Add(start+3);
    }
    cubes[i]=cube;
   }
   mesh.Clear();visual.enabled=active>0;if(active==0)return;
   mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.subMeshCount=indices.Count;
   for(int i=0;i<indices.Count;i++)mesh.SetTriangles(indices[i],i,false);mesh.RecalculateBounds();
  }
 }
 private void OnDestroy(){if(mesh!=null)Destroy(mesh);foreach(var material in materials)Destroy(material);}
}
