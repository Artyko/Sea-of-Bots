using UnityEngine;

public partial class NavalTestWorld
{
    public bool Spectating {get;private set;}
    private Camera liveCamera,spectatorCamera;
    private AudioListener liveListener;
    private Vector3 lastViewPosition;
    private Quaternion lastViewRotation=Quaternion.identity;
    private CursorLockMode beforeSpectatorLock;
    private bool beforeSpectatorVisible,liveListenerWasEnabled;
    private float spectatorYaw,spectatorPitch;
    public Camera ViewCamera {get{return Spectating?spectatorCamera:liveCamera;}}
    public bool OwnsPlayer(Character character)
    {return GameModeManager.IsSinglePlayer()&&!object.ReferenceEquals(character,null)&&object.ReferenceEquals(character,player);}
    private void RememberLiveCamera()
    {
        if(player!=null&&liveCamera==null)liveCamera=player.GetPlayerCamera();
        if(liveCamera==null)return;
        lastViewPosition=liveCamera.transform.position;lastViewRotation=liveCamera.transform.rotation;
    }
    public void BeginSpectating()
    {
        if(Spectating)return;
        RememberLiveCamera();CloseShipMenu();SetStationLock(false);SetSwimming(false);
        if(stations!=null)stations.Exit();
        if(hull!=null&&hull.Bilge!=null)hull.Bilge.ReleasePlayer();
        if(enemy!=null&&enemy.Hull.Bilge!=null)enemy.Hull.Bilge.ReleasePlayer();
        beforeSpectatorLock=Cursor.lockState;beforeSpectatorVisible=Cursor.visible;
        GameObject view=new GameObject("Naval free flight spectator camera");view.transform.SetParent(transform,false);
        spectatorCamera=view.AddComponent<Camera>();
        if(liveCamera!=null)
        {
            spectatorCamera.CopyFrom(liveCamera);liveCamera.enabled=false;
            liveListener=liveCamera.GetComponentInParent<AudioListener>();
            if(liveListener!=null){liveListenerWasEnabled=liveListener.enabled;liveListener.enabled=false;}
        }
        spectatorCamera.targetTexture=null;spectatorCamera.farClipPlane=12000f;
        spectatorCamera.tag="MainCamera";view.AddComponent<AudioListener>();
        view.transform.position=lastViewPosition;view.transform.rotation=lastViewRotation;
        spectatorYaw=view.transform.eulerAngles.y;spectatorPitch=Mathf.DeltaAngle(0f,view.transform.eulerAngles.x);
        Spectating=true;
        if(player!=null){PlayerInputController input=player.GetComponent<PlayerInputController>();if(input!=null)input.enabled=false;}
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        if(seaMap!=null)seaMap.SetFocus(view.transform);
        Debug.Log("[NavalPrototype] Player died: naval battle continues in free flight spectator mode");
    }
    private void UpdateSpectator()
    {
        if(spectatorCamera==null)return;
        if(liveCamera!=null)liveCamera.enabled=false;
        if(ShipMenuOpen)return;
        bool looking=Input.GetMouseButton(1);
        if(looking){Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
        else {Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
        Transform view=spectatorCamera.transform;
        if(looking)
        {
            spectatorYaw+=Input.GetAxisRaw("Mouse X")*2.5f;
            spectatorPitch=Mathf.Clamp(spectatorPitch-Input.GetAxisRaw("Mouse Y")*2.5f,-85f,85f);
            view.rotation=Quaternion.Euler(spectatorPitch,spectatorYaw,0f);
        }
        Vector3 direction=view.forward*((Input.GetKey(KeyCode.W)?1f:0f)-(Input.GetKey(KeyCode.S)?1f:0f))+
            view.right*((Input.GetKey(KeyCode.D)?1f:0f)-(Input.GetKey(KeyCode.A)?1f:0f))+
            Vector3.up*((Input.GetKey(KeyCode.Space)||Input.GetKey(KeyCode.E)?1f:0f)-(Input.GetKey(KeyCode.LeftControl)||Input.GetKey(KeyCode.Q)?1f:0f));
        float speed=Input.GetKey(KeyCode.LeftShift)?48f:16f;
        view.position+=Vector3.ClampMagnitude(direction,1f)*speed*Mathf.Min(Time.unscaledDeltaTime,.05f);
        if(Input.GetKeyDown(KeyCode.F8)&&boat!=null)view.position=boat.position-boat.forward*15f+Vector3.up*12f;
    }
    private void EndSpectating()
    {
        if(!Spectating)return;
        if(spectatorCamera!=null)Destroy(spectatorCamera.gameObject);
        if(liveListener!=null)liveListener.enabled=liveListenerWasEnabled;
        Cursor.lockState=beforeSpectatorLock;Cursor.visible=beforeSpectatorVisible;Spectating=false;
    }
}
