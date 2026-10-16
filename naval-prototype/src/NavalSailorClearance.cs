using UnityEngine;
public partial class NavalRepairSailor
{
    private bool ClearBody(Vector3 local)
    {
        CharacterController body=Controller();float height=body!=null?Mathf.Max(1.8f,body.bounds.size.y):2.6f;
        Vector3 bottom=boat.TransformPoint(local)+boat.up*.45f,top=boat.TransformPoint(local)+boat.up*(height-.4f);
        return !Physics.CheckCapsule(bottom,top,.38f,PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore);
    }
}
