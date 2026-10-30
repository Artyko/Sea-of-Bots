using UnityEngine;

public static class NavalPhysicsQueries
{
    private struct SupportMemo {public Vector3 Origin,Direction;public float Distance,Radius;public Transform Ship;public int Frame,Generation;public bool Result;}
    private static readonly SupportMemo[] memo=new SupportMemo[24];private static int cursor,generation;
    public static void Invalidate(){generation++;}
    private struct LocalMemo {public NavalHull Hull;public int X,Y,Z,Revision;}
    private struct MovingMemo {public NavalHull Hull;public int X,Y,Z,DX,DY,DZ,Radius,Distance,Revision;}
    private struct RegisteredHull {public Transform Root;public NavalHull Hull;}
    private static readonly LocalMemo[] localMemo=new LocalMemo[512];
    private static readonly MovingMemo[] movingMemo=new MovingMemo[256];
    private static readonly RegisteredHull[] hulls=new RegisteredHull[8];
    private static int localHits,localLive,riderHits,riderLive;
    private static int Q(float value,float scale){return Mathf.RoundToInt(value*scale);}
    private static int LocalSlot(NavalHull hull,int x,int y,int z)
    {unchecked{return (hull.GetHashCode()*486187739^x*73856093^y*19349663^z*83492791)&(localMemo.Length-1);}}
    private static int MovingSlot(NavalHull hull,int x,int y,int z,int dx,int dy,int dz,int radius,int distance)
    {unchecked{return (hull.GetHashCode()*486187739^x*73856093^y*19349663^z*83492791^dx*31^dy*131^dz*521^radius*2053^distance*8191)&(movingMemo.Length-1);}}
    public static void Register(NavalHull hull)
    {
        if(hull==null||hull.Root==null)return;
        for(int i=0;i<hulls.Length;i++)if(hulls[i].Root==hull.Root||hulls[i].Root==null){hulls[i]=new RegisteredHull{Root=hull.Root,Hull=hull};return;}
    }
    public static void Unregister(NavalHull hull)
    {if(hull!=null)for(int i=0;i<hulls.Length;i++)if(hulls[i].Hull==hull)hulls[i]=new RegisteredHull();}
    public static string ConsumeDiagnostics()
    {
        string value="crew_static_hit/live="+localHits+"/"+localLive+" rider_static_hit/live="+riderHits+"/"+riderLive;
        localHits=localLive=riderHits=riderLive=0;return value;
    }
    private static NavalHull Registered(Transform root)
    {for(int i=0;i<hulls.Length;i++)if(hulls[i].Root==root)return hulls[i].Hull;return null;}
    private static bool StaticDeckHit(RaycastHit hit,NavalHull hull)
    {
        NavalHullVoxel cell=hit.collider!=null?NavalVoxelHit.Resolve(hit.collider,hit.point):null;
        return cell!=null&&cell.Owner==hull&&!cell.MovableEquipment&&!cell.ManagedRig&&!cell.Detached&&!cell.Missing;
    }
    public static bool HasLocalSupport(NavalHull hull,Vector3 point)
    {
        if(hull==null||hull.Root==null)return false;
        int x=Q(point.x,50f),y=Q(point.y,50f),z=Q(point.z,50f);
        int slot=LocalSlot(hull,x,y,z);LocalMemo cached=localMemo[slot];
        if(cached.Hull==hull&&cached.Revision==hull.Revision&&cached.X==x&&cached.Y==y&&cached.Z==z){localHits++;return true;}
        localLive++;
        Transform ship=hull.Root;RaycastHit ray;Vector3 world=ship.TransformPoint(point);
        bool found=Physics.Raycast(world+ship.up*.25f,-ship.up,out ray,.55f,PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore)&&
            ray.collider.transform.IsChildOf(ship)&&Mathf.Abs(ship.InverseTransformPoint(ray.point).y-point.y)*hull.WorldScale<.3f;
        // Cache only stable positive deck hits. Misses and moving equipment
        // remain live queries; hull damage invalidates via Revision.
        if(found&&StaticDeckHit(ray,hull))
        localMemo[slot]=new LocalMemo{Hull=hull,X=x,Y=y,Z=z,Revision=hull.Revision};
        return found;
    }
    private static readonly NavalQueryBuffer<RaycastHit> support=new NavalQueryBuffer<RaycastHit>(64);
    private struct SupportCast:INavalBufferQuery<RaycastHit>
    {
        public Vector3 Origin,Direction;
        public float Radius,Distance;
        public int Mask;
        public int Execute(RaycastHit[] buffer)
        {
            return Radius>0f?Physics.SphereCastNonAlloc(Origin,Radius,Direction,buffer,Distance,Mask,QueryTriggerInteraction.Ignore):
                Physics.RaycastNonAlloc(Origin,Direction,buffer,Distance,Mask,QueryTriggerInteraction.Ignore);
        }
    }
    public static bool HasSupport(Vector3 origin,Vector3 direction,float distance,Transform ship,float radius)
    {
        if(ship==null)return false;
        NavalHull registered=Registered(ship);bool stable=registered!=null&&Vector3.Dot(ship.up,Vector3.up)>.7f;
        Vector3 localOrigin=Vector3.zero,localDirection=Vector3.zero;int lx=0,ly=0,lz=0,ldx=0,ldy=0,ldz=0,qr=0,qd=0;
        if(stable)
        {
            localOrigin=ship.InverseTransformPoint(origin);localDirection=ship.InverseTransformDirection(direction);
            lx=Q(localOrigin.x,40f);ly=Q(localOrigin.y,40f);lz=Q(localOrigin.z,40f);
            ldx=Q(localDirection.x,40f);ldy=Q(localDirection.y,40f);ldz=Q(localDirection.z,40f);qr=Q(radius,100f);qd=Q(distance,100f);
            int slot=MovingSlot(registered,lx,ly,lz,ldx,ldy,ldz,qr,qd);MovingMemo hit=movingMemo[slot];
            if(hit.Hull==registered&&hit.Revision==registered.Revision&&hit.X==lx&&hit.Y==ly&&hit.Z==lz&&hit.DX==ldx&&hit.DY==ldy&&hit.DZ==ldz&&hit.Radius==qr&&hit.Distance==qd){riderHits++;return true;}
        }
        riderLive++;
        int frame=Time.frameCount;
        for(int i=0;i<memo.Length;i++){var hit=memo[i];if(hit.Ship==ship&&hit.Frame==frame&&hit.Generation==generation&&hit.Origin.Equals(origin)&&hit.Direction.Equals(direction)&&hit.Distance==distance&&hit.Radius==radius)return hit.Result;}
        bool found=false;
        using(NavalPerformance.Measure(NavalPerfArea.SupportQuery))
        {
            try
            {
                // Most riders stand on the nearest hit. Avoid collecting every
                // intersected deck/cargo collider when that hit already proves support.
                RaycastHit nearest;int mask=PhysicsManager.GetEnvironmentLayerMask();
                bool hasNearest=radius>0?Physics.SphereCast(origin,radius,direction,out nearest,distance,mask,QueryTriggerInteraction.Ignore):Physics.Raycast(origin,direction,out nearest,distance,mask,QueryTriggerInteraction.Ignore);
                // A nearest-hit query with no hit already proves the complete cast empty.
                if(!hasNearest){
                    memo[cursor]=new SupportMemo{Origin=origin,Direction=direction,Distance=distance,Radius=radius,Ship=ship,Frame=frame,Generation=generation,Result=false};cursor=(cursor+1)%memo.Length;return false;
                }
                if(hasNearest&&nearest.collider!=null&&nearest.collider.transform.IsChildOf(ship)&&nearest.normal.y>.35f){
                    if(stable&&StaticDeckHit(nearest,registered))
                    {int slot=MovingSlot(registered,lx,ly,lz,ldx,ldy,ldz,qr,qd);movingMemo[slot]=new MovingMemo{Hull=registered,X=lx,Y=ly,Z=lz,DX=ldx,DY=ldy,DZ=ldz,Radius=qr,Distance=qd,Revision=registered.Revision};}
                    memo[cursor]=new SupportMemo{Origin=origin,Direction=direction,Distance=distance,Radius=radius,Ship=ship,Frame=frame,Generation=generation,Result=true};cursor=(cursor+1)%memo.Length;return true;
                }
                // Foreign ship, side face or ambiguous hit: retain the full original query.
                int count=support.Fill(new SupportCast{Origin=origin,Direction=direction,Radius=radius,Distance=distance,Mask=PhysicsManager.GetEnvironmentLayerMask()});
                for(int i=0;i<count;i++)
                {
                    RaycastHit hit=support.Items[i];
                    if(hit.collider!=null&&hit.collider.transform.IsChildOf(ship)&&hit.normal.y>.35f){found=true;break;}
                }
                memo[cursor]=new SupportMemo{Origin=origin,Direction=direction,Distance=distance,Radius=radius,Ship=ship,Frame=frame,Generation=generation,Result=found};cursor=(cursor+1)%memo.Length;
                return found;
            }
            finally{support.Clear();}
        }
    }
}
