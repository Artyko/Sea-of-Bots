using System;
using UnityEngine;

public class NavalUnderwater : MonoBehaviour
{
    private FirstPersonMover player;
    private Func<Vector3, float> level;
    private NavalWaterCamera effect;
    public void Initialize(FirstPersonMover owner, Func<Vector3, float> waterLevel)
    { player = owner; level = waterLevel; }
    private void LateUpdate()
    {
        NavalTestWorld world=NavalTestWorld.Active;
        Camera camera=world!=null?world.ViewCamera:player!=null?player.GetPlayerCamera():null;
        if (camera == null) return;
        if (effect == null || effect.GetComponent<Camera>() != camera)
        {
            if (effect != null) Destroy(effect);
            effect = camera.gameObject.AddComponent<NavalWaterCamera>();
        }
        Vector3 position = camera.transform.position;
        if(world!=null&&world.Spectating)
        {
            effect.Depth=level(position)-position.y;effect.Underwater=effect.Depth>.04f;return;
        }
        if(player==null)return;
        Transform head=player.GetHeadTransform();
        Vector3 top=head!=null?head.position+Vector3.up*(.3f*Mathf.Abs(player.transform.lossyScale.y)):player.transform.position;
        effect.Depth=NavalSubmersionMath.VisualDepth(level(position)-position.y,level(top)-top.y);
        effect.Underwater = player.IsAlive() && player.IsPlayerInputEnabled() && effect.Depth > 0.04f;
    }
    private void OnDestroy() { if (effect != null) Destroy(effect); }
}

// Fog and clear colour are scoped to this camera render, and restored afterward.
public class NavalWaterCamera : MonoBehaviour
{
    public bool Underwater;
    public float Depth;
    private bool applied;
    private bool previousFog;
    private Color previousFogColor;
    private FogMode previousMode;
    private float previousDensity;
    private Color previousBackground;
    private CameraClearFlags previousFlags;
    private Camera camera;
    private void OnPreCull()
    {
        if (!Underwater) return;
        camera = GetComponent<Camera>();
        previousFog = RenderSettings.fog; previousFogColor = RenderSettings.fogColor;
        previousMode = RenderSettings.fogMode; previousDensity = RenderSettings.fogDensity;
        previousBackground = camera.backgroundColor; previousFlags = camera.clearFlags;
        RenderSettings.fog = true; RenderSettings.fogColor = new Color(0.02f, 0.20f, 0.30f);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = Mathf.Lerp(0.035f, 0.12f, Mathf.Clamp01(Depth / 8f));
        camera.backgroundColor = RenderSettings.fogColor; camera.clearFlags = CameraClearFlags.SolidColor;
        applied = true;
    }
    private void Restore()
    {
        if (!applied) return;
        RenderSettings.fog = previousFog; RenderSettings.fogColor = previousFogColor;
        RenderSettings.fogMode = previousMode; RenderSettings.fogDensity = previousDensity;
        if (camera != null) { camera.backgroundColor = previousBackground; camera.clearFlags = previousFlags; }
        applied = false;
    }
    private void OnPostRender() { Restore(); }
    private void OnDisable() { Restore(); }
    private void OnDestroy() { Restore(); }
    private void OnGUI()
    {
        if (!Underwater) return;
        int previousDepth = GUI.depth; Color previousColor = GUI.color;
        GUI.depth = 1000; GUI.color = new Color(0.05f, 0.35f, 0.60f, 0.32f + Mathf.Clamp01(Depth / 10f) * 0.20f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = previousColor; GUI.depth = previousDepth;
        GUI.Box(new Rect(Screen.width-194f,14f,180f,26f),"ПОД ВОДОЙ | "+Depth.ToString("F1")+" м");
    }
}
