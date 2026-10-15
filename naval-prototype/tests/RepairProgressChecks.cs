using System;
class RepairProgressChecks
{
    static void Check(bool value,string name){if(!value)throw new Exception(name);}
    static void Main()
    {
        NavalRepairProgress task=new NavalRepairProgress();
        task.Step(10f,false,true);Check(task.Elapsed==0f,"Travel does not count as repair");
        for(int i=0;i<60;i++)task.Step(0.05f,true,true);
        Check(task.Elapsed<4f,"Three seconds do not instantly close the leak");
        task.Step(0.5f,true,false);Check(task.Elapsed<3.01f&&task.DrySeconds==0.5f,"Dry opening does not advance repair");
        task.Step(1.1f,true,true);Check(task.Elapsed>=4f&&task.DrySeconds==0f,"Four active work seconds complete repair");
        task.Reset();Check(task.Elapsed==0f&&task.DrySeconds==0f,"Death or cancellation resets work");
        task.Step(2.1f,false,false);Check(task.DrySeconds>2f,"Dry leak is abandoned after grace period");
        Console.WriteLine("PASS: repair delay, travel exclusion, dry leaks, wave grace and cancellation.");
    }
}
