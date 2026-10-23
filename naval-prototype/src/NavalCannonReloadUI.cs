using UnityEngine;
public partial class NavalStations
{
    private void DrawReloadBars()
    {
        if(Event.current.type!=EventType.Repaint)return;
        NavalTestWorld world=NavalTestWorld.Active;
        if(EnemyControlled||boat==null||hull==null||hull.Sunk||world==null||!world.SimulationActive)return;
        Camera camera=world.ViewCamera;if(camera==null)return;
        for(int gun=0;gun<GunCount;gun++)
        {
            if(guns[gun]==null)continue;
            var repair=cannonMechanisms!=null?cannonMechanisms[gun]:null;
            if(repair!=null&&Time.time<repair.ProgressUntil)
            {
                float remainingRepair=Mathf.Max(0,repair.RepairSeconds-repair.DisplaySeconds);
                string text=repair.RepairStage=="Ремонт"?"Ремонт "+Mathf.CeilToInt(remainingRepair)+" с":repair.RepairStage;
                NavalWorldProgressBar.Draw(camera,guns[gun].TransformPoint(new Vector3(0,1.5f,0)),repair.DisplaySeconds/repair.RepairSeconds,true,false,text,guns[gun].GetInstanceID());continue;
            }
            bool functional=Functional(gun+1);float remaining=Mathf.Max(0f,readyAt[gun]-Time.time);
            float progress=functional?Mathf.Clamp01(1f-remaining/NavalGunneryMath.ReloadSeconds):0f;
            NavalWorldProgressBar.Draw(camera,guns[gun].TransformPoint(new Vector3(0f,1.5f,0f)),progress,functional,remaining<=0f,!functional?"DAMAGED":remaining<=0f?"READY":remaining.ToString("F1")+" s",guns[gun].GetInstanceID());
        }
    }
}
