using UnityEngine;
public partial class NavalStations
{
    private void BuildCapitalDoor(CapitalMount t,int slot,Material armor,Material accent)
    {
        Transform frame=new GameObject("Turret access hatch "+NavalCapitalLayout.Name(slot)).transform;frame.SetParent(t.Root,false);
        frame.localPosition=NavalTurretAccessLayout.Door(slot);frame.localRotation=Quaternion.LookRotation(NavalTurretAccessLayout.Out(slot));
        t.DoorHinge=new GameObject("Armoured hatch hinge").transform;t.DoorHinge.SetParent(frame,false);t.DoorHinge.localPosition=new Vector3(-.18f,0,.035f);
        int first=hull.Voxels.Count;
        ArmoredDecoration(t.DoorHinge,armor,new Vector3(.18f,.24f,0),new Vector3(.34f,.46f,.035f),true);
        ArmoredDecoration(t.DoorHinge,accent,new Vector3(.27f,.24f,.04f),new Vector3(.075f,.025f,.04f),false);
        for(int i=first;i<hull.Voxels.Count;i++)hull.Voxels[i].InternalBarrier=true;
    }
    private void BuildCapitalAccessSteps(int slot,Material armor,Material accent)
    {
        Vector3 mount=NavalCapitalLayout.Mount(slot);mount.y=hull.Definition.WeatherDeck;
        Transform ramp=Root("Turret access stairs "+NavalCapitalLayout.Name(slot),mount);ramp.localRotation=Quaternion.Euler(0,NavalCapitalLayout.BaseYaw(slot),0);
        float rise=NavalCapitalLayout.TurretRise(slot)+NavalTurretAccessLayout.RootLift+NavalTurretAccessLayout.Floor;
        float outer=NavalTurretAccessLayout.GroundDistance(slot),edge=NavalTurretAccessLayout.Side(slot)==0?.36f:.43f;
        if(slot==3){
            float x=NavalTurretAccessLayout.Side(slot)*(edge+.15f);
            for(int side=-1;side<=1;side+=2)
                ArmoredDecoration(ramp,armor,new Vector3(x,rise*.5f,side*.22f),new Vector3(.035f,rise+.15f,.035f),true);
            for(float y=.05f;y<=rise+.05f;y+=.05f)
                ArmoredDecoration(ramp,accent,new Vector3(x,y,0),new Vector3(.04f,.025f,.48f),true);
            return;
        }
        int steps=Mathf.Max(4,Mathf.CeilToInt(rise*hull.WorldScale/.14f));float run=(outer-edge)/steps;
        for(int i=0;i<steps;i++){
            float top=rise*(i+1)/steps;Vector3 p=NavalTurretAccessLayout.Out(slot)*(outer-(i+.5f)*run);p.y=top*.5f;
            Vector3 size=NavalTurretAccessLayout.Side(slot)==0?new Vector3(.40f,top,run+.003f):new Vector3(run+.003f,top,.40f);
            ArmoredDecoration(ramp,i%2==0?armor:accent,p,size,true);
        }
    }
    public Vector3 CapitalSeat(int slot){return boat.InverseTransformPoint(capitalTurrets[slot].Root.TransformPoint(NavalTurretAccessLayout.Seat));}
    public void RequestCapitalAccess(int slot,bool requested){if(capitalTurrets!=null&&slot>=0&&slot<capitalTurrets.Length)capitalTurrets[slot].AccessRequested=requested;}
    public bool CapitalDoorReady(int slot){var t=capitalTurrets[slot];return Mathf.Abs(Mathf.DeltaAngle(t.Yaw,NavalCapitalLayout.BaseYaw(slot)))<.3f&&t.DoorAngle>179f;}
    public void UpdateCapitalAccess(float dt){if(capitalTurrets!=null)TickCapitalDoors(dt);}
    private static Collider[] hatchContacts=new Collider[32];
    private bool CapitalDoorPoseClear(CapitalMount mount,float angle)
    {
        Quaternion rotation=mount.DoorHinge.parent.rotation*Quaternion.Euler(0,-angle,0);
        Vector3 scale=mount.DoorHinge.lossyScale;
        Vector3 centre=mount.DoorHinge.position+rotation*Vector3.Scale(new Vector3(.18f,.24f,0),scale);
        Vector3 half=Vector3.Scale(new Vector3(.34f,.46f,.035f),scale)*.5f+Vector3.one*.025f;
        int count;
        while(true){count=Physics.OverlapBoxNonAlloc(centre,half,hatchContacts,rotation,~0,QueryTriggerInteraction.Ignore);if(count<hatchContacts.Length)break;System.Array.Resize(ref hatchContacts,hatchContacts.Length*2);}
        bool clear=true;for(int i=0;i<count;i++)if(hatchContacts[i] is CharacterController){var actor=hatchContacts[i].GetComponentInParent<Character>();if(actor!=null&&actor.IsAlive()){clear=false;break;}}
        System.Array.Clear(hatchContacts,0,count);return clear;
    }
    private void TickCapitalDoors(float dt)
    {
        bool changed=false;
        for(int slot=0;slot<capitalTurrets.Length;slot++){
            var t=capitalTurrets[slot];float oldYaw=t.Yaw,oldDoor=t.DoorAngle;
            if(Time.time>=t.NextDoorScan){
                t.NextDoorScan=Time.time+.2f;Vector3 approach=NavalTurretAccessLayout.Approach(hull.Definition,slot),door=NavalTurretAccessLayout.Door(slot),normal=NavalTurretAccessLayout.Out(slot);
                foreach(Character actor in NavalCharacters.Snapshot())if(actor!=null&&actor.IsAlive()){
                    if(actor==player&&slot==playerCapitalSlot)continue;
                    Vector3 ship=boat.InverseTransformPoint(actor.transform.position),local=t.Root.InverseTransformPoint(actor.transform.position),d=local-door;
                    float depth=Vector3.Dot(d,normal),lateral=Vector3.Cross(normal,d).y;
                    bool ground=actor==player&&(boat.TransformVector(ship-approach)).sqrMagnitude<3f*3f;
                    bool passage=depth>-.17f&&depth<.30f&&Mathf.Abs(lateral)<.32f&&local.y>-.10f&&local.y<.9f;
                    bool visiting=actor==player&&Mathf.Abs(local.x)<.36f&&Mathf.Abs(local.z)<.33f&&local.y>-.10f&&local.y<.94f;
                    if(t.Ready&&actor!=player){var sailor=actor.GetComponent<NavalRepairSailor>();if(sailor!=null&&sailor.CapitalSeated)continue;}
                    if((ground&&!t.Ready&&slot!=playerCapitalSlot)||passage||visiting){t.AutoDoorUntil=Time.time+.5f;break;}
                }
            }
            bool wanted=hull.Sunk||t.AccessRequested||Time.time<t.AutoDoorUntil;
            bool home=Mathf.Abs(Mathf.DeltaAngle(t.Yaw,NavalCapitalLayout.BaseYaw(slot)))<.3f;
            if(wanted&&!home){
                t.DoorAngle=Mathf.MoveTowards(t.DoorAngle,0,240f*dt);

            }else t.DoorAngle=Mathf.MoveTowards(t.DoorAngle,wanted?180f:0f,240f*dt);
            if(t.DoorAngle!=oldDoor&&!CapitalDoorPoseClear(t,t.DoorAngle))t.DoorAngle=oldDoor;
            if(wanted&&!home&&t.DoorAngle<=.1f&&oldDoor<=.1f){

                // Retract elevation before returning to the access bearing.
                float nextPitch=Mathf.MoveTowards(t.Pitch,NavalTurretAccessLayout.MaximumPitch,6f*dt);
                if(CapitalBarrelsClear(t,t.Yaw,nextPitch)){t.Pitch=nextPitch;t.Barrels.localRotation=Quaternion.Euler(-t.Pitch,0,0);}
                TraverseCapital(t,NavalCapitalLayout.BaseYaw(slot),t.Pitch,24f,dt);
            }
            t.DoorHinge.localRotation=Quaternion.Euler(0,-t.DoorAngle,0);
            changed|=oldYaw!=t.Yaw||oldDoor!=t.DoorAngle;
        }
        if(changed)NavalPhysicsQueries.Invalidate();
    }
}
