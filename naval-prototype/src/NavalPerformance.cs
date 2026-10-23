using System.Diagnostics;
using UnityEngine;

public enum NavalPerfArea {Sail,RigSupport,Ocean,HullDamage,Chunks,Structure,Crew,Gunnery,PhysicsSync,Flood,Effects,RigRendering,RigContact,HullPose,SupportQuery,ColliderChunks,Shore,ShoreNavigation,ShoreMotion,Count}
// Opt-in CPU sampling. Inclusive scopes may overlap; these numbers must not be summed.
public static class NavalPerformance
{
    public static bool Enabled;
    private static bool autoSample;
    private static int autoFrames;
    private static readonly long[] autoTotals=new long[(int)NavalPerfArea.Count],autoMaxima=new long[(int)NavalPerfArea.Count];
    public static void AutoFrame(bool active){autoSample=active&&!Enabled&&Time.frameCount%60==0;if(autoSample)autoFrames++;}
    public static string AutoReport()
    {
        var report=new System.Text.StringBuilder();report.Append("sampled=").Append(autoFrames);
        if(autoFrames>0){double factor=1000.0/Stopwatch.Frequency;
            for(int i=0;i<autoTotals.Length;i++)if(autoTotals[i]>0)report.Append(" | ").Append((NavalPerfArea)i).Append("=").Append((autoTotals[i]*factor/autoFrames).ToString("F2")).Append("/").Append((autoMaxima[i]*factor).ToString("F2"));
        }
        autoFrames=0;System.Array.Clear(autoTotals,0,autoTotals.Length);System.Array.Clear(autoMaxima,0,autoMaxima.Length);return report.ToString();
    }
    private static readonly long[] totals=new long[(int)NavalPerfArea.Count],maxima=new long[(int)NavalPerfArea.Count];
    private static readonly int[] calls=new int[(int)NavalPerfArea.Count];
    private static readonly float[] average=new float[(int)NavalPerfArea.Count],peak=new float[(int)NavalPerfArea.Count];
    private static readonly string[] names={"Паруса","Связи такелажа","Океан","Урон корпусу","Меши чанков","Связи корпуса","Экипаж","Наведение","Physics sync","Затопление","Дым","Меши такелажа","Таран такелажа","Поза корпуса","Опора игрока","Коллайдеры чанков","Берег / навигация","Берег: поиск курса","Берег: перемещение"};
    private static float since;
    private static int startFrame;
    public struct Scope:System.IDisposable
    {
        private readonly long start;
        private readonly int area;
        private readonly bool automatic;
        public Scope(NavalPerfArea value){area=(int)value;automatic=!Enabled&&autoSample;start=Enabled||automatic?Stopwatch.GetTimestamp():0;}
        public void Dispose(){if(start==0)return;long ticks=Stopwatch.GetTimestamp()-start;
            if(automatic){autoTotals[area]+=ticks;if(ticks>autoMaxima[area])autoMaxima[area]=ticks;}
            else{totals[area]+=ticks;calls[area]++;if(ticks>maxima[area])maxima[area]=ticks;}
        }
    }
    public static Scope Measure(NavalPerfArea area){return new Scope(area);}
    public static void Toggle()
    {
        Enabled=!Enabled;since=Time.realtimeSinceStartup;startFrame=Time.frameCount;
        NavalTestWorld world=NavalTestWorld.Active;
        if(Enabled&&world!=null)
        {
            if(world.PlayerHull!=null&&world.PlayerHull.PhysicsBody!=null)world.PlayerHull.PhysicsBody.RequestAudit();
            NavalShipPhysics enemy=world.EnemyBoat!=null?world.EnemyBoat.GetComponent<NavalShipPhysics>():null;
            if(enemy!=null)enemy.RequestAudit();
        }
        System.Array.Clear(totals,0,totals.Length);System.Array.Clear(maxima,0,maxima.Length);System.Array.Clear(calls,0,calls.Length);
        System.Array.Clear(average,0,average.Length);System.Array.Clear(peak,0,peak.Length);
    }
    public static void SyncTransforms(){NavalPhysicsQueries.Invalidate();NavalHullColliders.FlushAll();using(Measure(NavalPerfArea.PhysicsSync))Physics.SyncTransforms();}
    public static void Draw()
    {
        if(!Enabled||Event.current.type!=EventType.Repaint)return;
        if(Time.realtimeSinceStartup-since>=1f)
        {
            int frames=Mathf.Max(1,Time.frameCount-startFrame);double toMs=1000.0/Stopwatch.Frequency;
            for(int i=0;i<totals.Length;i++){average[i]=(float)(totals[i]*toMs/frames);peak[i]=(float)(maxima[i]*toMs);totals[i]=maxima[i]=0;calls[i]=0;}
            since=Time.realtimeSinceStartup;startFrame=Time.frameCount;
        }
        float extra=Mathf.Max(0,names.Length-16)*21f;
        float x=Mathf.Max(5f,Screen.width-325f),y=Mathf.Max(5f,Screen.height-449f-extra);
        GUI.Box(new Rect(x,y,320f,444f+extra),"F3: CPU — среднее/кадр | max вызов");
        for(int i=0;i<names.Length;i++)GUI.Label(new Rect(x+8f,y+24f+i*21f,306f,21f),names[i]+": "+average[i].ToString("F2")+" | "+peak[i].ToString("F2")+" мс");
        NavalTestWorld world=NavalTestWorld.Active;
        if(world!=null)
        {
            NavalHull own=world.PlayerHull;
            NavalShipPhysics enemy=world.EnemyBoat!=null?world.EnemyBoat.GetComponent<NavalShipPhysics>():null;
            if(own!=null&&own.PhysicsBody!=null)GUI.Label(new Rect(x+8f,y+363f+extra,306f,20f),"Свой: "+own.PhysicsBody.Diagnostics);
            if(enemy!=null)GUI.Label(new Rect(x+8f,y+383f+extra,306f,20f),"Враг: "+enemy.Diagnostics);
        }
        GUI.Label(new Rect(x+8f,y+403f+extra,306f,37f),"Вложенные замеры; не складывать.\nGPU и время самой игры не измеряются.");
    }
}
