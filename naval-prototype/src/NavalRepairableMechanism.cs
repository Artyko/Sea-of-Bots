using System.Collections.Generic;
using UnityEngine;

// One mechanism owns all its original voxels; repair eligibility is not per patch.
public sealed class NavalRepairableMechanism
{
    private readonly NavalHull hull;
    private readonly List<NavalHullVoxel> cells;
    private readonly NavalMechanismDamage state=new NavalMechanismDamage();
    private int revision=-1,damaged;
    public readonly bool IsPump,IsCannon;
    private readonly NavalCannonRepairBudget cannonRepair=new NavalCannonRepairBudget();
    public NavalRepairSailor RepairWorker;
    public float PlayerRepairUntil;
    public Vector3 RepairPoint;
    public bool PartsDelivered;
    public float DisplaySeconds,ProgressUntil;
    public string RepairStage;
    public bool CanFetchParts {get{return PartsDelivered||hull.HasCannonStock;}}
    public bool RecoverableWithParts {get{return CanFetchParts||hull.CannonSupply!=null&&hull.CannonSupply.PlayerHasKit||RepairWorker!=null&&RepairWorker.CarryingCannonParts(this);}}
    public void ReportRepair(float seconds,string stage){DisplaySeconds=seconds;RepairStage=stage;ProgressUntil=Time.time+.3f;}
    public bool RestoringCannon {get;private set;}
    public bool RepairUsed {get{return IsCannon&&cannonRepair.Used;}}
    public float RepairSeconds {get{return IsCannon?NavalCannonRepairBudget.Duration:4f;}}
    public NavalRepairableMechanism(NavalHull owner,IEnumerable<NavalHullVoxel> members,bool pump,bool cannon=false)
    {
        hull=owner;IsPump=pump;IsCannon=cannon;cells=new List<NavalHullVoxel>(members);
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
    public bool CanRepair {get{Refresh();return cells.Count>0&&(IsCannon?!cannonRepair.Used:!state.Destroyed);}}
    public bool NeedsRepair {get{Refresh();return damaged>0&&CanRepair;}}
    public bool Destroyed {get{Refresh();return IsCannon?damaged>0&&cannonRepair.Used:state.Destroyed;}}
    public bool Functional {get{Refresh();return cells.Count>0&&damaged==0&&(IsCannon||!state.Destroyed);}}
    public string Name {get{return IsCannon?"Пушка":IsPump?"Помпа":"Штурвал";}}
    public bool CompleteCannonRepair(NavalStructure structure,float seconds)
    {
        if(!IsCannon||!CanRepair||!PartsDelivered||structure==null||!cannonRepair.Ready(seconds))return false;
        RestoringCannon=true;
        try{foreach(var cell in cells)if(cell!=null&&(cell.Missing||cell.Detached))structure.RestoreCell(cell);}
        finally{RestoringCannon=false;revision=-1;}
        Refresh();
        bool complete=cannonRepair.Complete(damaged==0,seconds);
        if(complete)Debug.Log("[NavalPrototype] Cannon restored once: "+hull.Root.name+" at "+RepairPoint);
        return complete;
    }
    public void Reset(){state.Reset();cannonRepair.Reset();RepairWorker=null;PlayerRepairUntil=0;PartsDelivered=false;ProgressUntil=DisplaySeconds=0;revision=-1;}
}
