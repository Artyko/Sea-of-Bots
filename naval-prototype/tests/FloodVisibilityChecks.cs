using System;
class FloodVisibilityChecks
{
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
    static void Main()
    {
        Check(NavalFloodVisibilityMath.Visibility(2f)==1f,"Visible flood while hull is above water");
        Check(Math.Abs(NavalFloodVisibilityMath.Visibility(0.4f)-0.5f)<0.00001f,"Smooth fade near complete submergence");
        Check(NavalFloodVisibilityMath.Visibility(0f)==0f,"No flood blocks after highest hull point submerges");
        Check(NavalFloodVisibilityMath.Visibility(-10f)==0f,"Deep wreck has no visible flood blocks");
        Check(NavalFloodVisibilityMath.Visibility(0.9f)==1f,"Visibility restores when hull emerges");
        Check(NavalFloodVisibilityMath.HideEffects(-1f),"Flood cells and inlet streams share submerged suppression");
        Check(NavalFloodVisibilityMath.HideEffects(0f),"Suppression starts at complete submergence");
        Check(!NavalFloodVisibilityMath.HideEffects(0.1f),"Effects can return when hull emerges");
        Console.WriteLine("PASS: above-water flood, smooth fade, submerged wreck and restoration.");
    }
}
