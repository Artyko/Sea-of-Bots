using System.Collections.Generic;
using UnityEngine;

// One mechanism owns all its original voxels; repair eligibility is not per patch.
public sealed class NavalRepairableMechanism
{
    private readonly NavalHull hull;
    private readonly List<NavalHullVoxel> cells;
    private readonly NavalMechanismDamage state=new NavalMechanismDamage();
    private int revision=-1,damaged;
    public readonly bool IsPump;
    public NavalRepairableMechanism(NavalHull owner,IEnumerable<NavalHullVoxel> members,bool pump)
    {
        hull=owner;IsPump=pump;cells=new List<NavalHullVoxel>(members);
        foreach(NavalHullVoxel cell in cells)
        {
            cell.RepairableEquipment=true;cell.RepairMechanism=this;
            // Equipment can have a moving parent; repairs target its original hull-space location.
            cell.LocalCenter=owner.Root.InverseTransformPoint(cell.transform.position);
        }
    }
    private void Refresh()
    {
        if(revision==hull.Revision)return;revision=hull.Revision;damaged=0;
        foreach(NavalHullVoxel cell in cells)if(cell==null||cell.Missing||cell.Detached)damaged++;
        state.Observe(cells.Count,damaged);
    }
    public bool CanRepair {get{Refresh();return cells.Count>0&&!state.Destroyed;}}
    public bool NeedsRepair {get{Refresh();return damaged>0&&CanRepair;}}
    public bool Destroyed {get{Refresh();return state.Destroyed;}}
    public bool Functional {get{Refresh();return cells.Count>0&&damaged==0&&!state.Destroyed;}}
    public string Name {get{return IsPump?"Помпа":"Штурвал";}}
    public void Reset(){state.Reset();revision=-1;}
}
