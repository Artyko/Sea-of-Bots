using UnityEngine;
public partial class NavalTestWorld {
 private readonly System.Collections.Generic.Dictionary<Renderer,UnityEngine.Rendering.ShadowCastingMode> binocularHidden=new System.Collections.Generic.Dictionary<Renderer,UnityEngine.Rendering.ShadowCastingMode>();
 private void RestoreBinocularBody(){foreach(var pair in binocularHidden)if(pair.Key!=null)pair.Key.shadowCastingMode=pair.Value;binocularHidden.Clear();}
 private Texture2D binocularMask;
 private Camera binocularCamera;private float binocularFov;private bool binocularHeld;
 private void UpdateBinoculars(){
  Camera view=ViewCamera;
  bool held=!Spectating&&(stations==null||!stations.AtHelm)&&player!=null&&player.IsAlive()&&Input.GetMouseButton(1);
  if(binocularHeld&&(!held||view!=binocularCamera)){
   if(binocularCamera!=null)binocularCamera.fieldOfView=binocularFov;binocularHeld=false;RestoreBinocularBody();
  }
  if(held&&view!=null){
   if(!binocularHeld){binocularCamera=view;binocularFov=view.fieldOfView;binocularHeld=true;
    foreach(Renderer shape in player.GetComponentsInChildren<Renderer>())if(shape!=null){binocularHidden[shape]=shape.shadowCastingMode;shape.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;}
   }
   // Apply independently in LateUpdate; native camera FOV writes cannot restart the interpolation.
  }
 }
 private void ApplyBinocularZoom(){
  if(binocularHeld&&binocularCamera!=null)binocularCamera.fieldOfView=7f;
 }
 private void DrawBinoculars(){
  if(!binocularHeld)return;
  if(binocularMask==null){
   const int w=512,h=256;binocularMask=new Texture2D(w,h,TextureFormat.RGBA32,false);
   Color[] pixels=new Color[w*h];
   for(int y=0;y<h;y++)for(int x=0;x<w;x++){
    float dy=(y-h*.5f)/(h*.46f),dx1=(x-w*.36f)/(h*.46f),dx2=(x-w*.64f)/(h*.46f);
    float d=Mathf.Min(dx1*dx1+dy*dy,dx2*dx2+dy*dy);
    pixels[y*w+x]=new Color(.015f,.015f,.018f,Mathf.Clamp01((d-.94f)/.06f));
   }
   binocularMask.SetPixels(pixels);binocularMask.Apply();
  }
  GUI.depth=-100;GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),binocularMask);
  GUI.Label(new Rect(Screen.width*.5f-60,Screen.height*.88f,180,24),"BINOCULARS");
  GUI.depth=0;
 }
 private void ReleaseBinoculars(){
  if(binocularHeld&&binocularCamera!=null)binocularCamera.fieldOfView=binocularFov;
  RestoreBinocularBody();binocularHeld=false;if(binocularMask!=null)Destroy(binocularMask);
 }
}
