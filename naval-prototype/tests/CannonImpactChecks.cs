using System;
class CannonImpactChecks
{
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
    static int DeckCells(float x,float y,float z,float radius)
    {
        float length=(float)Math.Sqrt(x*x+y*y+z*z),inset=NavalCannonImpactMath.InsetFraction(length);
        x*=1f-inset;y*=1f-inset;z*=1f-inset;
        int count=0;
        for(int a=-4;a<=4;a++)for(int b=-4;b<=4;b++)
        {
            float dx=a*0.5f-x,dz=b*0.5f-z;
            if(dx*dx+y*y+dz*dz<=radius*radius)count++;
        }
        return count;
    }
    static void Main()
    {
        Check(!NavalCannonImpactMath.ValidContact(0f,0f,0f,0f),"Initial sphere overlap must use closest-point fallback");
        Check(!NavalCannonImpactMath.ValidContact(0.1f,0f,-80f,0f),"World-origin sentinel outside elevated ship is rejected");
        Check(NavalCannonImpactMath.ValidContact(0.1f,0.5f,0.2f,-0.4f),"Legitimate face contact accepted");
        Check(!NavalCannonImpactMath.ValidContact(0.1f,0.6f,0.2f,0f),"Stale contact outside cell rejected");
        float radius=NavalRigImpactMath.CannonRadius*2f;
        int centre=DeckCells(0f,0.25f,0f,radius),edge=DeckCells(0.25f,0.25f,0f,radius),corner=DeckCells(0.25f,0.25f,0.25f,radius);
        Check(centre>1&&edge>1&&corner>1,"Centre, edge and corner deck impacts damage a compact cluster");
        Check(centre<=9&&edge<=9&&corner<=9,"Fix retains reduced hole footprint");
        Check(DeckCells(0f,0.25f,0f,radius*NavalProjectileWaterMath.DamageRadiusScale)<centre,"Underwater attenuation preserved");
        Check(NavalCannonImpactMath.InsetFraction(0f)==0f,"Inside-cell impact has no invalid direction");
        Check(NavalCannonImpactMath.InsetFraction(0.05f)==1f,"Inset never crosses voxel centre");
        Console.WriteLine("PASS: invalid initial contacts, compact deck cluster ("+centre+"/"+edge+"/"+corner+"), underwater attenuation.");
    }
}
