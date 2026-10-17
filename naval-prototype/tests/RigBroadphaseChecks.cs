using System;
class RigBroadphaseChecks
{
    static void Check(bool value,string name){if(!value)throw new Exception(name);}
    static double[,] Rotation(Random random)
    {
        double a=random.NextDouble()*6.28,b=random.NextDouble()*6.28,c=random.NextDouble()*6.28;
        double ca=Math.Cos(a),sa=Math.Sin(a),cb=Math.Cos(b),sb=Math.Sin(b),cc=Math.Cos(c),sc=Math.Sin(c);
        return new double[,]{{cc*cb,cc*sb*sa-sc*ca,cc*sb*ca+sc*sa},{sc*cb,sc*sb*sa+cc*ca,sc*sb*ca-cc*sa},{-sb,cb*sa,cb*ca}};
    }
    static double[] Apply(double[,] r,double[] p)
    {double[] v=new double[3];for(int i=0;i<3;i++)for(int j=0;j<3;j++)v[i]+=r[i,j]*p[j];return v;}
    static double[] Extent(double[,] r,double[] half)
    {double[] v=new double[3];for(int i=0;i<3;i++)for(int j=0;j<3;j++)v[i]+=Math.Abs(r[i,j])*half[j];return v;}
    static void Main()
    {
        Random random=new Random(8719);int contacts=0;
        for(int test=0;test<80000;test++)
        {
            double[,] body=Rotation(random),cell=Rotation(random),relative=new double[3,3];
            for(int i=0;i<3;i++)for(int j=0;j<3;j++)for(int k=0;k<3;k++)relative[i,j]+=body[k,i]*cell[k,j];
            double scale=test%2==0?1:1.5;
            double[] half={(float)(.03+random.NextDouble()*2),(float)(.03+random.NextDouble()*2),(float)(.03+random.NextDouble()*2)};
            double[] extent=Extent(relative,half);
            double[] local={(random.NextDouble()*2-1)*(6.35*scale+extent[0])*1.2,(random.NextDouble()*2-1)*(4.95*scale+extent[1])*1.2,(random.NextDouble()*2-1)*(12.65*scale+extent[2])*1.2};
            bool hit=NavalRigImpactMath.TouchesHull((float)local[0],(float)local[1],(float)local[2],(float)extent[0],(float)extent[1],(float)extent[2],(float)scale);
            if(!hit)continue;contacts++;
            double[] center=Apply(body,local),bodyCenter=Apply(body,new double[]{0,-.225*scale,0});
            double[] bodyExtent=Extent(body,new double[]{6.35*scale,4.725*scale,12.65*scale});
            double padding=NavalRigImpactMath.BroadphasePadding((float)half[0],(float)half[1],(float)half[2]);
            for(int axis=0;axis<3;axis++)
                Check(Math.Abs(center[axis]-bodyCenter[axis])<=bodyExtent[axis]+padding+.004,"broadphase must retain every previous TouchesHull result");
        }
        Check(contacts>20000,"enough random contact samples");
        Console.WriteLine("PASS: 80000 arbitrary hull/cell rotations and sloop/brig scales; retained "+contacts+" exact-predicate contacts.");
    }
}
