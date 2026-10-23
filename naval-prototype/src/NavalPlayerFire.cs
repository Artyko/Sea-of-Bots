using UnityEngine;
[DefaultExecutionOrder(60)]
public sealed class NavalPlayerFire : MonoBehaviour
{
    private FirstPersonMover player;
    private NavalHull own;
    private NavalStations stations;
    private NavalBucketVisual bucket;
    private NavalFireSystem carry,pourSystem;
    private NavalFireTarget target,pourTarget;
    private float amount,fill,pouring,scanAt;
    private readonly NavalBucketLoad load=new NavalBucketLoad();
    private Vector3 pourPoint;
    public void Initialize(FirstPersonMover owner,NavalHull hull,NavalStations controls)
    {player=owner;own=hull;stations=controls;bucket=gameObject.AddComponent<NavalBucketVisual>();bucket.Initialize();}
    private bool Available()
    {var world=NavalTestWorld.Active;return world!=null&&world.SimulationActive&&player!=null&&player.IsAlive()&&player.IsPlayerInputEnabled()&&!stations.Operating&&!world.ShipMenuOpen;}
    private NavalFireSystem OnShip()
    {
        foreach(var ladder in NavalSeaLadder.All)if(ladder!=null&&ladder.Hull.Fire!=null)
        {Vector3 p=ladder.transform.InverseTransformPoint(player.transform.position);if(Mathf.Abs(p.x)<ladder.Hull.Definition.HullHalfWidth(p.z)+.1f&&Mathf.Abs(p.z)<6&&p.y>ladder.Hull.Floor-.3f&&p.y<ladder.Hull.Definition.RimY+1f)return ladder.Hull.Fire;}
        return null;
    }
    private bool AtSource()
    {
        var world=NavalTestWorld.Active;if(world.WaterLevelAt(player.transform.position)>player.transform.position.y-1.2f)return true;
        foreach(var ladder in NavalSeaLadder.All)if(ladder!=null)
        {
            Vector3 p=ladder.transform.InverseTransformPoint(player.transform.position);
            if(Mathf.Abs(p.x)>1.7f&&Mathf.Abs(p.x)<3.2f&&Mathf.Abs(p.z)<5.8f&&p.y>ladder.Hull.Definition.WeatherDeck-.2f&&p.y<ladder.Hull.Definition.WeatherDeck+1.5f)return true;
        }
        return false;
    }
    private float TakeWater()
    {
        NavalFireSystem ship=OnShip();
        if(ship!=null)
        {
            Vector3 local=ship.transform.InverseTransformPoint(player.transform.position);
            if(local.y<ship.Ship.Definition.WeatherDeck)
            {load.Scoop(ship.Ship.Flood,local.x,local.z,NavalBucketLoad.DefaultCapacity,1.5f/ship.Ship.WorldScale);return load.Dump();}
        }
        return NavalBucketLoad.DefaultCapacity;
    }
    private void FindTarget()
    {
        target=null;pourSystem=null;Camera camera=player.GetPlayerCamera();if(camera==null)return;
        float best=.8f;
        foreach(var ladder in NavalSeaLadder.All)if(ladder!=null&&ladder.Hull.Fire!=null)
        {
            var system=ladder.Hull.Fire;
            for(int i=0;i<system.TargetCount;i++)
            {
                var fire=system.TargetAt(i);if(!fire.Active)continue;
                Vector3 point=ladder.transform.TransformPoint(fire.Point),delta=point-camera.transform.position;
                float along=Vector3.Dot(delta,camera.transform.forward);if(along<0||delta.sqrMagnitude>64)continue;
                float off=(delta-camera.transform.forward*along).magnitude;if(off>best)continue;
                RaycastHit hit;
                if(Physics.Raycast(camera.transform.position,delta.normalized,out hit,Mathf.Max(0,delta.magnitude-.8f),PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore))continue;
                best=off;target=fire;pourSystem=system;
            }
        }
    }
    private void Update()
    {
        if(!Available())
        {
            if(player!=null&&!player.IsAlive()&&amount>0&&carry!=null){carry.Douse(player.transform.position,amount);amount=0;}
            fill=0;pouring=0;bucket.Hide();UpdateMass();return;
        }
        if(Time.time>=scanAt){scanAt=Time.time+.1f;if(pouring<=0)FindTarget();}
        if(Input.GetKeyDown(KeyCode.Y))
        {
            Camera camera=player.GetPlayerCamera();
            if(camera!=null)
            {
                RaycastHit[] hits=Physics.RaycastAll(camera.transform.position,camera.transform.forward,8f,~0,QueryTriggerInteraction.Collide);
                System.Array.Sort(hits,delegate(RaycastHit a,RaycastHit b){return a.distance.CompareTo(b.distance);});
                foreach(var hit in hits)
                {
                    if(hit.collider.GetComponentInParent<Character>()==player)continue;
                    var cell=NavalVoxelHit.Resolve(hit.collider,hit.point);
                    if(cell!=null&&cell.Owner.Fire!=null){cell.Owner.Fire.Spark(cell);break;}
                    if(!hit.collider.isTrigger)break;
                }
            }
        }
        if(amount<=0&&Input.GetKey(KeyCode.B)&&AtSource())
        {fill+=Time.deltaTime;if(fill>=1.2f){amount=TakeWater();fill=0;}}
        else fill=0;
        if(Input.GetKeyDown(KeyCode.V)&&amount>0&&pouring<=0)
        {
            pourTarget=target;pouring=.01f;
            if(pourTarget==null)
            {
                pourSystem=OnShip();if(pourSystem==null)pourSystem=own.Fire;
                Camera camera=player.GetPlayerCamera();RaycastHit hit;
                pourPoint=camera!=null?camera.transform.position+camera.transform.forward*4:player.transform.position;
                if(camera!=null&&Physics.Raycast(camera.transform.position,camera.transform.forward,out hit,8,PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore))pourPoint=hit.point;
            }
        }
        if(pouring>0)
        {
            pouring+=Time.deltaTime;
            if(pouring>=.8f)
            {
                if(pourSystem!=null)
                {Vector3 point=pourTarget!=null?pourSystem.transform.TransformPoint(pourTarget.Point):pourPoint;if((point-player.transform.position).sqrMagnitude<81)pourSystem.Douse(point,amount);}
                amount=0;pouring=0;pourTarget=null;
            }
        }
        if(amount>0||fill>0||pouring>0)
        {
            Transform reference=carry!=null?carry.transform:own.Root;
            Vector3 point=player.transform.position+player.transform.forward*.6f-Vector3.up*.2f;
            bucket.Pose(reference,reference.InverseTransformPoint(point),pouring>0?70:0,amount>0,pouring>.3f);
        }
        else bucket.Hide();
        UpdateMass();
    }
    private void UpdateMass()
    {
        if(carry!=null)carry.PlayerWater=0;
        carry=player!=null&&player.IsAlive()?OnShip():null;
        if(carry!=null){carry.PlayerWater=amount;carry.PlayerWaterPoint=carry.transform.InverseTransformPoint(player.transform.position);}
    }
    private void OnGUI()
    {
        if(!Available()||amount<=0&&fill<=0&&target==null)return;
        string text=fill>0?"B: filling bucket "+Mathf.RoundToInt(fill/1.2f*100)+"%":amount>0?"Bucket "+amount.ToString("F1")+" m3 | Aim at fire / fuse, V: pour":"Hold B near a rail or water: fill bucket";
        GUI.Box(new Rect(Screen.width*.5f-250,Screen.height-245,500,28),text);
    }
    private void OnDestroy(){if(carry!=null)carry.PlayerWater=0;}
}
