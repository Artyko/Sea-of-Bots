using System;
public sealed class NavalSailControlMath
{
    public float Target {get;private set;}
    public float Setting {get;private set;}
    public bool Manual {get;private set;}
    public void Adjust(float input,float dt){Manual=true;Target=Math.Max(-1,Math.Min(1,Target+input*Math.Max(0,dt)*.4f));}
    public void Auto(){Manual=false;}
    public float Step(float dt,float command,bool automated,bool mayOperate=true,bool rescueStop=false)
    {
        if(!mayOperate)return Setting;
        if(automated&&!Manual&&!rescueStop)Target=Math.Max(-1,Math.Min(1,command));
        float step=Math.Max(0,dt)*.3f;
        float goal=rescueStop?0:Target;
        Setting=Setting<goal?Math.Min(goal,Setting+step):Math.Max(goal,Setting-step);return Setting;
    }
    public static float ClothY(float original,float top,float deployed){return top+(original-top)*Math.Max(.06f,Math.Min(1,Math.Abs(deployed)));}
}
