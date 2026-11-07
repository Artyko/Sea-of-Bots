using UnityEngine;

public partial class NavalStations
{
    private int playerCapitalSlot = -1;
    private int nearbyCapitalSlot = -1;

    private Vector3 CapitalPlayerPosition(int slot)
    {
        return boat.TransformPoint(NavalCapitalLayout.ServicePoint(hull.Definition, slot));
    }

    private bool HandleCapitalInput(float dt, float feetOffset, bool unavailable)
    {
        nearbyCapitalSlot = -1;
        if (capitalTurrets == null || EnemyControlled) return false;
        if (unavailable || player == null || !player.IsAlive())
        {
            playerCapitalSlot = -1;
            return false;
        }

        if (playerCapitalSlot < 0)
        {
            if (active >= 0 || hull.Bilge != null && hull.Bilge.PlayerInteracting) return false;
            float closest = 2f;
            Vector3 feet = player.transform.position - Vector3.up * feetOffset;
            for (int slot = 0; slot < capitalTurrets.Length; slot++)
            {
                float distance = Vector3.Distance(feet, CapitalPlayerPosition(slot));
                if (distance >= closest || !MainBatterySlotFunctional(slot)) continue;
                nearbyCapitalSlot = slot;
                closest = distance;
            }
            if (nearbyCapitalSlot < 0 || !Input.GetKeyDown(KeyCode.E)) return false;
            playerCapitalSlot = nearbyCapitalSlot;
            capitalTurrets[playerCapitalSlot].SecondShot = 0;
            if(GetComponent<NavalCapitalSight>()==null)gameObject.AddComponent<NavalCapitalSight>().Stations=this;
            capitalTurrets[playerCapitalSlot].AutoDoorUntil=0f;
            capitalTurrets[playerCapitalSlot].AccessRequested=false;
            return true;
        }
        int selected = playerCapitalSlot;
        if (Input.GetKeyDown(KeyCode.E) || !MainBatterySlotFunctional(selected))
        {
            playerCapitalSlot = -1;
            return true;
        }
        var mount = capitalTurrets[selected];
        mount.Reload = Mathf.Max(0, mount.Reload - dt * (SteamPlant != null ? SteamPlant.AuxiliaryFactor : 1f));
        if (mount.AccessRequested || mount.DoorAngle > .1f) return true;
        float turn = (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0);
        float elevate = (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0);
        float mouseYaw=Mathf.Clamp(Input.GetAxis("Mouse X")*1.5f,-8f*dt,8f*dt);
        float mousePitch=Mathf.Clamp(Input.GetAxis("Mouse Y")*1.2f,-6f*dt,6f*dt);
        float yaw = mount.Yaw + turn * 8f * Mathf.Min(dt,.1f)+mouseYaw;
        float pitch = Mathf.Clamp(mount.Pitch + elevate * 6f * dt+mousePitch, NavalCapitalLayout.MinimumPitch(selected), NavalTurretAccessLayout.MaximumPitch);
        if(!CapitalBarrelsClear(mount,yaw,pitch))
        {
            if(CapitalBarrelsClear(mount,mount.Yaw,pitch))yaw=mount.Yaw;
            else if(CapitalBarrelsClear(mount,yaw,mount.Pitch))pitch=mount.Pitch;
        }
        if ((turn != 0 || elevate != 0 || mouseYaw!=0 || mousePitch!=0) && CapitalBarrelsClear(mount,yaw,pitch))
        {
            mount.Yaw = Mathf.Repeat(yaw,360f);
            mount.Pitch = pitch;
            mount.Root.localRotation = Quaternion.Euler(0,mount.Yaw,0);
            mount.Barrels.localRotation = Quaternion.Euler(-pitch,0,0);
        }
        if (Input.GetMouseButtonDown(0) && mount.Reload <= 0 && NavalCapitalLayout.Bears(selected,mount.Yaw))
        {
            int before = mount.Ammo;
            FireCapitalTube(mount,selected,-1);
            if (mount.Ammo > 0) FireCapitalTube(mount,selected,1);
            if (mount.Ammo < before) mount.Reload = NavalCapitalLayout.Reload;
        }
        return true;
    }

    internal bool CapitalSight(out Vector3 position,out Quaternion rotation)
    {
        position=Vector3.zero;rotation=Quaternion.identity;
        if(playerCapitalSlot<0||player==null||!player.IsAlive())return false;
        var barrel=capitalTurrets[playerCapitalSlot].Barrels;
        position=barrel.TransformPoint(new Vector3(0,0,CapitalMuzzleZ)) +barrel.up*.35f;
        rotation=Quaternion.LookRotation(barrel.forward,boat.up);return true;
    }
    private bool DrawCapitalControls()
    {
        if (playerCapitalSlot < 0 && nearbyCapitalSlot < 0) return false;
        string hint = playerCapitalSlot < 0 ? "E — взять управление башней ГК" :
            "Мышь / A/D, W/S — наводка   ЛКМ — залп   E — выйти";
        if(playerCapitalSlot>=0){
            float left=capitalTurrets[playerCapitalSlot].Reload;
            Rect bar=new Rect(Screen.width*.5f-110,Screen.height-178,220,16);
            Color saved=GUI.color;GUI.color=new Color(.12f,.14f,.15f,.85f);GUI.DrawTexture(bar,Texture2D.whiteTexture);
            GUI.color=new Color(.48f,.65f,.67f,.85f);GUI.DrawTexture(new Rect(bar.x,bar.y,bar.width*(1f-Mathf.Clamp01(left/NavalCapitalLayout.Reload)),bar.height),Texture2D.whiteTexture);GUI.color=saved;
            GUI.Label(new Rect(bar.x,bar.y-20,260,20),left>0?"Перезарядка ГК: "+left.ToString("F1")+" с":"ГК готов к залпу");
        }
        if(playerCapitalSlot>=0)GUI.Label(new Rect(Screen.width*.5f-6,Screen.height*.5f-12,20,24),"+");
        GUI.Box(new Rect(Screen.width*.5f-245,Screen.height-100,490,26),hint);
        return true;
    }
}
