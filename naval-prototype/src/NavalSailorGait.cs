using UnityEngine;

// Sample actual animation clips after all ship/crew movement. This avoids
// relying on a disabled Animator evaluating its state machine automatically.
[DefaultExecutionOrder(300)]
public class NavalSailorGait : MonoBehaviour
{
    private Character robot;
    private CharacterModel model;
    private AnimationClip walk,idle;
    private Transform root;
    private Transform[] bones;
    private Vector3[] idlePosition;
    private Quaternion[] idleRotation;
    private readonly NavalGaitClock clock=new NavalGaitClock();
    private float stride,weight,lastMove=-100f;
    private bool idleApplied,nativeInjury;
    public void Initialize(Character sailor,CharacterModel body)
    {
        robot=sailor;model=body;
        if(model==null||model.LegsAnimator==null||model.LegsAnimator.runtimeAnimatorController==null)return;
        root=model.LegsAnimator.transform;
        int best=-1;
        foreach(AnimationClip clip in model.LegsAnimator.runtimeAnimatorController.animationClips)
        {
            string name=clip.name.ToLowerInvariant();
            if(name.Contains("idle")&&!name.Contains("dangling")&&idle==null)idle=clip;
            if(!name.Contains("walk")&&!name.Contains("run"))continue;
            if(name.Contains("back")||name.Contains("left")||name.Contains("right")||name.Contains("oneleg"))continue;
            int score=(name.Contains("forward")?20:0)+(name.Contains("walk")?10:0)+(name.Contains("hammer")?4:0);
            if(score>best&&clip.length>0f){best=score;walk=clip;}
        }
        if(walk==null){Debug.LogWarning("[NavalPrototype] No forward leg clip for sailor "+robot.name);return;}
        stride=walk.averageSpeed.magnitude*walk.length*robot.transform.lossyScale.x;
        if(stride<0.5f)stride=2.8f*robot.transform.lossyScale.x;
        bones=root.GetComponentsInChildren<Transform>(true);idlePosition=new Vector3[bones.Length];idleRotation=new Quaternion[bones.Length];
        Vector3[] savedP=new Vector3[bones.Length];Quaternion[] savedR=new Quaternion[bones.Length];
        for(int i=0;i<bones.Length;i++){savedP[i]=bones[i].localPosition;savedR[i]=bones[i].localRotation;}
        if(idle!=null)idle.SampleAnimation(root.gameObject,0f);
        for(int i=0;i<bones.Length;i++)
        {idlePosition[i]=bones[i].localPosition;idleRotation[i]=bones[i].localRotation;bones[i].localPosition=savedP[i];bones[i].localRotation=savedR[i];}
        Debug.Log("[NavalPrototype] Sailor gait: "+walk.name+", stride "+stride.ToString("F2")+" m");
    }
    public void Moved(float distance)
    {if(distance>0.002f){clock.Advance(distance,stride);lastMove=Time.time;}}
    public void Stop(){lastMove=-100f;}
    private void LateUpdate()
    {
        if(robot==null||!robot.IsAlive()){Restore();enabled=false;return;}
        bool rightDamaged=robot.IsDamaged(MechBodyPartType.RightLeg),leftDamaged=robot.IsDamaged(MechBodyPartType.LeftLeg);
        if((rightDamaged||leftDamaged)&&model!=null&&model.LegsAnimator!=null)
        {
            // The game's animator owns one-legged hopping/crawling. Sampling
            // the intact run clip here would overwrite its injury animation.
            nativeInjury=true;idleApplied=false;model.SetManualLegsAnimationEnabled(false);
            model.SetLegsBool("IsOneLeggedLeft",rightDamaged&&!leftDamaged);
            model.SetLegsBool("IsOneLeggedRight",leftDamaged&&!rightDamaged);
            model.SetLegsBool("IsCrawling",rightDamaged&&leftDamaged);
            model.SetLegsBool("IsMoving",Time.time-lastMove<.08f);
            FirstPersonMover mover=robot as FirstPersonMover;
            if(mover!=null)model.SetLegsFloat("RunSpeed",mover.BaseAnimatorRunSpeed*(rightDamaged&&leftDamaged?1f:mover.OneLeggedAnimatorRunSpeedMultiplier));
            return;
        }
        if(nativeInjury)
        {
            nativeInjury=false;idleApplied=false;
            model.SetLegsBool("IsOneLeggedLeft",false);model.SetLegsBool("IsOneLeggedRight",false);model.SetLegsBool("IsCrawling",false);
        }
        if(root==null||walk==null)return;
        weight=Mathf.MoveTowards(weight,Time.time-lastMove<0.08f?1f:0f,Time.deltaTime*6f);
        if(weight==0f&&idleApplied)return;
        idleApplied=weight==0f;
        model.SetManualLegsAnimationEnabled(true);
        Vector3 position=root.localPosition;Quaternion rotation=root.localRotation;
        walk.SampleAnimation(root.gameObject,clock.Phase*walk.length);
        for(int i=0;i<bones.Length;i++)if(bones[i]!=null&&bones[i]!=root&&bones[i].IsChildOf(root))
        {
            bones[i].localPosition=Vector3.Lerp(idlePosition[i],bones[i].localPosition,weight);
            bones[i].localRotation=Quaternion.Slerp(idleRotation[i],bones[i].localRotation,weight);
        }
        // A clip may contain root motion; locomotion belongs to the controller.
        root.localPosition=position;root.localRotation=rotation;
    }
    private void Restore()
    {if(model!=null){model.SetManualLegsAnimationEnabled(false);if(model.LegsAnimator!=null)model.LegsAnimator.speed=1f;}}
    private void OnDisable(){idleApplied=false;Restore();}
    private void OnDestroy(){Restore();}
}
