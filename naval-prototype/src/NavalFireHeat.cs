using System.Collections.Generic;
using UnityEngine;

// Sustained heat damages mechanisms, independently of ammunition cook-off.
public sealed partial class NavalFireSystem
{
    private readonly List<NavalHullVoxel> heatCells=new List<NavalHullVoxel>();
    private readonly NavalPointIndex heatIndex=new NavalPointIndex();
    private readonly List<int> heatNearby=new List<int>();
    private NavalFireHeatState[] heatStates;
    private float[] heatLoads;
    private void BuildHeatIndex()
    {
        foreach(var cell in hull.Voxels)if(cell.RepairMechanism!=null){
            int id=heatCells.Count;heatCells.Add(cell);Vector3 p=cell.LocalCenter;heatIndex.Add(id,p.x,p.y,p.z);
        }
        heatStates=new NavalFireHeatState[heatCells.Count];heatLoads=new float[heatCells.Count];
        for(int i=0;i<heatStates.Length;i++)heatStates[i]=new NavalFireHeatState();
    }
    private void HeatMechanisms()
    {
        if(heatStates==null)return;
        System.Array.Clear(heatLoads,0,heatLoads.Length);
        foreach(var flame in flames)if(flame.Active){
            Vector3 point=flame.Cell.transform.position,local=hull.Root.InverseTransformPoint(point);
            heatIndex.Query(local.x,local.y,local.z,1.8f/hull.WorldScale,heatNearby);
            foreach(int i in heatNearby){var cell=heatCells[i];if(cell==null||cell.Missing||cell.Detached)continue;
                float distance=(cell.transform.position-point).magnitude;
                heatLoads[i]=Mathf.Max(heatLoads[i],flame.Flame.Intensity*Mathf.Clamp01(1f-distance/1.8f));
            }
        }
        int lost=0;
        for(int i=0;i<heatCells.Count;i++){
            var cell=heatCells[i];if(cell==null||cell.Missing||cell.Detached){heatStates[i].Reset();continue;}
            if(heatLoads[i]<=0f&&heatStates[i].Heat<=0f)continue;
            float load=Underwater(cell.transform.position)?0f:heatLoads[i];
            if(heatStates[i].Step(1f,load)){cell.SetMissing(true);lost++;}
        }
        if(lost>0)Debug.Log("[NavalPrototype] Fire heat damaged mechanisms: ship="+hull.Root.name+" cells="+lost);
    }
}
