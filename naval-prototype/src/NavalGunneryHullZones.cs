using UnityEngine;
public partial class NavalStations
{
    private int aimSampleStamp, aimSampleFrame = -1;
    private Matrix4x4 aimSamplePose;
    private NavalWater aimSampleOcean;
    private Vector3 aimSampleOrigin;
    private float aimSampleTime, aimSampleAmplitude;

    private void PrepareAimSamples(NavalWater water)
    {
        int frame = Time.frameCount;
        Matrix4x4 pose = boat.localToWorldMatrix;
        Vector3 origin = water.transform.position;
        float time = water.WaveSampleTime, amplitude = water.Amplitude;
        if (aimSampleFrame == frame && aimSampleOcean == water && aimSamplePose.Equals(pose) &&
            aimSampleOrigin.Equals(origin) && aimSampleTime == time && aimSampleAmplitude == amplitude)
            return;

        aimSampleFrame = frame;
        aimSamplePose = pose;
        aimSampleOcean = water;
        aimSampleOrigin = origin;
        aimSampleTime = time;
        aimSampleAmplitude = amplitude;
        aimSampleStamp++;
    }

    private float SampleAimDepth(NavalHullVoxel cell, out Vector3 world,
        float minDepth=float.NegativeInfinity,float maxDepth=float.PositiveInfinity)
    {
        bool fixedCell = cell.AimRootCell;
        if (fixedCell && cell.AimSampleStamp == aimSampleStamp)
        {
            world = cell.AimSampleWorld;
            return cell.AimSampleDepth;
        }

        world = fixedCell?aimSamplePose.MultiplyPoint3x4(cell.LocalCenter):cell.transform.position;
        // A point outside the wave envelope cannot enter the requested band.
        float relativeHeight=world.y-aimSampleOrigin.y;
        float crest=Mathf.Abs(aimSampleAmplitude)+.001f;
        if(relativeHeight+crest<minDepth)return float.NegativeInfinity;
        if(relativeHeight-crest>maxDepth)return float.PositiveInfinity;
        float depth = world.y - aimSampleOcean.Height(world.x, world.z);
        if (fixedCell)
        {
            cell.AimSampleStamp = aimSampleStamp;
            cell.AimSampleWorld = world;
            cell.AimSampleDepth = depth;
        }
        return depth;
    }

    private float ArmorAimIncidence(NavalHullVoxel cell,Vector3 source,bool deck)
    {
        return ArmorAimIncidenceLocal(cell,boat.InverseTransformPoint(source),deck);
    }
    private float ArmorAimIncidenceLocal(NavalHullVoxel cell,Vector3 view,bool deck)
    {
        Vector3 p=cell.LocalCenter;
        float slope=deck?0f:cell.AimWidthSlope;
        bool end=Mathf.Abs(p.z)>5.85f;
        return NavalHullAimZones.Incidence(view.x,view.y,view.z,p.x,p.y,p.z,slope,end,deck);
    }
    private bool HullPointVisible(int gun,NavalStations victim,NavalHullVoxel cell)
    {
        if(cell==null||cell.Missing||cell.Detached)return false;
        var first=FirstVisibleCell(gun,cell);
        return first!=null&&first.Owner==victim.hull&&!first.ManagedRig&&
            (first.transform.position-cell.transform.position).sqrMagnitude<2.25f;
    }
    private bool HullZone(int gun,NavalStations victim,Vector3 velocity,bool deck,bool belowWaterline,out NavalHullVoxel selected)
    {
        selected=null;victim.PrepareTargetIndex();victim.PrepareAimSamples(ocean);
        for(int i=0;i<8;i++){hullProbes[i]=null;hullProbeScores[i]=float.PositiveInfinity;}
        Vector3 muzzle=barrels[gun].TransformPoint(new Vector3(0,0,hull.Definition.GunMuzzleZ));
        Vector3 view=victim.boat.InverseTransformPoint(muzzle);
        foreach(var cell in deck?victim.deckTargets:victim.shellTargets)
        {
            if(cell==null||cell.Missing||cell.Detached)continue;
            Vector3 world;float score;
            if(deck)
            {
                if(victim.SampleAimDepth(cell,out world,.03f)<.03f)continue;
                score=(world-muzzle).magnitude*.01f+(1f-victim.ArmorAimIncidenceLocal(cell,view,true))*18f;
            }
            else
            {
                float incidence=victim.ArmorAimIncidenceLocal(cell,view,false);
                if(incidence<.12f)continue;
                float depth=victim.SampleAimDepth(cell,out world,belowWaterline?-.95f:-.08f,belowWaterline?-.12f:.78f);
                if(belowWaterline)
                {
                    if(depth<-.95f||depth>-.12f)continue;
                }
                else if(depth<-.08f||depth>.78f)continue;
                score=Mathf.Abs(depth-NavalGunneryMath.DesiredHullDepth(belowWaterline))*10f+
                    (world-muzzle).magnitude*.01f+Random.value*.55f+(1f-incidence)*18f;
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
