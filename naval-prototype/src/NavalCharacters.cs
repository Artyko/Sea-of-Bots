using System.Collections.Generic;
using UnityEngine;

public static class NavalCharacters
{
    private static readonly HashSet<Character> known=new HashSet<Character>();
    private static readonly List<Character> live=new List<Character>();
    private static Character[] snapshot=new Character[0];
    private static bool dirty=true,seeded;
    private static float scanAt=float.NegativeInfinity;
    public static void Reset(){known.Clear();live.Clear();snapshot=new Character[0];dirty=true;seeded=false;scanAt=float.NegativeInfinity;}
    public static void Register(Character robot){if(robot!=null&&known.Add(robot))dirty=true;}
    public static void Remove(Character robot){if(!object.ReferenceEquals(robot,null)&&known.Remove(robot))dirty=true;}
    public static Character[] Snapshot()
    {
        if(Time.time>=scanAt)
        {
            scanAt=Time.time+.25f;
            var world=NavalTestWorld.Active;
            // Convoy crews register when initialized; seed native scene occupants once.
            // Other arena modes can still introduce native waves during the session.
            if(!seeded||world==null||!world.ConvoyMission)
            {
                using(NavalPerformance.Measure(NavalPerfArea.CharacterDiscovery))
                    foreach(Character robot in Object.FindObjectsByType<Character>(FindObjectsSortMode.None))Register(robot);
                seeded=true;
            }
            foreach(Character robot in known)if(robot==null||!robot.IsAlive()){dirty=true;break;}
        }
        if(dirty)
        {
            live.Clear();foreach(Character robot in known)if(robot!=null&&robot.IsAlive())live.Add(robot);
            known.Clear();foreach(Character robot in live)known.Add(robot);
            snapshot=live.ToArray();dirty=false;
        }
        return snapshot;
    }
}
