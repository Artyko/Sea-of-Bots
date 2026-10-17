using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Cosmetic capacity is bounded. Overflow replaces the next old slot, never a projectile.
public sealed class NavalSmokeBatch : MonoBehaviour
{
    private const int Capacity=512,PaletteLimit=64;
    private struct Puff {public Vector3 Position,Velocity;public float Life,Total,Size;public int Palette;}
    private readonly Puff[] particles=new Puff[Capacity];
    private readonly List<Vector3> vertices=new List<Vector3>(Capacity*24),normals=new List<Vector3>(Capacity*24);
    private readonly List<Material> materials=new List<Material>();
    private readonly List<Material> sources=new List<Material>();
    private readonly List<int> shades=new List<int>();
    private readonly List<List<int>> triangles=new List<List<int>>();
    private Mesh mesh;
    private MeshRenderer renderer;
    private int cursor,active;
    private static readonly Vector3[] directions={Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back};
    public void Emit(Vector3 position,Vector3 velocity,Material source,float duration,float radius)
    {
        if(mesh==null)
        {
            GameObject visual=new GameObject("NavalPrototype_PixelSmokeBatch");visual.transform.SetParent(transform,false);
            mesh=new Mesh();mesh.name="Naval pixel smoke";mesh.MarkDynamic();
            visual.AddComponent<MeshFilter>().sharedMesh=mesh;renderer=visual.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.enabled=false;
        }
        int shade=Random.Range(0,8),palette=-1;
        for(int i=0;i<materials.Count;i++)if(sources[i]==source&&shades[i]==shade){palette=i;break;}
        if(palette<0&&materials.Count<PaletteLimit)
        {
            palette=materials.Count;Material material=new Material(source);
            Color color=source.color*Mathf.Lerp(.8f,1.18f,shade/7f);color.a=1f;material.color=color;
            materials.Add(material);sources.Add(source);shades.Add(shade);triangles.Add(new List<int>(Capacity*36/8));
            renderer.sharedMaterials=materials.ToArray();
        }
        if(palette<0)palette=0;
        // Prefer a free slot; bursts cannot evict live smoke until capacity is reached.
        int slot=cursor;
        for(int i=0;i<Capacity;i++){int probe=(cursor+i)%Capacity;if(particles[probe].Life<=0f){slot=probe;break;}}
        if(particles[slot].Life<=0f)active++;
        particles[slot]=new Puff{Position=position,Velocity=velocity,Life=duration,Total=duration,Size=radius,Palette=palette};
        cursor=(slot+1)%Capacity;
    }
    private void LateUpdate()
    {
        if(active==0||mesh==null)return;
        using(NavalPerformance.Measure(NavalPerfArea.Effects))
        {
            vertices.Clear();normals.Clear();foreach(List<int> list in triangles)list.Clear();
            float dt=Time.deltaTime,drag=1f-Mathf.Exp(-1.6f*dt);
            Matrix4x4 toLocal=transform.worldToLocalMatrix;
            for(int i=0;i<Capacity;i++)
            {
                Puff puff=particles[i];if(puff.Life<=0f)continue;
                puff.Life-=dt;if(puff.Life<=0f){particles[i]=puff;active--;continue;}
                puff.Position+=puff.Velocity*dt;puff.Velocity=Vector3.Lerp(puff.Velocity,Vector3.up*.5f,drag);
                float size=puff.Size*(1f+(1f-puff.Life/puff.Total)*1.3f)*Mathf.Min(1f,puff.Life/(puff.Total*.3f));
                float half=Mathf.Max(.04f,Mathf.Round(size/.04f)*.04f)*.5f;
                Vector3 center=toLocal.MultiplyPoint3x4(puff.Position);
                for(int face=0;face<6;face++)
                {
                    Vector3 normal=directions[face],u=face<2?Vector3.up:Vector3.right,v=Vector3.Cross(normal,u);
                    int start=vertices.Count;Vector3 middle=center+normal*half;
                    vertices.Add(middle+(-u-v)*half);vertices.Add(middle+(u-v)*half);
                    vertices.Add(middle+(u+v)*half);vertices.Add(middle+(-u+v)*half);
                    for(int corner=0;corner<4;corner++)normals.Add(normal);
                    List<int> indices=triangles[puff.Palette];indices.Add(start);indices.Add(start+1);indices.Add(start+2);
                    indices.Add(start);indices.Add(start+2);indices.Add(start+3);
                }
                particles[i]=puff;
            }
            renderer.enabled=active>0;
            mesh.Clear();if(active==0)return;
            mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.subMeshCount=triangles.Count;
            for(int i=0;i<triangles.Count;i++)mesh.SetTriangles(triangles[i],i,false);
            mesh.RecalculateBounds();
        }
    }
    private void OnDestroy(){if(mesh!=null)Destroy(mesh);foreach(Material material in materials)Destroy(material);}
}
