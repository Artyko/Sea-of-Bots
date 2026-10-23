using UnityEngine;
[DefaultExecutionOrder(360)]
public sealed class NavalCannonPartsVisual : MonoBehaviour
{
 private CharacterModel model;
 private Transform left,right;
 private Quaternion leftRest,rightRest;
 private bool manual,previousManual;
 private GameObject box;
 private Material wood,iron;
 private int mode;
 private float footOffset;
 private bool planks;
 public void Initialize(CharacterModel character,float offset,bool boards=false)
 {
  model=character;footOffset=offset;planks=boards;
  if(model!=null&&model.UpperAnimator!=null)foreach(var bone in model.UpperAnimator.GetComponentsInChildren<Transform>(true))
  {if(bone.name=="HandL")left=Arm(bone);if(bone.name=="HandR")right=Arm(bone);}
  wood=new Material(Shader.Find("Unlit/Color"));wood.color=new Color(.43f,.27f,.12f);
  iron=new Material(Shader.Find("Unlit/Color"));iron.color=new Color(.19f,.21f,.23f);
  box=new GameObject(boards?"Carried repair planks":"Carried cannon spare parts");
  if(boards){for(int i=0;i<3;i++)Cube(new Vector3(0,i*.14f,0),new Vector3(1.15f,.11f,.24f),wood);}
  else {
  Cube(new Vector3(0,0,0),new Vector3(.65f,.3f,.42f),wood);
  for(int i=-1;i<=1;i++)Cube(new Vector3(i*.18f,.23f,0),new Vector3(.10f,.18f,.26f),iron);
  }
  box.SetActive(false);
 }
 private static Transform Arm(Transform hand){for(var p=hand.parent;p!=null;p=p.parent){string n=p.name.ToLowerInvariant();if(n.Contains("arm")||n.Contains("shoulder"))return p;}return hand;}
 private void Cube(Vector3 point,Vector3 size,Material material)
 {
  var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.transform.SetParent(box.transform,false);obj.transform.localPosition=point;obj.transform.localScale=size;
  var collider=obj.GetComponent<Collider>();collider.enabled=false;Destroy(collider);obj.GetComponent<Renderer>().sharedMaterial=material;
 }
 public void Pose(int value)
 {
  if(value==0){Hide();return;}mode=value;
  if(!manual&&model!=null){previousManual=model.IsManualUpperAnimationEnabled();if(left!=null)leftRest=left.localRotation;if(right!=null)rightRest=right.localRotation;model.SetManualUpperAnimationEnabled(true);manual=true;}
 }
 private void LateUpdate()
 {
  if(mode==0||box==null)return;
  Vector3 feet=transform.position-Vector3.up*footOffset;float bob=Mathf.Sin(Time.time*7f)*.035f;
  box.SetActive(true);box.transform.position=feet+transform.forward*(mode==3?.8f:.58f)+Vector3.up*(mode==3?(planks?.95f:.18f):1.15f+bob);
  box.transform.rotation=Quaternion.Euler(0,transform.eulerAngles.y,mode==3?0:Mathf.Sin(Time.time*4)*2);
  float hammer=mode==3?Mathf.Sin(Time.time*10)*32:mode==2?Mathf.Sin(Time.time*5)*12:0;
  if(left!=null)left.localRotation=leftRest*Quaternion.Euler(mode==3?-35:-55,-12,-8);
  if(right!=null)right.localRotation=rightRest*Quaternion.Euler(-55+hammer,12,8);
 }
 public void Hide()
 {
  mode=0;if(box!=null)box.SetActive(false);
  if(manual){if(left!=null)left.localRotation=leftRest;if(right!=null)right.localRotation=rightRest;if(model!=null)model.SetManualUpperAnimationEnabled(previousManual);manual=false;}
 }
 private void OnDisable(){Hide();}
 private void OnDestroy(){Hide();if(box!=null)Destroy(box);if(wood!=null)Destroy(wood);if(iron!=null)Destroy(iron);}
}
