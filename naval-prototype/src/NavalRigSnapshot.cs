using System.Collections.Generic;
using UnityEngine;

// Captured once per Resolve, not per sweep sample. Never reused across sequential ship moves.
public sealed class NavalRigSnapshot
{
    public struct CellPose
    {public NavalHullVoxel Cell;public Vector3 Center,HalfSize;public Quaternion Rotation;public bool Attached;public int Group;}
    private sealed class Group {public Bounds Bounds;public bool Included;}
    private readonly Dictionary<Object,int> groupIds=new Dictionary<Object,int>();
    private readonly List<Group> groups=new List<Group>();
    public readonly List<CellPose> Cells=new List<CellPose>();
    private int groupCount;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    public void Capture(NavalRamDamage rig,bool includeDetached)
    {
        Cells.Clear();groupIds.Clear();groupCount=0;
        originalPosition=rig.transform.position;originalRotation=rig.transform.rotation;
        foreach(NavalHullVoxel cell in rig.RigCells)
        {
            if(cell==null||cell.Missing)continue;
            Transform pose=cell.transform;bool attached=pose.parent==rig.transform;
            if(!attached&&!includeDetached)continue;
            Object key=attached&&cell.RigOwner!=null?(Object)cell.RigOwner:pose.parent;
            if(key==null)key=cell;
            Vector3 center=pose.position,half=pose.lossyScale*.5f;
            float padding=NavalRigImpactMath.BroadphasePadding(half.x,half.y,half.z);
            Bounds cellBounds=new Bounds(center,Vector3.one*(padding*2f));int group;
            if(!groupIds.TryGetValue(key,out group))
            {
                group=groupCount++;groupIds.Add(key,group);
                if(group==groups.Count)groups.Add(new Group());groups[group].Bounds=cellBounds;
            }
            else groups[group].Bounds.Encapsulate(cellBounds);
            Cells.Add(new CellPose{Cell=cell,Center=center,HalfSize=half,Rotation=pose.rotation,Attached=attached,Group=group});
        }
    }
    public Quaternion Delta(Quaternion predictedRotation){return predictedRotation*Quaternion.Inverse(originalRotation);}
    public Vector3 Predict(Vector3 center,Vector3 position,Quaternion delta){return position+delta*(center-originalPosition);}
    public bool PrepareGroups(Vector3 ship,Quaternion shipRotation,float scale,Vector3 rigPosition,Quaternion rigRotation,bool moving)
    {
        Vector3 center=ship+shipRotation*new Vector3(0f,-.225f*scale,0f);
        Vector3 half=Extent(shipRotation,new Vector3(6.35f,4.725f,12.65f)*scale);
        Bounds body=new Bounds(center,half*2f);body.Expand(.004f);
        bool any=false;Quaternion delta=Delta(rigRotation);
        for(int i=0;i<groupCount;i++)
        {
            Bounds bounds=groups[i].Bounds;
            if(moving)bounds=new Bounds(Predict(bounds.center,rigPosition,delta),Extent(delta,bounds.extents)*2f);
            groups[i].Included=body.Intersects(bounds);any|=groups[i].Included;
        }
        return any;
    }
    public bool Included(int group){return groups[group].Included;}
    private static Vector3 Extent(Quaternion rotation,Vector3 half)
    {
        Vector3 x=rotation*Vector3.right,y=rotation*Vector3.up,z=rotation*Vector3.forward;
        return new Vector3(Mathf.Abs(x.x)*half.x+Mathf.Abs(y.x)*half.y+Mathf.Abs(z.x)*half.z,
            Mathf.Abs(x.y)*half.x+Mathf.Abs(y.y)*half.y+Mathf.Abs(z.y)*half.z,
            Mathf.Abs(x.z)*half.x+Mathf.Abs(y.z)*half.y+Mathf.Abs(z.z)*half.z);
    }
}
