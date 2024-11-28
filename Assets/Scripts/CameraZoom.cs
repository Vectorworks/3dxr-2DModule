using NUnit.Framework.Constraints;
using System;
using UnityEngine;

public class CameraZoom : MonoBehaviour {

    private float orthographicSize;
    private Camera cam;
    private Transform cameraTransform;

    private float zoomLevel = 1f;
    private const float ZOOM_FACTOR = 0.1f;

    private const float ZOOM_END_DELTA = 0.1f;
    private float previousZoomTime = -1f;

    private RaycastHit zoomCenterHit;

    public event Action ZoomComplete;
    public event Action<float> ZoomLevelChanged;

    public void Start() {
        this.cam = Camera.main;
        this.orthographicSize = cam.orthographicSize;

        cameraTransform = cam.transform;
    }

    public void Update() {
        bool hasHit = Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out zoomCenterHit);

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f) {
            zoomLevel = Mathf.Max(1f, zoomLevel + ZOOM_FACTOR * Mathf.Sign(scroll));
            Zoom(zoomLevel);
            ZoomLevelChanged?.Invoke(zoomLevel);

            previousZoomTime = Time.time;
        } else {
            if (previousZoomTime > 0f && Time.time - previousZoomTime > ZOOM_END_DELTA) {
                previousZoomTime = -1f;
                ZoomComplete?.Invoke();
            }
        }
    }

    public void Zoom(float zoomLevel) {
        Vector3 mouseWorldPosBeforeZoom = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, cam.nearClipPlane));
        cam.orthographicSize = orthographicSize / zoomLevel;
        Vector3 mouseWorldPosAfterZoom = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, cam.nearClipPlane));
        Vector3 offset = mouseWorldPosBeforeZoom - mouseWorldPosAfterZoom;
        cam.transform.position += offset;
    }

    public void Reset() {
        cam.orthographicSize = 1;
        zoomLevel = 1f;
    }

}
