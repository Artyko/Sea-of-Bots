public static class NavalRepairMaterials
{
    public static bool NeedsPlanks(NavalHullVoxel cell)
    {
        return cell!=null&&NeedsPlanks(cell.HydraulicShell,cell.InternalBarrier,cell.RepairableEquipment,cell.RepairMechanism!=null&&cell.RepairMechanism.IsCannon);
    }
    public static bool NeedsPlanks(bool shell,bool barrier,bool equipment,bool cannon)
    {
        if(!shell&&!barrier&&!equipment)return false;
        // Guns retain their dedicated spare-parts workflow. Every other hull,
        // barrier, pump, helm or machinery repair consumes a plank bundle.
        return !cannon;
    }
    public static bool ReceivesWoodPatch(NavalHullVoxel cell)
    {return cell!=null&&(cell.HydraulicShell||cell.InternalBarrier);}
}
