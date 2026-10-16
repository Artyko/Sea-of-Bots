using System.Collections.Generic;
using UnityEngine;

// One mesh / two material draws instead of 28 objects, renderers and colliders.
// Only cosmetic water contact is sampled at 10 Hz; projectile physics stays exact.
public class NavalWaterSplash : MonoBehaviour
{
    private const int Count=28,Drops=20;
    private readonly Vector3[] positions=new Vector3[Count],speeds=new Vector3[Drops];
    private readonly Vector3[] vertices=new Vector3[Count*8];
    private readonly float[] sizes=new float[Count],levels=new float[Count];
    private readonly bool[] visible=new bool[Count];
    private static readonly Vector3[] corners={
        new Vector3(-.5f,-.5f,-.5f),new Vector3(.5f,-.5f,-.5f),
        new Vector3(-.5f,.5f,-.5f),new Vector3(.5f,.5f,-.5f),
        new Vector3(-.5f,-.5f,.5f),new Vector3(.5f,-.5f,.5f),
        new Vector3(-.5f,.5f,.5f),new Vector3(.5f,.5f,.5f)};
    private static readonly int[] faces={0,2,1,1,2,3,4,5,6,5,7,6,
        0,1,4,1,5,4,2,6,3,3,6,7,0,4,2,2,4,6,1,3,5,3,7,5};
    private Material water,foam;
    private Mesh mesh;
    private NavalWater ocean;
    private Vector3 origin;
    private float age,nextContactAt;
    public static void Create(Transform parent,Vector3 point,NavalWater sea)
    {
        if(float.IsInfinity(point.y)||float.IsNaN(point.y))return;
        GameObject obj=new GameObject("NavalPrototype_WaterSplash");obj.transform.SetParent(parent,false);
        obj.transform.position=point;obj.transform.rotation=Quaternion.identity;
        NavalWaterSplash splash=obj.AddComponent<NavalWaterSplash>();splash.origin=point;splash.ocean=sea;splash.Build();
    }
    private Material Surface(Color color)
    {
        Shader shader=Shader.Find("Unlit/Color");if(shader==null)shader=Shader.Find("Standard");
        Material material=new Material(shader);material.color=color;return material;
    }
    private void Build()
    {
        water=Surface(new Color(.26f,.68f,.86f));foam=Surface(new Color(.8f,.94f,1f));
        List<int> blue=new List<int>(),white=new List<int>();
        for(int i=0;i<Count;i++)
        {
            positions[i]=origin;visible[i]=true;levels[i]=origin.y;
            List<int> indices=i%3==0?white:blue;
            foreach(int corner in faces)indices.Add(i*8+corner);
            if(i<Drops)
            {
                float angle=i*2.39996f,spread=Random.Range(.7f,2.5f);
                speeds[i]=new Vector3(Mathf.Cos(angle)*spread,Random.Range(2.5f,5f),Mathf.Sin(angle)*spread);
                sizes[i]=Random.Range(.1f,.22f);
            }
        }
        mesh=new Mesh();mesh.name="Batched voxel splash";mesh.MarkDynamic();WriteVertices();
        mesh.vertices=vertices;mesh.subMeshCount=2;
        mesh.SetTriangles(blue,0);mesh.SetTriangles(white,1);
        mesh.bounds=new Bounds(Vector3.zero,Vector3.one*20f);
        gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
        MeshRenderer renderer=gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterials=new Material[]{water,foam};
        renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
    }
    private void Update()
    {
        float dt=Time.deltaTime;if(dt<=0f)return;
        age+=dt;if(age>=1.1f||ocean==null){Destroy(gameObject);return;}
        bool sample=age>=nextContactAt;if(sample)nextContactAt=age+.1f;
        NavalTestWorld world=NavalTestWorld.Active;
        for(int i=0;i<Count;i++)
        {
            if(i<Drops)
            {
                if(!visible[i])continue;
                speeds[i]+=Vector3.down*9.81f*dt;positions[i]+=speeds[i]*dt;
                if(sample)levels[i]=WaterLevel(world,positions[i]);
                if(age>.1f&&positions[i].y<levels[i])visible[i]=false;
            }
            else
            {
                float angle=(i-Drops)*Mathf.PI/4f,radius=.15f+age*1.6f;
                Vector3 point=origin+new Vector3(Mathf.Cos(angle)*radius,0f,Mathf.Sin(angle)*radius);
                if(sample)levels[i]=WaterLevel(world,point);
                visible[i]=!float.IsInfinity(levels[i])&&!float.IsNaN(levels[i]);
                if(visible[i]){point.y=levels[i]+.04f;positions[i]=point;}
            }
        }
        WriteVertices();mesh.vertices=vertices;
    }
    private void WriteVertices()
    {
        float fade=Mathf.Clamp01((1.1f-age)*2f);
        for(int i=0;i<Count;i++)
        {
            Vector3 size=i<Drops?Vector3.one*sizes[i]:new Vector3(.22f,.04f,.22f)*fade;
            // Collapsed triangles hide spent drops without enabling/disabling objects.
            if(!visible[i])size=Vector3.zero;
            Vector3 centre=positions[i]-origin;
            for(int c=0;c<8;c++)vertices[i*8+c]=centre+Vector3.Scale(corners[c],size);
        }
    }
    private float WaterLevel(NavalTestWorld world,Vector3 point)
    {return world!=null?world.WaterLevelAt(point):ocean.Height(point.x,point.z);}
    private void OnDestroy(){if(mesh!=null)Destroy(mesh);if(water!=null)Destroy(water);if(foam!=null)Destroy(foam);}
}
