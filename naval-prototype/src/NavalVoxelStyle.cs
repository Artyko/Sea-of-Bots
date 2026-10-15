using System.Collections.Generic;
using UnityEngine;

// Stable face shading keeps the voxel silhouette readable even with the
// arena's unlit materials. One mesh and a small material cache per ship.
public class NavalVoxelStyle : MonoBehaviour
{
    private Mesh cube;
    private readonly Dictionary<Material, Material> shades = new Dictionary<Material, Material>();
    private readonly List<Texture2D> textures = new List<Texture2D>();
    private Material[] scorch;
    public void Scorch(GameObject cell,float strength)
    {
        if(scorch==null)
        {
            Shader shader=Shader.Find("Unlit/Color");if(shader==null)shader=Shader.Find("Standard");
            Color[] colors={new Color(0.20f,0.115f,0.06f),new Color(0.105f,0.065f,0.035f),new Color(0.035f,0.025f,0.02f)};
            scorch=new Material[3];
            for(int i=0;i<3;i++){scorch[i]=new Material(shader);scorch[i].color=colors[i];}
        }
        Material material=scorch[strength>0.65f?2:(strength>0.3f?1:0)];
        cell.GetComponent<Renderer>().sharedMaterial=material;Apply(cell,material);
    }
    public void Apply(GameObject cell, Material original)
    {
        Shader shader=Shader.Find("Unlit/Texture");
        if(shader==null) return;
        if(cube==null) BuildMesh();
        Material palette;
        if(!shades.TryGetValue(original,out palette))
        {
            float[] light={1.08f,0.68f,0.90f,0.78f,0.97f,0.84f};
            Texture2D texture=new Texture2D(6,1,TextureFormat.RGBA32,false);
            texture.filterMode=FilterMode.Point; texture.wrapMode=TextureWrapMode.Clamp;
            for(int i=0;i<6;i++)
            {
                Color color=original.color*light[i]; color.a=original.color.a;
                texture.SetPixel(i,0,color);
            }
            texture.Apply(); textures.Add(texture);
            palette=new Material(shader); palette.mainTexture=texture;
            shades.Add(original,palette);
        }
        cell.GetComponent<MeshFilter>().sharedMesh=cube;
        cell.GetComponent<Renderer>().sharedMaterial=palette;
    }
    private void BuildMesh()
    {
        cube=new Mesh(); cube.name="NavalPrototype_ShadedVoxel";
        Vector3[] normals={Vector3.up,Vector3.down,Vector3.right,Vector3.left,Vector3.forward,Vector3.back};
        Vector3[] vertices=new Vector3[24];
        Vector2[] uv=new Vector2[24];
        int[] triangles=new int[36];
        for(int i=0;i<6;i++)
        {
            Vector3 n=normals[i];
            Vector3 u=Mathf.Abs(n.y)>0.5f?Vector3.right:Vector3.up;
            Vector3 v=Vector3.Cross(n,u);
            vertices[i*4]=(n-u-v)*0.5f; vertices[i*4+1]=(n+u-v)*0.5f;
            vertices[i*4+2]=(n+u+v)*0.5f; vertices[i*4+3]=(n-u+v)*0.5f;
            for(int corner=0;corner<4;corner++) uv[i*4+corner]=new Vector2((i+0.5f)/6f,0.5f);
            int b=i*4, t=i*6;
            triangles[t]=b; triangles[t+1]=b+1; triangles[t+2]=b+2;
            triangles[t+3]=b; triangles[t+4]=b+2; triangles[t+5]=b+3;
        }
        cube.vertices=vertices; cube.uv=uv; cube.triangles=triangles;
        cube.RecalculateNormals(); cube.RecalculateBounds();
    }
    private void OnDestroy()
    {
        if(scorch!=null)foreach(Material material in scorch)Destroy(material);
        foreach(Material material in shades.Values) Destroy(material);
        foreach(Texture2D texture in textures) Destroy(texture);
        if(cube!=null) Destroy(cube);
    }
}
