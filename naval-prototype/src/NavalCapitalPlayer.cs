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
        float yaw = mount.Yaw + turn * 8f * Mathf.Min(dt,.02f);
        float pitch = Mathf.Clamp(mount.Pitch + elevate * 6f * dt, NavalCapitalLayout.MinimumPitch(selected), NavalTurretAccessLayout.MaximumPitch);
        if ((turn != 0 || elevate != 0) && CapitalBarrelsClear(mount,yaw,pitch))
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

    private bool DrawCapitalControls()
    {
        if (playerCapitalSlot < 0 && nearbyCapitalSlot < 0) return false;
        string hint = playerCapitalSlot < 0 ? "E — взять управление башней ГК" :
            "A/D — поворот   W/S — высота   ЛКМ — залп   E — выйти";
        GUI.Box(new Rect(Screen.width*.5f-245,Screen.height-100,490,26),hint);
        return true;
    }
}
