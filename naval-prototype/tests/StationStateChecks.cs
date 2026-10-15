using System;
public static class StationStateChecks
{
    public static void Main()
    {
        NavalStationState state = new NavalStationState();
        state.PressInteract(-1, false); Check(state.Active == -1, "E remotely enters helm");
        state.PressInteract(0, false); Check(state.Active == 0, "helm cannot activate");
        state.PressInteract(1, false); Check(state.Active == -1, "E fails to leave current post");
        state.PressInteract(1, false); Check(state.Active == 1, "port cannot activate");
        state.Exit(); state.PressInteract(2, false); Check(state.Active == 2, "starboard cannot activate");
        state.PressInteract(2, true); Check(state.Active == -1, "disabled state retains control");
        state.PressInteract(0, true); Check(state.Active == -1, "swimmer/sunk ship can enter helm");
        state.PressInteract(20, false); Check(state.Active == -1, "invalid station accepted");
        Console.WriteLine("PASS: proximity selection, E entry/exit, two cannons, disabled control and invalid station.");
    }
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
}
