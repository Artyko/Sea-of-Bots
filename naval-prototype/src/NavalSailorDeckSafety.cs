using UnityEngine;
public partial class NavalRepairSailor
{
    private float nativeStepOffset=-1;
    private Matrix4x4 lastDeckPose;
    private bool deckCarryReady;
    private void InitializeDeckCarry(){lastDeckPose=boat.localToWorldMatrix;deckCarryReady=true;}
    public void CarrySupportedDeck()
    {
        Matrix4x4 current=boat.localToWorldMatrix;
        if(deckCarryReady&&!stranded&&!Rescuing&&!Defending&&!Assaulting&&robot!=null&&robot.IsAlive())
        {
            var body=Controller();
            if(body!=null&&body.enabled&&body.bounds.size.y>.5f){
                float actual=robot.transform.position.y-body.bounds.min.y;
                if(actual>=0&&actual<4f&&Mathf.Abs(actual-footOffset)>.05f){
                    if(Mathf.Abs(actual-footOffset)>.15f)Debug.Log("[NavalPrototype] Crew foot offset corrected: sailor="+robot.GetInstanceID()+" old="+footOffset.ToString("F2")+" actual="+actual.ToString("F2"));
                    footOffset=actual;feet=lastDeckPose.inverse.MultiplyPoint3x4(robot.transform.position-Vector3.up*footOffset);
                }
            }
            Vector3 previousLocal=lastDeckPose.inverse.MultiplyPoint3x4(robot.transform.position-Vector3.up*footOffset);
            // Preserve real knockback/falls: carry only a nearby sailor still inside the previous hull footprint.
            if(InsideWalkingDeck(previousLocal)&&boat.TransformVector(previousLocal-feet).sqrMagnitude<.64f)
            {feet=previousLocal;HoldPosition();}
        }
        lastDeckPose=current;deckCarryReady=true;
    }
    private bool InsideWalkingDeck(Vector3 point)
    {
        var body=Controller();float radius=body!=null?body.radius*Mathf.Abs(body.transform.lossyScale.x):.38f;
        return NavalCrewDeckSafety.Inside(point.x,point.z,hull.Definition.HullHalfWidth(point.z),hull.WorldScale,radius);
    }
    private void DeckStepHeight(CharacterController controller)
    {
        if(controller==null)return;
        if(nativeStepOffset<0)nativeStepOffset=controller.stepOffset;
        float wanted=NavalCrewDeckSafety.StepHeight(nativeStepOffset);
        if(controller.stepOffset!=wanted)controller.stepOffset=wanted;
    }
    private void RestoreStepHeight()
    {var body=Controller();if(body!=null&&nativeStepOffset>=0)body.stepOffset=nativeStepOffset;}
}
