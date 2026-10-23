using UnityEngine;

public static class NavalPhysicsQueries
{
    private struct SupportMemo {public Vector3 Origin,Direction;public float Distance,Radius;public Transform Ship;public int Frame,Generation;public bool Result;}
    private static readonly SupportMemo[] memo=new SupportMemo[24];private static int cursor,generation;
    public static void Invalidate(){generation++;}
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
