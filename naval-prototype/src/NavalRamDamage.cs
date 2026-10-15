using UnityEngine;
using System.Collections.Generic;
public class NavalRamDamage : MonoBehaviour
{
    public NavalHull Hull;
    public Vector3 Velocity;
    public float AngularRate;
    public float LastContact=float.NegativeInfinity;
    private float lastDamage=float.NegativeInfinity;
    private float lastRigDamage=float.NegativeInfinity;
    public readonly List<NavalHullVoxel> RigCells=new List<NavalHullVoxel>();
    public NavalRamDamage Opponent;
    public Vector3 ContactLocal;
    public void Initialize(NavalHull hull)
    {Hull=hull;foreach(NavalHullVoxel cell in hull.Voxels)if(cell.ManagedRig)RigCells.Add(cell);}
    public void RigImpact(Vector3 point,float speed)
    {
        if(speed<1.2f||Time.time-lastRigDamage<2f)return;
        Vector3 local=transform.InverseTransformPoint(point);NavalHullVoxel closest=null;float best=0.8f*0.8f;
        foreach(NavalHullVoxel cell in Hull.Voxels)
        {
            if(!cell.HydraulicShell||cell.Missing||cell.Detached)continue;
            float distance=(cell.LocalCenter-local).sqrMagnitude;
            if(distance<best){best=distance;closest=cell;}
        }
        if(closest==null)return;
        lastRigDamage=Time.time;Hull.Damage(closest.LocalCenter,0.01f);
    }
    private static Vector3[] Corners(Transform ship)
    {
        Quaternion flat=Quaternion.Euler(0f,ship.eulerAngles.y,0f);
        Vector3[] points=new Vector3[4];
        for(int i=0;i<4;i++)points[i]=ship.position+flat*new Vector3((i==0||i==3?-1f:1f)*NavalShipContactMath.HalfWidth,0f,(i<2?-1f:1f)*NavalShipContactMath.HalfLength);
        for(int i=0;i<4;i++)points[i].y=0f;return points;
    }
    public static void Closest(Transform a,Transform b,out Vector3 pa,out Vector3 pb)
    {
        Vector3[] aa=Corners(a),bb=Corners(b);pa=aa[0];pb=bb[0];float best=float.MaxValue;
        for(int side=0;side<2;side++)for(int i=0;i<4;i++)for(int j=0;j<4;j++)
        {
            Vector3 p=side==0?aa[i]:bb[i],start=side==0?bb[j]:aa[j],end=side==0?bb[(j+1)%4]:aa[(j+1)%4];
            Vector3 edge=end-start,q=start+edge*Mathf.Clamp01(Vector3.Dot(p-start,edge)/edge.sqrMagnitude);
            float distance=(p-q).sqrMagnitude;if(distance>=best)continue;best=distance;
            pa=side==0?p:q;pb=side==0?q:p;
        }
        pa.y=pb.y=(a.position.y+b.position.y)*0.5f-0.2f;
    }
    public void Contact(NavalRamDamage other)
    {
        if(other==null||other==this)return;
        Vector3 a,b;Closest(transform,other.transform,out a,out b);
        LastContact=other.LastContact=Time.time;Opponent=other;other.Opponent=this;
        ContactLocal=transform.InverseTransformPoint(a);other.ContactLocal=other.transform.InverseTransformPoint(b);
        if(Time.time-Mathf.Max(lastDamage,other.lastDamage)<3f)return;
        Vector3 normal=b-a;normal.y=0f;
        if(normal.sqrMagnitude<0.00001f){normal=other.transform.position-transform.position;normal.y=0f;}
        Vector3 va=Velocity+Vector3.Cross(Vector3.up*AngularRate*Mathf.Deg2Rad,a-transform.position);
        Vector3 vb=other.Velocity+Vector3.Cross(Vector3.up*other.AngularRate*Mathf.Deg2Rad,b-other.transform.position);
        float closing=Mathf.Max(0f,Vector3.Dot(va-vb,normal.normalized));
        if(closing<0.8f)return;
        lastDamage=other.lastDamage=Time.time;
        Damage(a,closing);other.Damage(b,closing);
    }
    private void Damage(Vector3 point,float speed)
    {
        Vector3 local=transform.InverseTransformPoint(point);
        int budget=NavalRamMath.DamageCells(speed,local.z>4.8f,local.z< -4.8f);
        List<NavalHullVoxel> cells=new List<NavalHullVoxel>();
        foreach(NavalHullVoxel cell in Hull.Voxels)
            if(cell.HydraulicShell&&!cell.Missing&&!cell.Detached)cells.Add(cell);
        cells.Sort(delegate(NavalHullVoxel a,NavalHullVoxel b){return (a.LocalCenter-local).sqrMagnitude.CompareTo((b.LocalCenter-local).sqrMagnitude);});
        for(int i=0;i<budget&&i<cells.Count;i++)
        {if((cells[i].LocalCenter-local).sqrMagnitude>0.9f*0.9f)break;Hull.Damage(cells[i].LocalCenter,0.01f);}
    }
}
