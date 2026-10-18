using System;
using System.Collections.Generic;
using UnityEngine;
[DefaultExecutionOrder(400)]
public class NavalDrowning : MonoBehaviour
{
    private FirstPersonMover player;
    private Func<Vector3,float> waterLevel;
    private float scanAt;
    private readonly Dictionary<Character,NavalBreathClock> breaths=new Dictionary<Character,NavalBreathClock>();
    private readonly List<Character> removed=new List<Character>();
    public static Vector3 HeadTop(Character robot)
    {
        FirstPersonMover mover=robot as FirstPersonMover;
        Transform head=mover!=null?mover.GetHeadTransform():null;
        return HeadTop(robot,head);
    }
    private static Vector3 HeadTop(Character robot,Transform head)
    {
        return (head!=null?head.position:robot.transform.position+Vector3.up*.8f)+Vector3.up*(.3f*Mathf.Abs(robot.transform.lossyScale.y));
    }
    public float AirRemaining(Character robot)
    {NavalBreathClock clock;return breaths.TryGetValue(robot,out clock)?clock.Air:NavalBreathClock.Capacity;}
    public void Initialize(FirstPersonMover owner,Func<Vector3,float> level)
    {player=owner;waterLevel=level;}
    private void LateUpdate()
    {
        if(NavalTestWorld.Active==null||!NavalTestWorld.Active.SimulationActive)return;
        if(Time.time>=scanAt)
        {
            scanAt=Time.time+0.5f;
            foreach(Character robot in NavalCharacters.Snapshot())
                if(robot!=null&&robot.IsAlive()&&!breaths.ContainsKey(robot))breaths.Add(robot,new NavalBreathClock());
        }
        removed.Clear();
        foreach(KeyValuePair<Character,NavalBreathClock> entry in breaths)
        {
            Character robot=entry.Key;
            if(robot==null||!robot.IsAlive()){removed.Add(robot);continue;}
            FirstPersonMover mover=robot as FirstPersonMover;
            Transform head=mover!=null?mover.GetHeadTransform():null;
            if(head==null)continue;
            // Measure the top of the robot's head, independently of third-person camera position.
            Vector3 top=HeadTop(robot,head);
            if(entry.Value.Step(waterLevel(top)-top.y,Time.deltaTime))
            {robot.Kill(null,DamageSourceType.ScriptedKill,-1);removed.Add(robot);}
        }
        foreach(Character robot in removed)breaths.Remove(robot);
    }
    private void OnGUI()
    {
        NavalBreathClock breath;
        if(player==null||!player.IsAlive()||!player.IsPlayerInputEnabled()||!breaths.TryGetValue(player,out breath)||
            !breath.Submerged&&breath.Air>=NavalBreathClock.Capacity)return;
        Color original=GUI.color;
        if(breath.Air<=10f)GUI.color=new Color(1f,0.45f,0.35f);
        GUI.Box(new Rect(Screen.width*0.5f-180f,Screen.height-165f,360f,30f),
            "AIR: "+Mathf.CeilToInt(breath.Air)+" s | Surface to breathe");
        GUI.color=original;
    }
}
