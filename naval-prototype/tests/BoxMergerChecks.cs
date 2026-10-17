using System;
using System.Collections.Generic;
class BoxMergerChecks
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static NavalSolidBox Box(int id,float x,float y,float z,float sx,float sy,float sz)
    {return new NavalSolidBox{Id=id,MinX=x,MinY=y,MinZ=z,MaxX=x+sx,MaxY=y+sy,MaxZ=z+sz};}
    static double SourceDistance(List<NavalSolidBox> boxes,float x,float y,float z)
    {double best=double.PositiveInfinity;foreach(NavalSolidBox box in boxes)best=Math.Min(best,box.DistanceSquared(x,y,z));return best;}
    static double MergedDistance(List<NavalMergedBox> boxes,float x,float y,float z)
    {double best=double.PositiveInfinity;foreach(NavalMergedBox box in boxes)best=Math.Min(best,box.Box.DistanceSquared(x,y,z));return best;}
    static double Ray(NavalSolidBox box,double[] origin,double[] direction)
    {
        double near=0,far=1000;
        for(int axis=0;axis<3;axis++)
        {
            if(Math.Abs(direction[axis])<1e-12){if(origin[axis]<box.Low(axis)||origin[axis]>box.High(axis))return double.PositiveInfinity;continue;}
            double a=(box.Low(axis)-origin[axis])/direction[axis],b=(box.High(axis)-origin[axis])/direction[axis];
            near=Math.Max(near,Math.Min(a,b));far=Math.Min(far,Math.Max(a,b));if(near>far)return double.PositiveInfinity;
        }
        return near;
    }
    static void Compare(List<NavalSolidBox> full,List<NavalSolidBox> input,List<NavalMergedBox> output,Random random)
    {
        HashSet<int> seen=new HashSet<int>();
        foreach(NavalMergedBox merged in output)
        {
            double volume=(merged.Box.MaxX-merged.Box.MinX)*(merged.Box.MaxY-merged.Box.MinY)*(merged.Box.MaxZ-merged.Box.MinZ);
            Check(volume>0,"positive output volume");
            foreach(int id in merged.Cells)Check(seen.Add(id),"each source occurs once");
            for(int sample=0;sample<20;sample++)
            {
                float x=merged.Box.MinX+(float)random.NextDouble()*(merged.Box.MaxX-merged.Box.MinX);
                float y=merged.Box.MinY+(float)random.NextDouble()*(merged.Box.MaxY-merged.Box.MinY);
                float z=merged.Box.MinZ+(float)random.NextDouble()*(merged.Box.MaxZ-merged.Box.MinZ);
                int resolved=NavalBoxHitResolver.Resolve(full,merged.Cells,x,y,z,.00001f);
                Check(resolved>=0&&full[resolved].DistanceSquared(x,y,z)<=1e-10,"proxy points resolve to a real source voxel");
            }
        }
        Check(seen.Count==input.Count,"no dropped input cells");
        foreach(NavalSolidBox box in input)Check(seen.Contains(box.Id),"non-contiguous ids preserved");
        for(int sample=0;sample<80;sample++)
        {
            float x=(float)(random.NextDouble()*5-1),y=(float)(random.NextDouble()*5-1),z=(float)(random.NextDouble()*5-1);
            double source=SourceDistance(input,x,y,z),merged=MergedDistance(output,x,y,z);
            Check(source==merged||Math.Abs(source-merged)<1e-10,"same closest distance, including initial sphere overlaps");
            double[] origin={x,y,z},direction={random.NextDouble()*2-1,random.NextDouble()*2-1,random.NextDouble()*2-1};
            double sourceRay=double.PositiveInfinity,mergedRay=double.PositiveInfinity;
            foreach(NavalSolidBox box in input)sourceRay=Math.Min(sourceRay,Ray(box,origin,direction));
            foreach(NavalMergedBox box in output)mergedRay=Math.Min(mergedRay,Ray(box.Box,origin,direction));
            Check(sourceRay==mergedRay||Math.Abs(sourceRay-mergedRay)<1e-9,"same first ray contact and open passages");
        }
        // Exhaustively compare all quarter-grid atoms, covering every hole of these fixtures.
        for(int x=0;x<12;x++)for(int y=0;y<12;y++)for(int z=0;z<12;z++)
        {
            float px=(x+.5f)*.25f,py=(y+.5f)*.25f,pz=(z+.5f)*.25f;
            Check((SourceDistance(input,px,py,pz)==0)==(MergedDistance(output,px,py,pz)==0),"exact occupied volume; no sealed hole or fabricated solid");
        }
    }
    static void Main()
    {
        Random random=new Random(8917);NavalBoxMerger merger=new NavalBoxMerger();List<NavalSolidBox> full=new List<NavalSolidBox>(),input=new List<NavalSolidBox>();
        for(int x=0;x<8;x++)for(int y=0;y<4;y++)for(int z=0;z<8;z++)full.Add(Box(full.Count,x*.25f,y*.25f,z*.25f,.25f,.25f,.25f));
        input.AddRange(full);Check(merger.Build(input).Count==1,"intact rectangular block merges into one box");
        for(int scenario=0;scenario<100;scenario++)
        {
            input.Clear();foreach(NavalSolidBox box in full)if(random.NextDouble()>.35)input.Add(box);
            Compare(full,input,merger.Build(input),random);
        }
        input.Clear();Check(merger.Build(input).Count==0,"fully destroyed chunk has no collider");
        input.AddRange(full);Check(merger.Build(input).Count==1,"restore after complete destruction");
        // Different sizes, overlaps and L shapes must also retain exactly the same union.
        for(int scenario=0;scenario<40;scenario++)
        {
            full.Clear();input.Clear();
            for(int i=0;i<35;i++)full.Add(Box(i,random.Next(0,8)*.25f,random.Next(0,8)*.25f,random.Next(0,8)*.25f,random.Next(1,4)*.25f,random.Next(1,4)*.25f,random.Next(1,4)*.25f));
            input.AddRange(full);Compare(full,input,merger.Build(input),random);
        }
        input.Clear();input.Add(Box(0,0,0,0,1,1,1));input.Add(Box(1,1.000001f,0,0,1,1,1));
        Check(merger.Build(input).Count==2,"even a tiny positive gap must not be bridged");
        input[1]=Box(1,1,0,0,1,1,.5f);Check(merger.Build(input).Count==2,"partial face is not a full box union");
        input[1]=Box(1,1,0,0,1,1,1);List<NavalMergedBox> joined=merger.Build(input);
        Check(joined.Count==1,"full face joins");
        Check(NavalBoxHitResolver.Resolve(input,joined[0].Cells,.2f,.5f,.5f,.001f)==0,"ray face maps left cell");
        Check(NavalBoxHitResolver.Resolve(input,joined[0].Cells,1.8f,.5f,.5f,.001f)==1,"ray face maps right cell");
        Check(NavalBoxHitResolver.Resolve(input,joined[0].Cells,1f,.5f,.5f,.001f)==0,"shared edge tie deterministic");
        Check(NavalBoxHitResolver.Resolve(input,joined[0].Cells,20f,20f,20f,.001f)<0,"invalid contact is rejected instead of damaging a distant voxel");
        Console.WriteLine("PASS: exact unions through 140 damaged/overlapping fixtures, exhaustive hole occupancy, closest-point/sphere overlap equivalence, first ray hits, restoration and precise source mapping; 256 intact cells -> 1 box.");
    }
}
