using UnityEngine;
[DefaultExecutionOrder(350)]
public sealed class NavalSailHaulingAnimation : MonoBehaviour
{
    private CharacterModel model;
    private Transform left,right,leftHand,rightHand;
    private Quaternion leftRest,rightRest;
    private bool pulling,wasManual;
    private Vector3 anchor;
    private LineRenderer rope;
    private Material material;
    public void Initialize(CharacterModel character)
    {
        model=character;if(model==null||model.UpperAnimator==null)return;
        foreach(var bone in model.UpperAnimator.GetComponentsInChildren<Transform>(true))
        {if(bone.name=="HandL"){leftHand=bone;left=Arm(bone);}if(bone.name=="HandR"){rightHand=bone;right=Arm(bone);}}
        GameObject line=new GameObject("Sail hauling rope");line.transform.SetParent(transform,false);
        rope=line.AddComponent<LineRenderer>();rope.useWorldSpace=true;rope.positionCount=4;rope.startWidth=rope.endWidth=.035f;
        material=new Material(Shader.Find("Unlit/Color"));material.color=new Color(.62f,.46f,.25f);rope.sharedMaterial=material;rope.enabled=false;
        rope.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;rope.receiveShadows=false;
    }
    private static Transform Arm(Transform hand)
    {
        for(var p=hand.parent;p!=null;p=p.parent)
        {string name=p.name.ToLowerInvariant();if(name.Contains("arm")||name.Contains("shoulder"))return p;}
        return hand;
    }
    public void Haul(Vector3 ropeAnchor)
    {
        anchor=ropeAnchor;if(model==null)return;
        if(!pulling)
        {
            wasManual=model.IsManualUpperAnimationEnabled();
            if(left!=null)leftRest=left.localRotation;if(right!=null)rightRest=right.localRotation;
            model.SetManualUpperAnimationEnabled(true);pulling=true;
        }
    }
    private void LateUpdate()
    {
        if(!pulling)return;
        float phase=Time.time*5f,pull=(Mathf.Sin(phase)+1)*.5f,other=(Mathf.Sin(phase+1.1f)+1)*.5f;
        if(left!=null)left.localRotation=leftRest*Quaternion.Euler(-25-pull*45,-12,-8);
        if(right!=null)right.localRotation=rightRest*Quaternion.Euler(-25-other*45,12,8);
        if(rope!=null)
        {
            Vector3 hand=transform.position+transform.forward*(.65f-pull*.3f)+Vector3.up*.2f;
            Vector3 leftGrip=leftHand!=null?leftHand.position:hand-transform.right*.15f;
            Vector3 rightGrip=rightHand!=null?rightHand.position:hand+transform.right*.15f+Vector3.down*(other*.15f);
            rope.enabled=true;rope.SetPosition(0,anchor);rope.SetPosition(1,leftGrip);
            rope.SetPosition(2,rightGrip);rope.SetPosition(3,rightGrip+Vector3.down*.65f);
        }
    }
    public void StopHauling()
    {
        if(pulling){if(left!=null)left.localRotation=leftRest;if(right!=null)right.localRotation=rightRest;if(model!=null)model.SetManualUpperAnimationEnabled(wasManual);}
        pulling=false;if(rope!=null)rope.enabled=false;
    }
    private void OnDisable(){StopHauling();}
    private void OnDestroy(){StopHauling();if(material!=null)Destroy(material);}
}
