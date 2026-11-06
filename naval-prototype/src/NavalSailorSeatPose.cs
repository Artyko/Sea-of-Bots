using UnityEngine;

public partial class NavalRepairSailor
{
    private NavalSailorSeatPose mainBatterySeatPose;
    public void SetMainBatterySeated(bool value)
    {
        if(mainBatterySeatPose==null&&value)
        {
            mainBatterySeatPose=gameObject.AddComponent<NavalSailorSeatPose>();
            mainBatterySeatPose.Initialize(robot,model);
        }
        if(mainBatterySeatPose!=null)
        {
            bool wasSeated=mainBatterySeatPose.Seated;
            mainBatterySeatPose.Seated=value;
            if(wasSeated!=value)Debug.Log("[NavalPrototype] Main battery crew seat: sailor="+(robot!=null?robot.GetInstanceID():0)+" seated="+value);
            // The gait sampler caches an already-applied idle pose.  Sitting is
            // sampled after it, so without invalidation the cached idle never
            // overwrites the chair clip when this sailor stands up.
            if(wasSeated&&!value&&gait!=null)gait.InvalidatePose();
        }
    }
}

// Applied after the walking sampler, so a native sitting clip can hold the
// gun crew on its seats without changing controller height or route physics.
[DefaultExecutionOrder(310)]
public sealed class NavalSailorSeatPose:MonoBehaviour
{
    private Character sailor;private CharacterModel model;private AnimationClip sitting;private Transform root;
    public bool Seated;
    public void Initialize(Character character,CharacterModel body)
    {
        sailor=character;model=body;
        if(model==null||model.LegsAnimator==null||model.LegsAnimator.runtimeAnimatorController==null)return;
        root=model.LegsAnimator.transform;int best=-1;
        foreach(AnimationClip clip in model.LegsAnimator.runtimeAnimatorController.animationClips)
        {
            string name=clip.name.ToLowerInvariant();
            if(!name.Contains("sit")&&!name.Contains("seated"))continue;
            int score=(name.Contains("idle")?8:0)+(name.Contains("chair")?6:0)-(name.Contains("down")||name.Contains("up")?4:0);
            if(score>best){best=score;sitting=clip;}
        }
        if(sitting!=null)Debug.Log("[NavalPrototype] Main battery seated crew pose: "+sitting.name);
        else Debug.LogWarning("[NavalPrototype] No native seated clip; main battery crew remain positioned on the internal seats");
    }
    private void LateUpdate()
    {
        using(NavalPerformance.Measure(NavalPerfArea.CrewSeatAnimation)) TickLate();
    }
    private void TickLate()
    {
        if(!Seated||sitting==null||root==null||sailor==null||!sailor.IsAlive())return;
        Vector3 position=root.localPosition;Quaternion rotation=root.localRotation;
        sitting.SampleAnimation(root.gameObject,Mathf.Min(.15f,sitting.length*.25f));
        root.localPosition=position;root.localRotation=rotation;
    }
}
