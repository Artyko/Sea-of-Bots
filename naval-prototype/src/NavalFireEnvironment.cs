using UnityEngine;
public sealed partial class NavalFireSystem
{
    private float smokeClock;
    private Matrix4x4 smokePose,previousSmokePose;
    private int smokePoseFrames;
    private float smokeFrameDt;
    private void TickSmokeMotion()
    {
        previousSmokePose=smokePose;smokePose=hull.Root.localToWorldMatrix;
        smokeFrameDt=Mathf.Max(.001f,Time.deltaTime);smokePoseFrames++;
    }
    private Vector3 SmokeSourceVelocity(Vector3 point)
    {return smokePoseFrames<2?Vector3.zero:Vector3.ClampMagnitude((point-previousSmokePose.MultiplyPoint3x4(hull.Root.InverseTransformPoint(point)))/smokeFrameDt,15);}
    private readonly int[] smokeLoad=new int[NavalSmokePlumeMath.MaxColumns];
    // Wetting only: incoming water volume is already handled by flooding.
    public void Wash(Vector3 origin,Vector3 direction,float flow)
    {
        if(flow<=.00001f)return;
        Vector3 end=origin+direction.normalized*2.5f+Vector3.down*.8f;
        foreach(var target in flames)if(target.Active&&InWash(target.Cell.transform.position,origin,end))target.Flame.Douse(Time.time);
        foreach(var target in powder)if(target.Active&&
            (InWash(hull.Root.TransformPoint(target.PowderPoint),origin,end)||InWash(hull.Root.TransformPoint(target.PowderPoint)-hull.Root.up*(NavalPowderLayout.Height*.5f),origin,end)))target.Powder.Douse();
    }
    private static bool InWash(Vector3 p,Vector3 a,Vector3 b)
    {return NavalFireEnvironmentMath.Washed(p.x,p.y,p.z,a.x,a.y,a.z,b.x,b.y,b.z);}
    private bool WaterTouches(Vector3 point)
    {
        var world=NavalTestWorld.Active;if(world==null)return false;
        if(world.WaterLevelAt(point)>=point.y-.03f)return true;
        Vector3 local=hull.Root.InverseTransformPoint(point);
        // Ocean contact on exposed deck; dry hold remains protected by the hull.
        return NavalFireEnvironmentMath.SeaContact(local.y,hull.Definition.WeatherDeck,hull.WorldScale,world.OceanLevelAt(point),point.y);
    }
    private void TickFireWater()
    {
        foreach(var target in flames)if(target.Active&&WaterTouches(target.Cell.transform.position))target.Flame.Douse(Time.time);
        foreach(var target in powder)if(target.Active){Vector3 basePoint=hull.Root.TransformPoint(target.PowderPoint)-hull.Root.up*(NavalPowderLayout.Height*.5f);if(WaterTouches(basePoint))target.Powder.Douse();}
    }
    private readonly System.Collections.Generic.List<NavalFireTarget> smokeSources=new System.Collections.Generic.List<NavalFireTarget>(4);
    private Vector3 SmokeOrigin(NavalFireTarget target)
    {return target.Powder!=null?hull.Root.TransformPoint(target.PowderPoint)+hull.Root.up*(NavalPowderLayout.Height*.5f):target.Cell.transform.position+Vector3.up*.3f;}
    private void EmitFireSmoke(float dt)
    {
        smokeClock+=dt;if(smokeClock<NavalSmokePlumeMath.ColumnInterval)return;smokeClock-=NavalSmokePlumeMath.ColumnInterval;
        // Keep each cluster's emitter stable; do not hop between burning voxels.
        for(int i=smokeSources.Count-1;i>=0;i--)if(!smokeSources[i].Active||WaterTouches(SmokeOrigin(smokeSources[i])))smokeSources.RemoveAt(i);
        for(int i=0;i<TargetCount&&smokeSources.Count<NavalSmokePlumeMath.MaxColumns;i++)
        {
            var candidate=TargetAt(i);if(!candidate.Active)continue;Vector3 point=SmokeOrigin(candidate);
            bool covered=false;foreach(var existing in smokeSources)if((SmokeOrigin(existing)-point).sqrMagnitude<2.2f*2.2f){covered=true;break;}
            if(!covered&&!WaterTouches(point))smokeSources.Add(candidate);
        }
        System.Array.Clear(smokeLoad,0,smokeLoad.Length);
        for(int i=0;i<TargetCount;i++)
        {
            var fire=TargetAt(i);if(!fire.Active||smokeSources.Count==0)continue;
            Vector3 p=SmokeOrigin(fire);int nearest=0;float best=float.PositiveInfinity;
            for(int c=0;c<smokeSources.Count;c++){float d=(p-SmokeOrigin(smokeSources[c])).sqrMagnitude;if(d<best){best=d;nearest=c;}}
            smokeLoad[nearest]+=fire.Powder!=null?4:1;
        }
        int budget=NavalSmokePlumeMath.MaxColumnPuffsPerTick;
        for(int column=0;column<smokeSources.Count;column++)
        {
            var target=smokeSources[column];Vector3 point=SmokeOrigin(target);float duration=NavalSmokePlumeMath.Lifetime;
            RaycastHit ceiling;
            if(Physics.Raycast(point+Vector3.up*.15f,Vector3.up,out ceiling,30f,PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore))
                duration=Mathf.Min(duration,Mathf.Max(.15f,(ceiling.distance-.3f)/NavalSmokePlumeMath.RiseSpeed));
            int spare=Mathf.Max(0,budget-(smokeSources.Count-column));
            int count=1+Mathf.Min(spare,NavalSmokePlumeMath.Puffs(smokeLoad[column])-1);budget-=count;
            float size=NavalSmokePlumeMath.FireSize(smokeLoad[column]);Vector3 velocity=SmokeSourceVelocity(point);
            for(int puff=0;puff<count;puff++)
            {
                Vector3 spread=count==1?Vector3.zero:new Vector3((puff-(count-1)*.5f)*size*.35f,0,0);
                effects.EmitFireColumn(point+spread,velocity,smoke,duration,size);
            }
        }
    }
}
