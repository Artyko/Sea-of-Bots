using UnityEngine;

// Tactical intent only. The existing navigator still handles shores, contacts and reversing.
public sealed class NavalCombatPosition
{
    private readonly NavalFiringPositionMath plan=new NavalFiringPositionMath();
    private readonly NavalEngagementRange rangePlan=new NavalEngagementRange();
    private Transform opponent;
    private float reviewAt;
    private Vector3 anchor;
    private int workingSide;

    public void Reset(){opponent=null;reviewAt=0f;workingSide=0;plan.Reset();rangePlan.Reset(0);}
    public Vector3 Course(Transform own,NavalHull hull,NavalStations guns,Transform enemy,Vector3 enemyVelocity,float ownSpeed,ref float throttle)
    {
        NavalStations other=NavalStations.ForShip(enemy);
        if(other==null)return enemy.position-own.position;
        if(opponent!=enemy){Reset();opponent=enemy;rangePlan.Reset(own.GetInstanceID()^Random.Range(1,int.MaxValue));}
        Vector3 toward=enemy.position-own.position;toward.y=0f;
        float distance=toward.magnitude;Vector3 radial=toward.normalized;
        bool reachable=guns.CanReachRange(enemy.position);
        rangePlan.Step(Mathf.Min(Time.deltaTime,.05f),distance,reachable,hull.WorldScale,other.ShipScale);
        float radius=rangePlan.Desired;
        if(Time.time>=reviewAt)
        {
            reviewAt=Time.time+.35f;
            Vector3 forward=enemy.forward;forward.y=0f;forward.Normalize();
            Vector3 right=Vector3.Cross(Vector3.up,forward);
            Vector3 offset=own.position-enemy.position;float x,z;
            plan.Select(Vector3.Dot(offset,right),Vector3.Dot(offset,forward),radius,other.PortWorking,other.StarboardWorking,out x,out z);
            // Follow a moving blind sector, with a bounded two-second lead.
            Vector3 lead=Vector3.ClampMagnitude(enemyVelocity*2f,20f);lead.y=0f;
            anchor=enemy.position+right*x+forward*z+lead;
        }
        float bearing=Mathf.Atan2(radial.x,radial.z)*Mathf.Rad2Deg;
        workingSide=NavalBroadsideMath.HelmSide(own.eulerAngles.y,bearing,guns.PortWorking,guns.StarboardWorking,
            workingSide,guns.FiringSideToward(enemy.position));
        float broadsideBearing=NavalBroadsideMath.Heading(bearing,workingSide);
        Vector3 broadside=Quaternion.Euler(0f,broadsideBearing,0f)*Vector3.forward;
        Vector3 correction=anchor-own.position;correction.y=0f;
        float blend=Mathf.Clamp01((correction.magnitude-10f)/35f);
        Vector3 cruise=Vector3.Lerp(broadside,correction.normalized,blend);
        float cruiseBearing=cruise.sqrMagnitude>.001f?Mathf.Atan2(cruise.x,cruise.z)*Mathf.Rad2Deg:broadsideBearing;
        float clearance=NavalHelmTactics.Clearance(hull.WorldScale,other.ShipScale);
        float wanted;
        if(reachable||rangePlan.KeepBroadside)
        {
            wanted=NavalEngagementRange.Course(bearing,workingSide,distance,rangePlan.Desired,false);
            // Small stable variation stays inside the gun traverse; never swap to a nose-on chase here.
            wanted+=rangePlan.CourseBias+Mathf.Clamp(Mathf.DeltaAngle(wanted,cruiseBearing),-2f,2f);
        }
        else wanted=NavalEngagementRange.ApproachCourse(bearing,workingSide,distance,ownSpeed,Vector3.Dot(enemyVelocity,radial),rangePlan.CourseBias*.3f);
        Vector3 course=Quaternion.Euler(0f,wanted,0f)*Vector3.forward;
        float separation=NavalHelmTactics.Approach(distance,hull.WorldScale,other.ShipScale);
        if(distance<clearance&&separation<0f)course= broadside+radial*separation;
        // Keep enough headway to complete the turn against a moving target.
        bool aligning=Mathf.Abs(Mathf.DeltaAngle(own.eulerAngles.y,broadsideBearing))>20f;
        if(!reachable&&!rangePlan.KeepBroadside)throttle=1f;
        else if(aligning||distance>rangePlan.Desired+8f)throttle=Mathf.Max(throttle,.8f);
        else throttle=Mathf.Min(throttle,rangePlan.CruiseThrottle);
        return course.sqrMagnitude>.001f?course:broadside;
    }
}
