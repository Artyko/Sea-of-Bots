using UnityEngine;

public partial class NavalTestWorld
{
    private GUIStyle compassLabel;
    private void DrawCompass()
    {
        var camera=ViewCamera;
        if(camera==null||!SimulationActive)return;
        if(compassLabel==null)compassLabel=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter};
        float bearing=Mathf.Repeat(camera.transform.eulerAngles.y,360f);
        float width=Mathf.Min(560f,Screen.width-24f),x=(Screen.width-width)*.5f,y=8f;
        float pixels=width/130f;
        GUI.Box(new Rect(x,y,width,44f),"");
        GUI.BeginGroup(new Rect(x,y,width,44f));
        for(int degree=-360;degree<=720;degree+=10)
        {
            float delta=degree-bearing;
            if(Mathf.Abs(delta)>66f)continue;
            float at=width*.5f+delta*pixels;
            GUI.DrawTexture(new Rect(at,27f,1f,degree%30==0?12f:6f),Texture2D.whiteTexture);
            if(degree%30==0)
            {
                int normalized=((degree%360)+360)%360;
                string text=normalized==0?"С":normalized==90?"В":normalized==180?"Ю":normalized==270?"З":normalized.ToString();
                GUI.Label(new Rect(at-20f,0f,40f,24f),text,compassLabel);
            }
        }
        GUI.Label(new Rect(width*.5f-18f,24f,36f,20f),"▼",compassLabel);
        if(ConvoyMission)
        {
            Vector3 destination=new Vector3(waterCenter.x,boat.position.y,convoyFinishZ);
            Vector3 toExit=destination-camera.transform.position;
            float exitBearing=Mathf.Atan2(toExit.x,toExit.z)*Mathf.Rad2Deg;
            float delta=Mathf.DeltaAngle(bearing,exitBearing);
            float at=width*.5f+Mathf.Clamp(delta,-57f,57f)*pixels;
            Color saved=GUI.color;GUI.color=new Color(1f,.8f,.3f);
            GUI.Label(new Rect(at-34f,15f,68f,24f),(delta<-57f?"← ":delta>57f?"→ ":"")+"Выход",compassLabel);
            GUI.color=saved;
        }
        GUI.EndGroup();
        GUI.Label(new Rect(x+width+3f,y+7f,55f,25f),Mathf.RoundToInt(bearing)+"°",compassLabel);
    }
}
