using System;
using System.Collections.Generic;

public struct NavalSolidBox
{
    public int Id;
    public float MinX,MinY,MinZ,MaxX,MaxY,MaxZ;
    public float Low(int axis){return axis==0?MinX:axis==1?MinY:MinZ;}
    public float High(int axis){return axis==0?MaxX:axis==1?MaxY:MaxZ;}
    public void Extend(int axis,float high){if(axis==0)MaxX=high;else if(axis==1)MaxY=high;else MaxZ=high;}
    public double DistanceSquared(float x,float y,float z)
    {
        double dx=Math.Max(0,Math.Max((double)MinX-x,(double)x-MaxX));
        double dy=Math.Max(0,Math.Max((double)MinY-y,(double)y-MaxY));
        double dz=Math.Max(0,Math.Max((double)MinZ-z,(double)z-MaxZ));return dx*dx+dy*dy+dz*dz;
    }
    public double CenterDistanceSquared(float x,float y,float z)
    {double dx=((double)MinX+MaxX)*.5-x,dy=((double)MinY+MaxY)*.5-y,dz=((double)MinZ+MaxZ)*.5-z;return dx*dx+dy*dy+dz*dz;}
}
public sealed class NavalMergedBox
{
    public NavalSolidBox Box;
    public readonly List<int> Cells=new List<int>();
}
// Only identical orthogonal faces and overlapping/touching intervals merge.
// No positive gap is ever filled, including gaps smaller than float physics tolerances.
public sealed class NavalBoxMerger
{
    private sealed class Order:IComparer<NavalMergedBox>
    {
        private readonly int axis;
        public Order(int value){axis=value;}
        public int Compare(NavalMergedBox a,NavalMergedBox b)
        {
            for(int i=1;i<=2;i++)
            {
                int other=(axis+i)%3,result=a.Box.Low(other).CompareTo(b.Box.Low(other));if(result!=0)return result;
                result=a.Box.High(other).CompareTo(b.Box.High(other));if(result!=0)return result;
            }
            int order=a.Box.Low(axis).CompareTo(b.Box.Low(axis));return order!=0?order:a.Box.Id.CompareTo(b.Box.Id);
        }
    }
    private static readonly Order[] orders={new Order(0),new Order(1),new Order(2)};
    private readonly List<NavalMergedBox> pool=new List<NavalMergedBox>();
    private List<NavalMergedBox> working=new List<NavalMergedBox>(),scratch=new List<NavalMergedBox>();
    public List<NavalMergedBox> Build(List<NavalSolidBox> source)
    {
        working.Clear();scratch.Clear();
        for(int i=0;i<source.Count;i++)
        {
            NavalSolidBox box=source[i];
            if(!(box.MinX<box.MaxX&&box.MinY<box.MaxY&&box.MinZ<box.MaxZ))throw new ArgumentException("Degenerate box");
            if(pool.Count<=i)pool.Add(new NavalMergedBox());
            NavalMergedBox entry=pool[i];entry.Box=box;entry.Cells.Clear();entry.Cells.Add(box.Id);working.Add(entry);
        }
        bool changed;
        do
        {
            changed=false;
            for(int axis=0;axis<3;axis++)
            {
                working.Sort(orders[axis]);scratch.Clear();NavalMergedBox current=null;
                foreach(NavalMergedBox next in working)
                {
                    bool join=current!=null&&next.Box.Low(axis)<=current.Box.High(axis);
                    for(int other=0;other<3&&join;other++)if(other!=axis)
                        join=current.Box.Low(other)==next.Box.Low(other)&&current.Box.High(other)==next.Box.High(other);
                    if(join)
                    {
                        current.Box.Extend(axis,Math.Max(current.Box.High(axis),next.Box.High(axis)));
                        current.Cells.AddRange(next.Cells);changed=true;
                    }
                    else {scratch.Add(next);current=next;}
                }
                List<NavalMergedBox> swap=working;working=scratch;scratch=swap;
            }
        }while(changed);
        return working;
    }
}
public static class NavalBoxHitResolver
{
    // Closest actual source box; deterministic tie at shared edges, independent of merge order.
    public static int Resolve(List<NavalSolidBox> source,List<int> members,float x,float y,float z,float tolerance)
    {
        int selected=-1;double best=double.PositiveInfinity,center=double.PositiveInfinity;
        foreach(int id in members)
        {
            NavalSolidBox box=source[id];double distance=box.DistanceSquared(x,y,z),middle=box.CenterDistanceSquared(x,y,z);
            if(distance<best||distance==best&&(middle<center||middle==center&&(selected<0||id<selected)))
            {selected=id;best=distance;center=middle;}
        }
        return best<=(double)tolerance*tolerance?selected:-1;
    }
}
