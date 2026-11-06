using System;
using System.Text;
using System.Collections.Generic;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;

public sealed class NavalNativeFrameProfile : IDisposable
{
    private static readonly string[] Names =
    {
        "Main Thread", "Render Thread", "Physics.Simulate", "Animator.Update",
        "Camera.Render", "Gfx.WaitForPresentOnGfxThread"
    };
    private readonly ProfilerRecorder[] recorders=new ProfilerRecorder[Names.Length];
    private readonly double[] totals=new double[Names.Length];
    private readonly int[] samples=new int[Names.Length];

    public NavalNativeFrameProfile()
    {
        var available=new List<ProfilerRecorderHandle>();
        ProfilerRecorderHandle.GetAvailable(available);
        foreach(var handle in available)
        {
            var description=ProfilerRecorderHandle.GetDescription(handle);
            int index=Array.IndexOf(Names,description.Name);
            if(index<0||recorders[index].Valid)continue;
            try {recorders[index]=ProfilerRecorder.StartNew(description.Category,description.Name,1);}
            catch(ArgumentException) {}
            catch(InvalidOperationException) {}
        }
    }

    public void Capture()
    {
        for(int i=0;i<recorders.Length;i++)
        {
            var recorder=recorders[i];
            if(!recorder.Valid||recorder.Count==0)continue;
            totals[i]+=recorder.LastValue;
            samples[i]++;
        }
    }

    public string Report()
    {
        var output=new StringBuilder();
        for(int i=0;i<Names.Length;i++)
        {
            if(i>0)output.Append(" | ");
            output.Append(Names[i]).Append('=');
            output.Append(samples[i]>0?(totals[i]/samples[i]/1000000d).ToString("F2"):"n/a");
            totals[i]=0;
            samples[i]=0;
        }
        return output.ToString();
    }

    public void Dispose()
    {
        for(int i=0;i<recorders.Length;i++)if(recorders[i].Valid)recorders[i].Dispose();
    }
}
