using UnityEngine;
public partial class NavalStations
{
    private bool HullPointVisible(int gun,NavalStations victim,NavalHullVoxel cell)
    {
        if(cell==null||cell.Missing||cell.Detached)return false;
        var first=FirstVisibleCell(gun,cell);
        return first!=null&&first.Owner==victim.hull&&!first.ManagedRig&&
            (first.transform.position-cell.transform.position).sqrMagnitude<2.25f;
    }
    private bool HullZone(int gun,NavalStations victim,Vector3 velocity,bool deck,out NavalHullVoxel selected)
    {
        selected=null;victim.PrepareTargetIndex();
        for(int i=0;i<8;i++){hullProbes[i]=null;hullProbeScores[i]=float.PositiveInfinity;}
        Vector3 muzzle=barrels[gun].TransformPoint(new Vector3(0,0,1.38f));
        Vector3 view=victim.boat.InverseTransformPoint(muzzle);
        foreach(var cell in deck?victim.deckTargets:victim.shellTargets)
        {
            if(cell==null||cell.Missing||cell.Detached)continue;
            Vector3 local=cell.LocalCenter,world=cell.transform.position;float score;
            if(deck)
            {
                if(!AboveWater(cell))continue;
                score=(world-muzzle).sqrMagnitude;
            }
            else
            {
                if(!NavalHullAimZones.Facing(view.x,view.z,local.x,local.z,victim.hull.Definition.HullHalfWidth(local.z)))continue;
                float sea=ocean.Height(world.x,world.z),depth=world.y-sea;
                if(depth<-.65f||depth>.65f)continue;
                score=Mathf.Abs(depth+.25f)*8f+(world-muzzle).magnitude*.01f+Random.value*.8f;
            }
            // Keep several candidates: one hidden or unreachable plank must not suppress firing.
            for(int i=0;i<8;i++)if(score<hullProbeScores[i])
            {
                for(int j=7;j>i;j--){hullProbeScores[j]=hullProbeScores[j-1];hullProbes[j]=hullProbes[j-1];}
                hullProbeScores[i]=score;hullProbes[i]=cell;break;
            }
        }
        for(int i=0;i<8;i++)
        {
            var cell=hullProbes[i];if(cell==null)continue;Vector3 predicted;
            if(ReachableLead(gun,cell.transform.position,velocity,out predicted)&&HullPointVisible(gun,victim,cell))
            {selected=cell;return true;}
        }
        return false;
    }
}
