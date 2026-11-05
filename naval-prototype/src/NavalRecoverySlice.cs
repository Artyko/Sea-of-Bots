using System.Diagnostics;
public static class NavalRecoverySlice
{
    private static int usedFrame=-1;
    public static bool Take(int frame){if(frame==usedFrame)return false;usedFrame=frame;return true;}
    public static bool Expired(long start){return (Stopwatch.GetTimestamp()-start)/(double)Stopwatch.Frequency>=.002;}
}
