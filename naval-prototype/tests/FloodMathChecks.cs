using System;
public static class FloodMathChecks
{
    public static void Main()
    {
        Check(FloodMath.Flow(0f, 1f, 0f, -1f) == 0f, "sealed shell leaks");
        Check(FloodMath.Flow(1f, 1f, 1f, -1f) == 0f, "equal levels leak");
        Check(FloodMath.Flow(1f, -1f, -2f, 0f) == 0f, "dry hole admits water");
        float wet;
        Check(FloodMath.OpeningFlow(0.0625f, 0f, -1f, 0.3f, 0.125f, out wet) == 0f && wet == 0f, "opening entirely above sea leaks");
        float waterline = FloodMath.OpeningFlow(0.0625f, 0f, -1f, 0f, 0.125f, out wet);
        Check(waterline > 0f && Math.Abs(wet - 0.03125f) < 0.00001f, "waterline opening is not partially flooded");
        Check(FloodMath.OpeningFlow(0.0625f, 0f, -1f, -0.3f, 0.125f, out wet) > waterline, "deeper opening does not increase flow");
        float small = FloodMath.Flow(0.0625f, 0f, -0.4f, -0.5f);
        float large = FloodMath.Flow(0.125f, 0f, -0.4f, -0.5f);
        Check(small > 0f && Math.Abs(large - 2f * small) < 0.00001f, "hole area does not scale flow");
        Check(FloodMath.Flow(1f, -1f, 0f, -0.5f) < 0f, "submerged interior cannot drain");
        // Fixed sea, tank area 1: filling must approach equilibrium without unbounded growth.
        float volume = 0f;
        for (int i = 0; i < 30000; i++)
            volume += FloodMath.Flow(0.02f, 0f, -0.5f + volume, -0.5f) * 0.01f;
        Check(Math.Abs(volume - 0.5f) < 0.001f, "tank does not equilibrate");
        Console.WriteLine("PASS: sealed/dry holes, equal levels, opening size, outward flow and tank equilibrium.");
    }
    private static void Check(bool value, string message)
    { if (!value) throw new Exception(message); }
}
