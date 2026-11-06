using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine.LowLevel;

public sealed class NavalLoopFrameProfile : IDisposable
{
    private sealed class TimingMarker {}
    private static readonly string[] Names =
    {
        "PhysicsFixedUpdate", "PhysicsUpdate", "DirectorUpdateAnimationBegin",
        "ScriptRunBehaviourUpdate", "ScriptRunBehaviourLateUpdate",
        "UpdateAllRenderers", "FinishFrameRendering"
    };
    private readonly long[] starts=new long[Names.Length];
    private readonly long[] elapsed=new long[Names.Length];
    private readonly int[] calls=new int[Names.Length];
    private int frames;

    public NavalLoopFrameProfile()
    {
        PlayerLoopSystem loop=PlayerLoop.GetCurrentPlayerLoop();
        AddTimings(ref loop);
        PlayerLoop.SetPlayerLoop(loop);
    }

    private PlayerLoopSystem Marker(int slot,bool begin)
    {
        return new PlayerLoopSystem
        {
            type=typeof(TimingMarker),
            updateDelegate=delegate
            {
                long now=Stopwatch.GetTimestamp();
                if(begin)starts[slot]=now;
                else {elapsed[slot]+=now-starts[slot];calls[slot]++;}
            }
        };
    }

    private void AddTimings(ref PlayerLoopSystem parent)
    {
        var children=parent.subSystemList;
        if(children==null)return;
        var result=new List<PlayerLoopSystem>(children.Length+4);
        bool animationSpan=false;
        for(int i=0;i<children.Length;i++)
        {
            var child=children[i];
            AddTimings(ref child);
            string name=child.type!=null?child.type.Name:"";
            int slot=Array.IndexOf(Names,name);
            if(slot>=0)result.Add(Marker(slot,true));
            result.Add(child);
            if(slot==2)animationSpan=true;
            else if(slot>=0)result.Add(Marker(slot,false));
            if(animationSpan&&name=="DirectorUpdateAnimationEnd")
            {
                result.Add(Marker(2,false));
                animationSpan=false;
            }
        }
        if(animationSpan)result.Add(Marker(2,false));
        parent.subSystemList=result.ToArray();
    }

    public void CaptureFrame(){frames++;}

    public string Report()
    {
        var text=new StringBuilder();
        double scale=1000d/Stopwatch.Frequency/Math.Max(1,frames);
        for(int i=0;i<Names.Length;i++)
        {
            if(i>0)text.Append(" | ");
            text.Append(i==2?"AnimationSpan":Names[i]).Append('=');
            text.Append(calls[i]>0?(elapsed[i]*scale).ToString("F2"):"n/a");
            elapsed[i]=0;
            calls[i]=0;
        }
        frames=0;
        return text.ToString();
    }

    private static void RemoveTimings(ref PlayerLoopSystem parent)
    {
        var children=parent.subSystemList;
        if(children==null)return;
        var result=new List<PlayerLoopSystem>(children.Length);
        foreach(var node in children)
        {
            if(node.type==typeof(TimingMarker))continue;
            var child=node;
            RemoveTimings(ref child);
            result.Add(child);
        }
        parent.subSystemList=result.ToArray();
    }

    public void Dispose()
    {
        var loop=PlayerLoop.GetCurrentPlayerLoop();
        RemoveTimings(ref loop);
        PlayerLoop.SetPlayerLoop(loop);
    }
}
