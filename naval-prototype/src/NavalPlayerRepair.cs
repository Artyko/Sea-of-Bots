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
            if(next!=target)
            {
                bool sameGun=next!=null&&target!=null&&next.RepairMechanism!=null&&next.RepairMechanism.IsCannon&&next.RepairMechanism==target.RepairMechanism;
                target=next;if(!sameGun)elapsed=0f;
            }
        }
        if(target==null||!target.Missing&&!target.Detached){elapsed=0f;return;}
        if(target.RepairMechanism!=null&&!target.RepairMechanism.CanRepair){elapsed=0f;return;}
        if(!Input.GetKey(KeyCode.R)){elapsed=0f;return;}
        NavalRepairableMechanism mechanism=target.RepairMechanism;
        if(mechanism!=null&&mechanism.IsCannon)
        {
            if(mechanism.RepairWorker!=null){elapsed=0f;return;}
            mechanism.PlayerRepairUntil=Time.time+.3f;
            if(!mechanism.PartsDelivered)
            {
                if(hull.CannonSupply==null||!hull.CannonSupply.DeliverPlayerKit()){elapsed=0f;return;}
                mechanism.PartsDelivered=true;
            }
        }
        elapsed+=Time.deltaTime;
        if(mechanism!=null&&mechanism.IsCannon)mechanism.ReportRepair(elapsed,"Ремонт");
        float duration=mechanism!=null?mechanism.RepairSeconds:4f;
        if(elapsed<duration)return;
        if(mechanism!=null&&mechanism.IsCannon)
        {mechanism.CompleteCannonRepair(structure,elapsed);target=null;elapsed=0f;scanAt=0f;return;}
        if(mechanism==null&&hull.Cargo!=null&&!hull.Cargo.UsePlayerBoard()){elapsed=0f;return;}
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
            if(cell.RepairMechanism!=null&&cell.RepairMechanism.IsCannon&&cell.RepairMechanism.RepairWorker!=null)continue;
            Vector3 stand=hull.Root.InverseTransformPoint(ray.origin)-Vector3.up*(1.2f/hull.WorldScale);
            Vector3 point=hull.Root.TransformPoint(hull.RepairAccess(cell,stand));
            Vector3 offset=point-ray.origin;float along=Vector3.Dot(offset,ray.direction);
            if(along<0f||offset.sqrMagnitude>36f)continue;
            float distance=(offset-ray.direction*along).sqrMagnitude;
            if(distance>=score)continue;
            RaycastHit hit;
            if(Physics.Raycast(ray.origin,offset.normalized,out hit,Mathf.Max(0f,offset.magnitude-0.3f),
                PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore))
            {
                NavalHullVoxel obstacle=cell.RepairMechanism!=null&&cell.RepairMechanism.IsCannon?NavalVoxelHit.Resolve(hit.collider,hit.point):null;
                if(obstacle==null||obstacle.RepairMechanism!=cell.RepairMechanism)continue;
            }
            best=cell;score=distance;
        }
        return best;
    }
    private void OnGUI()
    {
        if(!Available()||target==null)return;
        if(target.RepairMechanism!=null&&!target.RepairMechanism.CanRepair)
        {GUI.Box(new Rect(NavalHudLayout.ContextX(Screen.width,Screen.height,500f),Screen.height-205f,NavalHudLayout.ContextWidth(Screen.width,Screen.height,500f),28f),target.RepairMechanism.Name+(target.RepairMechanism.RepairUsed?": единственный ремонт уже использован":": восстановление невозможно"));return;}
        if(target.RepairMechanism!=null&&target.RepairMechanism.IsCannon&&!target.RepairMechanism.PartsDelivered&&
            (hull.CannonSupply==null||!hull.CannonSupply.PlayerHasKit))
        {
            string missing=hull.HasCannonStock?"Нужны запчасти: E у ящика в трюме":"Ящик запчастей повреждён — ремонт недоступен";
            GUI.Box(new Rect(NavalHudLayout.ContextX(Screen.width,Screen.height,560),Screen.height-205,NavalHudLayout.ContextWidth(Screen.width,Screen.height,560),28),missing);return;
        }
        float duration=target.RepairMechanism!=null?target.RepairMechanism.RepairSeconds:4f;
        string label=target.RepairMechanism!=null&&target.RepairMechanism.IsCannon?"Пушка: удерживай R — ремонт 60 с (один раз)":"Удерживай R — ремонт 4 с";
        GUI.Box(new Rect(NavalHudLayout.ContextX(Screen.width,Screen.height,500f),Screen.height-205f,NavalHudLayout.ContextWidth(Screen.width,Screen.height,500f),28f),label+" | "+Mathf.RoundToInt(elapsed/duration*100f)+"%");
    }
}
