using UnityEngine;
public partial class NavalRepairSailor
{
    private float nativeStepOffset=-1;
    private Matrix4x4 lastDeckToLocal;
    private bool deckCarryReady;
    private void InitializeDeckCarry(){lastDeckToLocal=boat.worldToLocalMatrix;deckCarryReady=true;}
    public void CarrySupportedDeck(Matrix4x4 current)
    {
        if(deckCarryReady&&!stranded&&!Rescuing&&!Defending&&!Assaulting&&robot!=null&&robot.IsAlive())
        {
            bool correctedFootOffset=false;
            var body=Controller();
            Bounds bounds=body!=null&&body.enabled?body.bounds:default(Bounds);
            if(bounds.size.y>.5f){
                float actual=robot.transform.position.y-bounds.min.y;
                if(actual>=0&&actual<4f&&Mathf.Abs(actual-footOffset)>.05f){
                    if(Mathf.Abs(actual-footOffset)>.15f)Debug.Log("[NavalPrototype] Crew foot offset corrected: sailor="+robot.GetInstanceID()+" old="+footOffset.ToString("F2")+" actual="+actual.ToString("F2"));
                    footOffset=actual;
                    correctedFootOffset=true;
                }
            }
            Vector3 previousLocal=lastDeckToLocal.MultiplyPoint3x4(robot.transform.position-Vector3.up*footOffset);
            if(correctedFootOffset)feet=previousLocal;
            // A CharacterController can be held for one frame by a rail while
            // the ship advances beneath it. Keep carrying a planned inboard
            // footing through that small contact error; genuine knockback and
            // overboard falls are still outside the grace envelope.
            float radius=body!=null?body.radius*Mathf.Abs(body.transform.lossyScale.x):.38f;
            bool near=NavalCrewDeckSafety.Near(previousLocal.x,previousLocal.z,hull.Definition.HullHalfWidth(previousLocal.z),hull.WorldScale,radius);
            bool heightClose=Mathf.Abs(previousLocal.y-feet.y)*hull.WorldScale<.65f;
            if((InsideWalkingDeck(previousLocal)||InsideWalkingDeck(feet)&&near&&heightClose)&&boat.TransformVector(previousLocal-feet).sqrMagnitude<1.44f)
            {feet=previousLocal;HoldPosition();}
        }
        lastDeckToLocal=current;deckCarryReady=true;
    }
    private bool InsideWalkingDeck(Vector3 point)
    {
        var body=Controller();float radius=body!=null?body.radius*Mathf.Abs(body.transform.lossyScale.x):.38f;
        return NavalCrewDeckSafety.Inside(point.x,point.z,hull.Definition,radius);
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
