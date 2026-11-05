using UnityEngine;

public partial class NavalTestWorld
{
    private Transform helmCamera;
    private bool helmOrbitActive;
    private float helmOrbitYaw,helmOrbitPitch;
    private Vector3 helmCameraLocalPosition;
    private Quaternion helmCameraLocalRotation;

    private void RestoreHelmCamera()
    {
        if (helmCamera != null)
        {
            helmCamera.localPosition = helmCameraLocalPosition;
            helmCamera.localRotation = helmCameraLocalRotation;
        }
        helmCamera = null;
    }

    private void UpdateHelmCamera()
    {
        if (Spectating || !stations.AtHelm || hull.Sunk || liveCamera == null)
        {
            helmOrbitActive=false;
            return;
        }
        if (!helmOrbitActive)
        {
            helmOrbitActive=true;
            helmOrbitYaw=0f;
            helmOrbitPitch=52f;
        }
        if (!ShipMenuOpen)
        {
            helmOrbitYaw=Mathf.Repeat(helmOrbitYaw+Input.GetAxisRaw("Mouse X")*4f,360f);
            helmOrbitPitch=Mathf.Clamp(helmOrbitPitch-Input.GetAxisRaw("Mouse Y")*4f,12f,82f);
        }
        helmCamera = liveCamera.transform;
        helmCameraLocalPosition = helmCamera.localPosition;
        helmCameraLocalRotation = helmCamera.localRotation;
        Vector3 focus = boat.position + Vector3.up * (hull.Definition.WeatherDeck * hull.WorldScale + 1.5f);
        float vertical = liveCamera.fieldOfView * Mathf.Deg2Rad * .5f;
        float horizontal = Mathf.Atan(Mathf.Tan(vertical) * liveCamera.aspect);
        float halfAngle = Mathf.Clamp(Mathf.Min(vertical, horizontal), .25f, 1.2f);
        float radius = Mathf.Max(6f, hull.Definition.HullLength * .57f);
        Vector3 forward = Vector3.ProjectOnPlane(boat.forward, Vector3.up).normalized;
        Vector3 orbitBack=Quaternion.AngleAxis(helmOrbitYaw,Vector3.up)*-forward;
        float pitch=helmOrbitPitch*Mathf.Deg2Rad;
        Vector3 offset=(orbitBack*Mathf.Cos(pitch)+Vector3.up*Mathf.Sin(pitch))*(radius/Mathf.Sin(halfAngle));
        helmCamera.position = focus + offset;
        helmCamera.rotation = Quaternion.LookRotation(-offset, Vector3.up);
    }
}
