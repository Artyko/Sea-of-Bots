using UnityEngine;

public static class NavalPhysicsQueries
{
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
        using(NavalPerformance.Measure(NavalPerfArea.SupportQuery))
        {
            try
            {
                int count=support.Fill(new SupportCast{Origin=origin,Direction=direction,Radius=radius,Distance=distance,Mask=PhysicsManager.GetEnvironmentLayerMask()});
                for(int i=0;i<count;i++)
                {
                    RaycastHit hit=support.Items[i];
                    if(hit.collider!=null&&hit.collider.transform.IsChildOf(ship)&&hit.normal.y>.35f)return true;
                }
                return false;
            }
            finally{support.Clear();}
        }
    }
}
