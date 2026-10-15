using UnityEngine;

// Open voxel bucket, handle, visible carried water and falling block droplets.
public class NavalBucketVisual : MonoBehaviour
{
    private GameObject root,water;
    private Transform[] drops=new Transform[10];
    private Material wood,metal,blue;
    private Material Material(Color color)
    {Material m=new Material(Shader.Find("Unlit/Color"));m.color=color;return m;}
    private Transform Cube(string name,Transform parent,Vector3 position,Vector3 scale,Material material)
    {
        GameObject obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;
        obj.transform.SetParent(parent,false);obj.transform.localPosition=position;obj.transform.localScale=scale;
        Collider collider=obj.GetComponent<Collider>();if(collider!=null){collider.enabled=false;Destroy(collider);}
        obj.GetComponent<Renderer>().sharedMaterial=material;return obj.transform;
    }
    public void Initialize()
    {
        wood=Material(new Color(0.42f,0.24f,0.1f));metal=Material(new Color(0.13f,0.15f,0.17f));blue=Material(new Color(0.13f,0.62f,0.85f));
        root=new GameObject("Sailor voxel bucket");
        Cube("Base",root.transform,Vector3.zero,new Vector3(.48f,.08f,.48f),wood);
        for(int i=0;i<4;i++)
        {
            bool x=i<2;float side=i%2==0?-1f:1f;
            Cube("Wood wall",root.transform,new Vector3(x?side*.23f:0f,.23f,x?0f:side*.23f),new Vector3(x?.06f:.48f,.48f,x?.48f:.06f),wood);
        }
        Cube("Handle left",root.transform,new Vector3(-.27f,.6f,0f),new Vector3(.045f,.45f,.045f),metal);
        Cube("Handle right",root.transform,new Vector3(.27f,.6f,0f),new Vector3(.045f,.45f,.045f),metal);
        Cube("Handle grip",root.transform,new Vector3(0f,.82f,0f),new Vector3(.58f,.055f,.055f),metal);
        water=Cube("Bucket water",root.transform,new Vector3(0f,.37f,0f),new Vector3(.4f,.055f,.4f),blue).gameObject;
        for(int i=0;i<drops.Length;i++)drops[i]=Cube("Bucket splash",root.transform,Vector3.zero,Vector3.one*.09f,blue);
        Hide();
    }
    public void Pose(Transform boat,Vector3 local,float tilt,bool full,bool pouring)
    {
        root.SetActive(true);root.transform.position=boat.TransformPoint(local);root.transform.rotation=boat.rotation*Quaternion.Euler(0f,0f,tilt);
        water.SetActive(full);
        for(int i=0;i<drops.Length;i++)
        {
            drops[i].gameObject.SetActive(pouring&&full);
            if(!pouring)continue;
            float t=Mathf.Repeat(Time.time*1.5f+i/(float)drops.Length,1f);
            drops[i].position=boat.TransformPoint(local)+Vector3.down*(t*t*5f)+boat.right*Mathf.Sign(local.x)*t*.5f+boat.forward*Mathf.Sin(i*2f)*.12f;
            drops[i].rotation=Quaternion.Euler(i*31f,Time.time*70f,i*17f);
        }
    }
    public void Hide(){if(root!=null)root.SetActive(false);}
    private void OnDestroy(){if(root!=null)Destroy(root);if(wood!=null)Destroy(wood);if(metal!=null)Destroy(metal);if(blue!=null)Destroy(blue);}
}
