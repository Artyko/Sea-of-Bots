using System.Collections.Generic;
using UnityEngine;
// Steam-era cosmetic outfit over the native damageable/animated robot model.
public sealed class NavalSteamSailorUniform:MonoBehaviour {
 private Character robot;private readonly List<Material> materials=new List<Material>();
 private readonly List<Renderer> native=new List<Renderer>();private readonly List<Material[]> originals=new List<Material[]>();
 private readonly List<GameObject> weapons=new List<GameObject>();private readonly List<GameObject> details=new List<GameObject>();
 private Material Cloth(Color color){var m=new Material(Shader.Find("Diffuse")??Shader.Find("Unlit/Color"));m.color=color;materials.Add(m);return m;}
 private void Piece(Transform bone,string name,Vector3 p,Vector3 size,Material mat,float roll){
  if(bone==null)return;var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.SetParent(bone,false);
  Vector3 scale=bone.lossyScale;obj.transform.localPosition=new Vector3(p.x/Mathf.Max(.01f,Mathf.Abs(scale.x)),p.y/Mathf.Max(.01f,Mathf.Abs(scale.y)),p.z/Mathf.Max(.01f,Mathf.Abs(scale.z)));
  obj.transform.localScale=new Vector3(size.x/Mathf.Max(.01f,Mathf.Abs(scale.x)),size.y/Mathf.Max(.01f,Mathf.Abs(scale.y)),size.z/Mathf.Max(.01f,Mathf.Abs(scale.z)));obj.transform.localRotation=Quaternion.Euler(0,0,roll);
  var shape=obj.GetComponent<Collider>();shape.enabled=false;Destroy(shape);obj.GetComponent<Renderer>().sharedMaterial=mat;details.Add(obj);
 }
 public void Initialize(Character actor,CharacterModel model){
  robot=actor;if(model==null)return;
  Material navy=Cloth(new Color(.16f,.26f,.38f)),white=Cloth(new Color(.88f,.85f,.74f)),black=Cloth(new Color(.075f,.065f,.055f)),skin=Cloth(new Color(.48f,.52f,.53f));
  Material trousers=Cloth(new Color(.25f,.30f,.34f)),stripe=Cloth(new Color(.075f,.15f,.24f)),brass=Cloth(new Color(.66f,.47f,.19f)),leather=Cloth(new Color(.27f,.16f,.085f));
  foreach(var weapon in actor.GetComponentsInChildren<WeaponModel>(true)){weapons.Add(weapon.gameObject);weapon.gameObject.SetActive(false);}
  foreach(var renderer in model.GetComponentsInChildren<Renderer>(true)){
   if(renderer.GetComponentInParent<WeaponModel>()!=null)continue;
   string name=renderer.transform.name.ToLowerInvariant();if(renderer.transform.parent!=null)name+=" "+renderer.transform.parent.name.ToLowerInvariant();
   if(name.Contains("eye")||name.Contains("jet")||name.Contains("particle"))continue;
   native.Add(renderer);originals.Add(renderer.sharedMaterials);var colors=renderer.sharedMaterials;
   Material chosen=name.Contains("head")?skin:name.Contains("foot")||name.Contains("boot")?black:name.Contains("leg")||name.Contains("thigh")||name.Contains("knee")?trousers:name.Contains("hand")?skin:navy;
   for(int i=0;i<colors.Length;i++)colors[i]=chosen;renderer.sharedMaterials=colors;
  }
  FirstPersonMover mover=actor as FirstPersonMover;Transform head=mover!=null?mover.GetHeadTransform():null;
  if(head!=null){
   // A raised crown, dark tally and rear ribbons read as a cap rather than a face band.
   Piece(head,"Cap dark tally",new Vector3(0,.33f,0),new Vector3(.68f,.10f,.61f),stripe,0);
   Piece(head,"Cap ivory crown",new Vector3(0,.44f,-.015f),new Vector3(.82f,.16f,.73f),white,0);
   Piece(head,"Cap brass badge",new Vector3(0,.34f,.315f),new Vector3(.09f,.07f,.025f),brass,0);
   Piece(head,"Cap rear ribbon left",new Vector3(-.13f,.19f,-.32f),new Vector3(.07f,.30f,.035f),stripe,-12);
   Piece(head,"Cap rear ribbon right",new Vector3(.13f,.19f,-.32f),new Vector3(.07f,.30f,.035f),stripe,12);
   Transform torso=head.parent!=null?head.parent:head;Vector3 neck=Quaternion.Inverse(torso.rotation)*(head.position-torso.position);
   Piece(torso,"Undershirt ivory inset",neck+new Vector3(0,-.65f,.23f),new Vector3(.40f,.65f,.065f),white,0);
   for(int i=0;i<5;i++)Piece(torso,"Telnyashka stripe",neck+new Vector3(0,-.40f-i*.115f,.27f),new Vector3(.40f,.035f,.018f),stripe,0);
   Piece(torso,"Navy collar back",neck+new Vector3(0,-.35f,-.23f),new Vector3(.70f,.37f,.06f),stripe,0);
   Piece(torso,"Collar left",neck+new Vector3(-.23f,-.40f,.25f),new Vector3(.15f,.48f,.065f),stripe,28);
   Piece(torso,"Collar right",neck+new Vector3(.23f,-.40f,.25f),new Vector3(.15f,.48f,.065f),stripe,-28);
   Piece(torso,"Collar piping left",neck+new Vector3(-.22f,-.40f,.29f),new Vector3(.025f,.43f,.015f),white,28);
   Piece(torso,"Collar piping right",neck+new Vector3(.22f,-.40f,.29f),new Vector3(.025f,.43f,.015f),white,-28);
   Piece(torso,"Neckerchief",neck+new Vector3(0,-.64f,.31f),new Vector3(.075f,.23f,.045f),black,0);
   Piece(torso,"Leather belt",neck+new Vector3(0,-1.02f,.02f),new Vector3(.78f,.10f,.53f),leather,0);
   Piece(torso,"Brass belt buckle",neck+new Vector3(0,-1.02f,.30f),new Vector3(.14f,.12f,.035f),brass,0);
   for(int side=-1;side<=1;side+=2){
    Piece(torso,"Jacket pocket",neck+new Vector3(side*.29f,-.81f,.26f),new Vector3(.16f,.19f,.045f),stripe,0);
    Piece(torso,"Pocket brass button",neck+new Vector3(side*.29f,-.76f,.29f),new Vector3(.035f,.035f,.02f),brass,0);
   }
   BatchDetails();
  }
 }
 private readonly List<Mesh> detailMeshes=new List<Mesh>();
 private void BatchDetails(){
  var source=details.ToArray();var parents=new HashSet<Transform>();foreach(var obj in source)parents.Add(obj.transform.parent);
  foreach(var parent in parents)foreach(var material in materials){var parts=new List<CombineInstance>();
   foreach(var obj in source)if(obj.transform.parent==parent&&obj.GetComponent<Renderer>().sharedMaterial==material)parts.Add(new CombineInstance{mesh=obj.GetComponent<MeshFilter>().sharedMesh,transform=parent.worldToLocalMatrix*obj.transform.localToWorldMatrix});
   if(parts.Count==0)continue;var mesh=new Mesh();mesh.name="Sailor uniform details";mesh.CombineMeshes(parts.ToArray(),true,true);detailMeshes.Add(mesh);
   var batch=new GameObject("Sailor clothing");batch.transform.SetParent(parent,false);batch.AddComponent<MeshFilter>().sharedMesh=mesh;batch.AddComponent<MeshRenderer>().sharedMaterial=material;details.Add(batch);
  }
  foreach(var obj in source){details.Remove(obj);Destroy(obj);}
 }
 private void LateUpdate(){if(robot==null||!robot.IsAlive()){foreach(var obj in details)if(obj!=null)obj.SetActive(false);return;}
  foreach(var weapon in weapons)if(weapon!=null&&weapon.activeSelf)weapon.SetActive(false);
 }
 private void OnDestroy(){for(int i=0;i<native.Count;i++)if(native[i]!=null)native[i].sharedMaterials=originals[i];foreach(var obj in details)if(obj!=null)Destroy(obj);foreach(var mat in materials)if(mat!=null)Destroy(mat);foreach(var mesh in detailMeshes)if(mesh!=null)Destroy(mesh);}
}
