using UnityEngine;
using UnityEngine.Rendering;
[DefaultExecutionOrder(450)]
public class NavalFloodAppearance : MonoBehaviour
{
    private NavalWater ocean;
    private NavalHull hull;
    private Material material;
    private Renderer[] renderers;
    private Color tint;
    private bool hidden;
    private float previous=-1f;
    public void Initialize(NavalWater sea,NavalHull owner,Transform[] cells)
    {
        hull=owner;
        ocean=sea;material=new Material(sea.InteriorMaterial);tint=material.color;
        // Fade all shading, including specular highlights, rather than retaining
        // Standard transparent-mode reflections at zero diffuse alpha.
        if(material.shader.name=="Standard")
        {
            material.SetFloat("_Mode",2f);material.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);material.SetInt("_ZWrite",0);
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");material.DisableKeyword("_ALPHATEST_ON");material.EnableKeyword("_ALPHABLEND_ON");
            material.SetOverrideTag("RenderType","Transparent");material.renderQueue=3000;
        }
        renderers=new Renderer[cells.Length];
        for(int i=0;i<cells.Length;i++)
        {renderers[i]=cells[i].GetComponent<Renderer>();renderers[i].sharedMaterial=material;}
    }
    public static float Clearance(Transform boat,NavalWater sea)
    {
        float clearance=float.NegativeInfinity;
        NavalSeaLadder equipment=boat.GetComponent<NavalSeaLadder>();
        float top=equipment!=null?equipment.Hull.Definition.AboveDeckY(2.25f):2.25f;
        // The hull and rails, not the still-protruding mast, define full submergence.
        for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)
        {
            Vector3 point=boat.TransformPoint(new Vector3(x*BoatDimensions.HalfWidth,top,z*BoatDimensions.HalfLength));
            clearance=Mathf.Max(clearance,point.y-sea.Height(point.x,point.z));
        }
        return clearance;
    }
    private void LateUpdate()
    {
        if(ocean==null||material==null)return;
        float clearance=Clearance(transform,ocean);
        float visibility=NavalFloodVisibilityMath.Visibility(clearance);
        if(Mathf.Abs(previous-visibility)>0.001f)
        {Color color=tint;color.a=tint.a*visibility;material.color=color;previous=visibility;}
        bool hide=NavalFloodVisibilityMath.HideEffects(clearance);
        hull.FloodEffectsHidden=hide;
        if(hide!=hidden)
        {
            hidden=hide;
            for(int i=0;i<renderers.Length;i++)if(renderers[i]!=null)
            {renderers[i].enabled=!hide;renderers[i].gameObject.SetActive(!hide&&hull.Flood.Depth(i)>0.0005f);}
            if(hide)foreach(NavalHullVoxel cell in hull.Voxels)
                if(cell.Stream!=null)cell.Stream.gameObject.SetActive(false);
        }
    }
    private void OnDestroy(){if(material!=null)Destroy(material);}
}
