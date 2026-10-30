using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// The editor map is intentionally made from editable cubes. During play they are
// static, so merge compatible renderers while leaving every collider untouched.
public sealed class NavalMapRenderBatch : MonoBehaviour
{
    private struct Key
    {
        public int Shader,Texture,Queue,Color;
        public override int GetHashCode(){unchecked{return Shader*397^Texture*31^Queue*17^Color;}}
        public override bool Equals(object value)
        {
            if(!(value is Key))return false;Key other=(Key)value;
            return Shader==other.Shader&&Texture==other.Texture&&Queue==other.Queue&&Color==other.Color;
        }
    }
    private sealed class Group
    {
        public Material Material;
        public Renderer Template;
        public readonly List<CombineInstance> Parts=new List<CombineInstance>();
        public readonly List<Renderer> Renderers=new List<Renderer>();
    }
    private readonly List<Renderer> originals=new List<Renderer>();
    private readonly List<Mesh> meshes=new List<Mesh>();
    private readonly List<Material> materials=new List<Material>();
    private readonly List<GameObject> batches=new List<GameObject>();

    public void Build(ObjectPlacedInLevel[] placed)
    {
        var groups=new Dictionary<Key,Group>();Matrix4x4 toLocal=transform.worldToLocalMatrix;
        foreach(ObjectPlacedInLevel obj in placed)
        {
            if(obj==null||NavalEraMarker.IsWater(obj.name))continue;
            foreach(MeshFilter filter in obj.GetComponentsInChildren<MeshFilter>())
            {
                Mesh source=filter.sharedMesh;MeshRenderer renderer=filter.GetComponent<MeshRenderer>();
                if(source==null||renderer==null||!renderer.enabled||source.subMeshCount!=1)continue;
                Material material=renderer.sharedMaterial;if(material==null||material.shader==null)continue;
                Color32 color=material.HasProperty("_Color")?(Color32)material.color:new Color32(255,255,255,255);
                int packed=color.r|(color.g<<8)|(color.b<<16)|(color.a<<24);
                Texture texture=material.mainTexture;
                Key key=new Key{Shader=material.shader.GetInstanceID(),Texture=texture!=null?texture.GetInstanceID():0,Queue=material.renderQueue,Color=packed};
                Group group;if(!groups.TryGetValue(key,out group)){group=new Group{Material=material,Template=renderer};groups.Add(key,group);}
                group.Parts.Add(new CombineInstance{mesh=source,subMeshIndex=0,transform=toLocal*filter.transform.localToWorldMatrix});
                group.Renderers.Add(renderer);
            }
        }
        int sourceCount=0;
        foreach(Group group in groups.Values)
        {
            if(group.Parts.Count<2)continue;
            Mesh combined=new Mesh();combined.name="Naval map static batch";combined.indexFormat=IndexFormat.UInt32;
            combined.CombineMeshes(group.Parts.ToArray(),true,true);meshes.Add(combined);
            Material material=new Material(group.Material);materials.Add(material);
            GameObject batch=new GameObject("NavalPrototype_MapBatch");batch.layer=group.Template.gameObject.layer;
            batch.transform.SetParent(transform,false);batch.AddComponent<MeshFilter>().sharedMesh=combined;
            MeshRenderer output=batch.AddComponent<MeshRenderer>();output.sharedMaterial=material;
            output.shadowCastingMode=group.Template.shadowCastingMode;output.receiveShadows=group.Template.receiveShadows;
            batches.Add(batch);
            foreach(Renderer original in group.Renderers){original.enabled=false;originals.Add(original);sourceCount++;}
        }
        Debug.Log("[NavalPrototype] Static map rendering: "+sourceCount+" renderers -> "+batches.Count+" batches; colliders unchanged.");
    }
    private void OnDestroy()
    {
        foreach(Renderer renderer in originals)if(renderer!=null)renderer.enabled=true;
        foreach(GameObject batch in batches)if(batch!=null)Destroy(batch);
        foreach(Mesh mesh in meshes)if(mesh!=null)Destroy(mesh);
        foreach(Material material in materials)if(material!=null)Destroy(material);
    }
}
