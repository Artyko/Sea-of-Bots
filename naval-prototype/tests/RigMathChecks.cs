using System;
class RigMathChecks
{
    static void Check(bool result, string name) { if (!result) throw new Exception(name); }
    static void Main()
    {
        // Two columns: one damaged cell must not sever a still connected mast.
        float[,] bounds = new float[8,6];
        for(int row=0;row<4;row++)for(int col=0;col<2;col++)
        { int i=row*2+col; bounds[i,0]=col; bounds[i,1]=row; bounds[i,3]=bounds[i,4]=bounds[i,5]=0.5f; }
        bool[] missing = new bool[8];
        bool[] supported = NavalRigMath.Supported(bounds,missing,0f);
        Check(supported[7],"Intact mast connects to deck");
        missing[2]=true;
        supported=NavalRigMath.Supported(bounds,missing,0f);
        Check(supported[7],"Partial notch keeps support");
        missing[3]=true;
        supported=NavalRigMath.Supported(bounds,missing,0f);
        Check(!supported[4]&&!supported[7]&&supported[0],"Severed mast drops upper cells only");
        missing[2]=missing[3]=false;
        Check(NavalRigMath.Supported(bounds,missing,0f)[7],"Repair restores support");
        Check(NavalRigMath.SailPower(352,352,true)==1f,"Full sail");
        Check(NavalRigMath.SailPower(176,352,true)==0.5f,"Half sail gives half speed");
        Check(NavalRigMath.SailPower(0,352,true)==0f,"Destroyed sail has no drive");
        Check(NavalRigMath.SailPower(352,352,false)==0f,"Fallen sail has no drive");
        Check(NavalRigMath.RudderRate(0f,1f)==0f,"No pivot at rest");
        Check(NavalRigMath.RudderRate(2f,1f)==6f,"Rudder scales with forward speed");
        Check(NavalRigMath.RudderRate(-2f,1f)==-6f,"Rudder reverses when backing up");
        float rudder=NavalRigMath.StepRudder(0f,1f,0.05f);
        Check(rudder>0f&&rudder<0.1f,"Initial steering input is gradual");
        Check(NavalRigMath.StepRudder(1f,-1f,0.05f)>0f,"Opposite steering does not instantly reverse rudder");
        Check(NavalRigMath.StepRudder(1f,0f,0.5f)<0.4f,"Released rudder returns toward centre");
        Check(Math.Abs(NavalRigMath.StepRudder(0f,1f,1f)-NavalRigMath.StepRudder(NavalRigMath.StepRudder(0f,1f,0.5f),1f,0.5f))<0.00001f,"Rudder response independent of frame subdivision");
        Check(NavalRigMath.RudderRate(4f,0f)==0f,"Neutral rudder does not turn");
        // Actual mast dimensions: a cut in the sail region severs all four
        // cells across the spar, while the embedded lower section stays put.
        float[,] mast=new float[148,6]; bool[] cut=new bool[148];
        for(int y=0;y<37;y++)for(int x=0;x<2;x++)for(int z=0;z<2;z++)
        {
            int i=y*4+x*2+z;
            mast[i,0]=(x-0.5f)*0.15f; mast[i,1]=-0.5f+(y+0.5f)*9.2f/37f;
            mast[i,2]=1.6f+(z-0.5f)*0.15f;
            mast[i,3]=mast[i,5]=0.075f; mast[i,4]=9.2f/74f;
            cut[i]=Math.Abs(mast[i,1]-5f)<0.4f;
        }
        supported=NavalRigMath.Supported(mast,cut,0.825f);
        Check(supported[0]&&!supported[147],"High cut across actual mast disconnects top");
        float split=NavalRigMath.BreakHeight(mast,cut,supported);
        Check(split>4.6f&&split<5.4f,"Cloth splits at the mast break, not at its lower mounting");
        Check(3.825f<split&&7.575f>split,"Lower cloth stays; upper cloth falls");
        float speed=0f;
        for(int i=0;i<20;i++)speed=NavalRigMath.StepSpeed(speed,1f,1f,0.05f);
        Check(speed>0.9f&&speed<1.2f,"One second of acceleration is gradual");
        speed=4f;
        for(int i=0;i<20;i++)speed=NavalRigMath.StepSpeed(speed,0f,1f,0.05f);
        Check(speed>3.4f&&speed<3.6f,"Releasing throttle preserves momentum");
        float coast=NavalRigMath.StepSpeed(4f,0f,1f,1f);
        Check(NavalRigMath.StepSpeed(4f,-1f,1f,1f)<coast,"Reverse thrust brakes faster than coasting");
        Check(NavalRigMath.StepSpeed(4f,-1f,1f,0.05f)>0f,"Reverse command does not instantly reverse motion");
        Check(Math.Abs(NavalRigMath.StepSpeed(0f,1f,1f,1f)-NavalRigMath.StepSpeed(NavalRigMath.StepSpeed(0f,1f,1f,0.5f),1f,1f,0.5f))<0.00001f,"Acceleration independent of frame subdivision");
        Check(NavalRigMath.StepSpeed(0f,0f,0f,1f)==0f,"Rest without sail drive stays at rest");
        Console.WriteLine("Rig support and sail power checks passed.");
    }
}
