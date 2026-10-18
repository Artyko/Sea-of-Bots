using UnityEngine;

[DefaultExecutionOrder(50)]
public class NavalPlayerRepair : MonoBehaviour
{
    private FirstPersonMover player;
    private NavalHull hull;
    private NavalStructure structure;
    private NavalStations stations;
    private NavalHullVoxel target;
    private float elapsed,scanAt;
    public void Initialize(FirstPersonMover owner,NavalHull ship,NavalStructure frame,NavalStations controls)
    {player=owner;hull=ship;structure=frame;stations=controls;}
    private bool Available()
    {return player!=null&&player.IsAlive()&&player.IsPlayerInputEnabled()&&Time.timeScale>0f&&!stations.Operating&&(NavalTestWorld.Active==null||!NavalTestWorld.Active.ShipMenuOpen);}
    private void LateUpdate()
    {
        if(!Available()||Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift))
        {target=null;elapsed=0f;return;}
        if(Time.time>=scanAt)
        {
            scanAt=Time.time+0.1f;NavalHullVoxel next=FindTarget();
            if(next!=target){target=next;elapsed=0f;}
        }
        if(target==null||!target.Missing&&!target.Detached){elapsed=0f;return;}
        if(target.RepairMechanism!=null&&!target.RepairMechanism.CanRepair){elapsed=0f;return;}
        if(!Input.GetKey(KeyCode.R)){elapsed=0f;return;}
        elapsed+=Time.deltaTime;
        if(elapsed<4f)return;
        Vector3 patch=target.LocalCenter;
        foreach(NavalHullVoxel cell in hull.Voxels)
            if((cell.HydraulicShell||cell.InternalBarrier||cell.RepairableEquipment)&&(cell.Missing||cell.Detached)&&(cell.LocalCenter-patch).sqrMagnitude<=0.85f*0.85f)
                structure.RestoreCell(cell);
        target=null;elapsed=0f;scanAt=0f;
    }
    private NavalHullVoxel FindTarget()
    {
        Camera camera=Camera.main;if(camera==null)return null;
        Ray ray=new Ray(camera.transform.position,camera.transform.forward);
        NavalHullVoxel best=null;float score=0.65f*0.65f;
        foreach(NavalHullVoxel cell in hull.Voxels)
        {
            if(!(cell.HydraulicShell||cell.InternalBarrier||cell.RepairableEquipment)||!cell.Missing&&!cell.Detached)continue;
            Vector3 stand=hull.Root.InverseTransformPoint(ray.origin)-Vector3.up*(1.2f/hull.WorldScale);
            Vector3 point=hull.Root.TransformPoint(hull.RepairAccess(cell,stand));
            Vector3 offset=point-ray.origin;float along=Vector3.Dot(offset,ray.direction);
            if(along<0f||offset.sqrMagnitude>36f)continue;
            float distance=(offset-ray.direction*along).sqrMagnitude;
            if(distance>=score)continue;
            RaycastHit hit;
            if(Physics.Raycast(ray.origin,offset.normalized,out hit,Mathf.Max(0f,offset.magnitude-0.3f),
                PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore))continue;
            best=cell;score=distance;
        }
        return best;
    }
    private void OnGUI()
    {
        if(!Available()||target==null)return;
        if(target.RepairMechanism!=null&&!target.RepairMechanism.CanRepair)
        {GUI.Box(new Rect(NavalHudLayout.ContextX(Screen.width,Screen.height,500f),Screen.height-205f,NavalHudLayout.ContextWidth(Screen.width,Screen.height,500f),28f),target.RepairMechanism.Name+": восстановление невозможно");return;}
        GUI.Box(new Rect(NavalHudLayout.ContextX(Screen.width,Screen.height,500f),Screen.height-205f,NavalHudLayout.ContextWidth(Screen.width,Screen.height,500f),28f),"Hold R: repair aimed hole (4 sec) | "+Mathf.RoundToInt(elapsed/4f*100f)+"%");
    }
}
