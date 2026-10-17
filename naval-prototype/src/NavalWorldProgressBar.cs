using UnityEngine;
using System.Collections.Generic;

// Shared subdued world-anchored indicator for guns, pumps and future equipment.
public static class NavalWorldProgressBar
{
    private static GUIStyle label;
    private struct Sight {public Vector3 Eye,Anchor;public Quaternion Rotation;public float Until;public int Frame,Revision;public bool Blocked;}
    private static readonly Dictionary<int,Sight> sights=new Dictionary<int,Sight>();
    public static void Reset(){sights.Clear();}
    private static bool Occluded(Camera camera,Vector3 anchor,int key)
    {
        Vector3 eye=camera.transform.position;
        int revision=NavalTestWorld.Active!=null&&NavalTestWorld.Active.PlayerHull!=null?NavalTestWorld.Active.PlayerHull.Revision:0;
        Sight sight;
        if(key!=0&&sights.TryGetValue(key,out sight)&&Time.time<sight.Until&&sight.Frame<=Time.frameCount&&sight.Revision==revision&&
            (eye-sight.Eye).sqrMagnitude<.04f&&(anchor-sight.Anchor).sqrMagnitude<.04f&&Quaternion.Angle(camera.transform.rotation,sight.Rotation)<2f)return sight.Blocked;
        Vector3 delta=anchor-eye;
        bool blocked=Physics.Raycast(eye,delta.normalized,Mathf.Max(0f,delta.magnitude-.2f),PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore);
        if(key!=0)
        {
            if(sights.Count>128)sights.Clear();
            sights[key]=new Sight{Eye=eye,Anchor=anchor,Rotation=camera.transform.rotation,Until=Time.time+.1f,Frame=Time.frameCount,Revision=revision,Blocked=blocked};
        }
        return blocked;
    }
    public static void Draw(Camera camera,Vector3 anchor,float progress,bool functional,bool ready,string text,int key=0)
    {
        if(Event.current.type!=EventType.Repaint||camera==null)return;
        float distance=Vector3.Distance(camera.transform.position,anchor);if(distance>160f)return;
        Vector3 screen=camera.WorldToScreenPoint(anchor);
        if(screen.z<=0f||screen.x<0f||screen.x>Screen.width||screen.y<0f||screen.y>Screen.height)return;
        if(Occluded(camera,anchor,key))return;
        if(label==null){label=new GUIStyle(GUI.skin.label);label.alignment=TextAnchor.MiddleCenter;label.fontSize=9;label.normal.textColor=new Color(.76f,.75f,.7f,.85f);}
        Color previous=GUI.color;
        float width=Mathf.Clamp(60f-distance*.2f,32f,56f),x=screen.x-width*.5f,y=Screen.height-screen.y;
        GUI.color=new Color(.08f,.075f,.06f,.55f);GUI.DrawTexture(new Rect(x-1f,y-1f,width+2f,6f),Texture2D.whiteTexture);
        GUI.color=new Color(.2f,.19f,.16f,.55f);GUI.DrawTexture(new Rect(x,y,width,4f),Texture2D.whiteTexture);
        GUI.color=!functional?new Color(.55f,.39f,.35f,.7f):ready?new Color(.47f,.57f,.43f,.7f):new Color(.61f,.55f,.4f,.7f);
        GUI.DrawTexture(new Rect(x,y,width*Mathf.Clamp01(progress),4f),Texture2D.whiteTexture);
        GUI.color=Color.white;GUI.Label(new Rect(x-20f,y-16f,width+40f,15f),text,label);GUI.color=previous;
    }
}
