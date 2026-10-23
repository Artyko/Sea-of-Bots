using UnityEngine;
// Built before fire indexing, attachment graph and render/collider batching.
public sealed class NavalFurnishings : MonoBehaviour
{
 private Material rope,canvas,glow;
 private static Material Tint(Color color){var material=new Material(Shader.Find("Unlit/Color"));material.color=color;return material;}
 public void Initialize(NavalHull hull,Material wood,Material trim,Material iron)
 {
  rope=Tint(new Color(.57f,.44f,.25f));canvas=Tint(new Color(.72f,.67f,.54f));glow=Tint(new Color(.92f,.58f,.21f));
  hull.RegisterFireMaterial(wood);hull.RegisterFireMaterial(trim);hull.RegisterFireMaterial(rope);hull.RegisterFireMaterial(canvas);
  var palette=new Material[]{wood,trim,iron,rope,canvas,glow};int first=hull.Voxels.Count;
  foreach(var box in NavalFurnishingLayout.Build(hull.Definition)){
   if(hull.Cargo!=null&&(box.Item.Contains("cask")||box.Item.Contains("Cask")||box.Item.Contains("Cargo crate")||box.Item=="Lashed cargo crate"))continue;
   int begin=hull.Voxels.Count;
   hull.AddDecorationBox(hull.Root,palette[box.Palette],new Vector3(box.X,box.Y,box.Z),new Vector3(box.Width,box.Height,box.Depth),true);
   for(int i=begin;i<hull.Voxels.Count;i++)hull.Voxels[i].gameObject.name="NavalPrototype_"+box.Item;
  }
  Debug.Log("[NavalPrototype] Ship furnishings: "+hull.Definition.Name+" | "+(hull.Voxels.Count-first)+" damageable cells; shared static batching.");
 }
 private void OnDestroy(){if(rope!=null)Destroy(rope);if(canvas!=null)Destroy(canvas);if(glow!=null)Destroy(glow);}
}
