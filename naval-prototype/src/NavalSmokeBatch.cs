using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Cosmetic capacity is bounded. Overflow replaces the next old slot, never a projectile.
public sealed class NavalSmokeBatch : MonoBehaviour
{
    private const int Capacity=512,PaletteLimit=64;
    private struct Puff {public Vector3 Position,Velocity;public float Life,Total,Size,Rise,Expansion,Gravity;public int Palette;public int[] Opacity;public Color Tint;}
    private readonly Puff[] particles=new Puff[Capacity];
    private readonly List<Vector3> vertices=new List<Vector3>(Capacity*24),normals=new List<Vector3>(Capacity*24);
    private readonly List<Color> colors=new List<Color>(Capacity*24);
    private readonly List<Material> materials=new List<Material>();
    private readonly List<Material> sources=new List<Material>();
    private readonly List<int> shades=new List<int>();
    private readonly List<List<int>> triangles=new List<List<int>>();
    private readonly Dictionary<Material,int[]> plumePalettes=new Dictionary<Material,int[]>();
    private NavalCubeSmoke fireCubes;
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
        if(palette<0)
        {
            float nearest=float.PositiveInfinity;Color wanted=source.color;palette=0;
            for(int i=0;i<materials.Count;i++){Color c=sources[i]!=null?sources[i].color:Color.white;float d=(c.r-wanted.r)*(c.r-wanted.r)+(c.g-wanted.g)*(c.g-wanted.g)+(c.b-wanted.b)*(c.b-wanted.b);if(d<nearest){nearest=d;palette=i;}}
        }
        // Prefer a free slot; bursts cannot evict live smoke until capacity is reached.
        int slot=cursor;
        for(int i=0;i<Capacity;i++){int probe=(cursor+i)%Capacity;if(particles[probe].Life<=0f){slot=probe;break;}}
        if(particles[slot].Life<=0f)active++;
        particles[slot]=new Puff{Position=position,Velocity=velocity,Life=duration,Total=duration,Size=radius,Palette=palette,Rise=.5f,Expansion=1.3f,Tint=source.color};
        cursor=(slot+1)%Capacity;
    }
    public void EmitDebris(Vector3 position,Vector3 velocity,Material source)
    {
        Emit(position,velocity,source,2.4f,.18f);int slot=(cursor+Capacity-1)%Capacity;
        Puff puff=particles[slot];puff.Gravity=9.8f;puff.Expansion=0;particles[slot]=puff;
    }
    public void EmitPlume(Vector3 position,Vector3 velocity,Material source,float size=.55f,float duration=NavalSmokePlumeMath.Lifetime)
    {
        if(source==null)return;
        // Emit performs the shared lazy mesh initialization and bounded-slot selection.
        Emit(position,velocity,source,duration,size);
        int slot=(cursor+Capacity-1)%Capacity;Puff puff=particles[slot];int[] opacity;
        if(!plumePalettes.TryGetValue(source,out opacity)&&materials.Count+NavalSmokePlumeMath.OpacityLevels<=PaletteLimit)
        {
            Shader shader=Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            if(shader==null)shader=Shader.Find("Legacy Shaders/Transparent/Diffuse");
            if(shader==null)shader=Shader.Find("Standard");
            if(shader!=null)
            {
                opacity=new int[NavalSmokePlumeMath.OpacityLevels];
                for(int i=0;i<opacity.Length;i++)
                {
                    float age=i/(float)opacity.Length;Material material=new Material(shader);
                    if(shader.name=="Standard")
                    {
                        material.SetFloat("_Mode",2);material.SetInt("_SrcBlend",5);material.SetInt("_DstBlend",10);material.SetInt("_ZWrite",0);
                        material.EnableKeyword("_ALPHABLEND_ON");material.DisableKeyword("_ALPHAPREMULTIPLY_ON");material.SetOverrideTag("RenderType","Transparent");material.renderQueue=3000;
                    }
                    Color color=Color.Lerp(source.color,new Color(.5f,.5f,.5f),age*.6f);color.a=.65f*(1-age);
                    // Particle shader multiplies vertex colour by _TintColor. Set
                    // white neutral tint; smoke darkness/alpha is explicit in vertices.
                    if(material.HasProperty("_TintColor"))material.SetColor("_TintColor",Color.white);
                    if(material.HasProperty("_Color"))material.SetColor("_Color",color);
                    opacity[i]=materials.Count;materials.Add(material);sources.Add(source);shades.Add(100+i);triangles.Add(new List<int>(Capacity*36/8));
                }
                plumePalettes.Add(source,opacity);renderer.SetSharedMaterials(materials);
            }
        }
        puff.Opacity=opacity;puff.Rise=NavalSmokePlumeMath.RiseSpeed;puff.Expansion=NavalSmokePlumeMath.Expansion;particles[slot]=puff;
    }
    public void EmitFireColumn(Vector3 point,Vector3 sourceVelocity,Material material,float duration,float size)
    {
        if(fireCubes==null)fireCubes=gameObject.AddComponent<NavalCubeSmoke>();
        fireCubes.Emit(point,sourceVelocity,duration,size);
    }
    private void LateUpdate()
    {
        if(active==0||mesh==null)return;
        using(NavalPerformance.Measure(NavalPerfArea.Effects))
        {
            vertices.Clear();normals.Clear();colors.Clear();foreach(List<int> list in triangles)list.Clear();
            float dt=Time.deltaTime,drag=NavalSmokePlumeMath.Drag(dt);
            Matrix4x4 toLocal=transform.worldToLocalMatrix;
            for(int i=0;i<Capacity;i++)
            {
                Puff puff=particles[i];if(puff.Life<=0f)continue;
                puff.Life-=dt;if(puff.Life<=0f){particles[i]=puff;active--;continue;}
                puff.Position+=puff.Velocity*dt;
                if(puff.Gravity>0)puff.Velocity+=Vector3.down*puff.Gravity*dt;
                else puff.Velocity=Vector3.Lerp(puff.Velocity,Vector3.up*puff.Rise,drag);
                float size=puff.Size*(1f+(1f-puff.Life/puff.Total)*puff.Expansion);
                if(puff.Opacity==null)size*=Mathf.Min(1f,puff.Life/(puff.Total*.3f));
                Color vertexTint=Color.white;
                if(puff.Opacity!=null)
                {
                    float age=puff.Total-puff.Life;
                    vertexTint=Color.Lerp(puff.Tint,new Color(.35f,.35f,.35f),Mathf.Clamp01(age/puff.Total)*.5f);
                    vertexTint.a=NavalSmokePlumeMath.Alpha(age,puff.Total);
                }
                int palette=puff.Opacity!=null?puff.Opacity[NavalSmokePlumeMath.Level(puff.Life,puff.Total)]:puff.Palette;
                float half=Mathf.Max(.04f,Mathf.Round(size/.04f)*.04f)*.5f;
                Vector3 center=toLocal.MultiplyPoint3x4(puff.Position);
                for(int face=0;face<6;face++)
                {
                    Vector3 normal=directions[face],u=face<2?Vector3.up:Vector3.right,v=Vector3.Cross(normal,u);
                    Vector3 axisNormal=normal*half,axisU=u*half,axisV=v*half;
                    int start=vertices.Count;Vector3 middle=center+axisNormal;
                    vertices.Add(middle-axisU-axisV);vertices.Add(middle+axisU-axisV);
                    vertices.Add(middle+axisU+axisV);vertices.Add(middle-axisU+axisV);
                    for(int corner=0;corner<4;corner++){normals.Add(normal);colors.Add(vertexTint);}
                    List<int> indices=triangles[palette];indices.Add(start);indices.Add(start+1);indices.Add(start+2);
                    indices.Add(start);indices.Add(start+2);indices.Add(start+3);
                }
                particles[i]=puff;
            }
            renderer.enabled=active>0;
            mesh.Clear();if(active==0)return;
            mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetColors(colors);mesh.subMeshCount=triangles.Count;
            for(int i=0;i<triangles.Count;i++)mesh.SetTriangles(triangles[i],i,false);
            mesh.RecalculateBounds();
        }
    }
    private void OnDestroy(){if(mesh!=null)Destroy(mesh);foreach(Material material in materials)Destroy(material);}
}
