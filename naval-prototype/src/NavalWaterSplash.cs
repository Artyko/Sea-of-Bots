using UnityEngine;
public class NavalWaterSplash : MonoBehaviour
{
    private readonly Transform[] blocks=new Transform[28];
    private readonly Vector3[] speeds=new Vector3[20];
    private Material water,foam;
    private NavalWater ocean;
    private Vector3 origin;
    private float age;
    public static void Create(Transform parent,Vector3 point,NavalWater sea)
    {
        GameObject obj=new GameObject("NavalPrototype_WaterSplash");obj.transform.SetParent(parent,false);
        NavalWaterSplash splash=obj.AddComponent<NavalWaterSplash>();splash.origin=point;splash.ocean=sea;splash.Build();
    }
    private Material Surface(Color color)
    {
        Shader shader=Shader.Find("Unlit/Color");if(shader==null)shader=Shader.Find("Standard");
        Material material=new Material(shader);material.color=color;return material;
    }
    private void Build()
    {
        water=Surface(new Color(0.26f,0.68f,0.86f));foam=Surface(new Color(0.8f,0.94f,1f));
        for(int i=0;i<blocks.Length;i++)
        {
            GameObject block=GameObject.CreatePrimitive(PrimitiveType.Cube);
            Collider collider=block.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
            block.transform.SetParent(transform,false);block.transform.position=origin;
            Renderer renderer=block.GetComponent<Renderer>();renderer.sharedMaterial=i%3==0?foam:water;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            blocks[i]=block.transform;
            if(i<20)
            {
                float angle=i*2.39996f;float spread=Random.Range(0.7f,2.5f);
                speeds[i]=new Vector3(Mathf.Cos(angle)*spread,Random.Range(2.5f,5f),Mathf.Sin(angle)*spread);
                block.transform.localScale=Vector3.one*Random.Range(0.1f,0.22f);
            }
        }
    }
    private void Update()
    {
        float dt=Time.deltaTime;age+=dt;if(age>=1.1f||ocean==null){Destroy(gameObject);return;}
        for(int i=0;i<blocks.Length;i++)
        {
            Transform block=blocks[i];
            if(i<20)
            {
                speeds[i]+=Vector3.down*9.81f*dt;block.position+=speeds[i]*dt;
                if(age>0.1f&&block.position.y<ocean.Height(block.position.x,block.position.z))block.gameObject.SetActive(false);
            }
            else
            {
                float angle=(i-20)*Mathf.PI/4f,radius=0.15f+age*1.6f;
                Vector3 p=origin+new Vector3(Mathf.Cos(angle)*radius,0f,Mathf.Sin(angle)*radius);
                p.y=ocean.Height(p.x,p.z)+0.04f;block.position=p;
                block.localScale=new Vector3(0.22f,0.04f,0.22f)*Mathf.Clamp01((1.1f-age)*2f);
            }
        }
    }
    private void OnDestroy(){if(water!=null)Destroy(water);if(foam!=null)Destroy(foam);}
}
