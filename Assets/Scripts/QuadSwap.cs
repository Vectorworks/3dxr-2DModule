using System;
using UnityEngine;
using UnityEngine.Rendering;



public class QuadSwap : MonoBehaviour {
    [SerializeField] Transform quadLowRes;
    [SerializeField] Transform quadHighRes;

    private Camera cam;
    private Transform cameraTransform;
    private float camWidth;
    private float camHeight;

    private const float TOP_HEIGHT = 0.01f;
    private const float BOTTOM_HEIGHT = -0.01f;

    private Vector2 intersectionCenter;
    private Vector2 intersectionSize;

    private float currentZoom = 1f;

    [SerializeField] private CameraPan cameraPan;
    [SerializeField] private CameraZoom cameraZoom;

    public event Action<Vector2> QuadSizeInPixelsChanged;

    private void OnEnable() {
        cameraZoom.ZoomLevelChanged += OnZoomLevelChanged;
        cameraPan.TranslationOngoing += OnTranslationDelta;
        cameraPan.TranslationComplete += OnTranslationComplete;
    }



    private void OnDisable() {
        cameraZoom.ZoomLevelChanged -= OnZoomLevelChanged;
        cameraPan.TranslationOngoing -= OnTranslationDelta;
        cameraPan.TranslationComplete -= OnTranslationComplete;
    }

    private void Start() {
        cam = Camera.main;
        cameraTransform = cam.transform;


        Debug.Log($"Cam width: {camWidth}, cam height: {camHeight}");
    }

    public void OnTranslationDelta() {
        GetCurrentCameraParameters();
        ResizeHighResQuad();
    }

    public void OnTranslationComplete() {
        GetCurrentCameraParameters();
        ResizeHighResQuad();
    }

    public void OnZoomLevelChanged(float zoom) {
        currentZoom = zoom;

        GetCurrentCameraParameters();
        GameObject quadHighResGO = quadHighRes.gameObject;

        if (zoom == 1f) {
            MoveUp(quadLowRes, quadHighRes);
            quadHighResGO.SetActive(false);
        } else { // TODO shouldn't do if prev zoom was also != 1
            if (!quadHighResGO.activeInHierarchy) {
                quadHighResGO.SetActive(true);
            }
            MoveUp(quadHighRes, quadLowRes);
            ResizeHighResQuad();
        }
    }

    private void MoveUp(Transform up, Transform down) {
        up.position = new Vector3(up.position.x, TOP_HEIGHT, up.position.z);
        down.position = new Vector3(down.position.x, BOTTOM_HEIGHT, down.position.z);
    }

    private void ResizeHighResQuad() {
        bool isIntersecting = CalculateQuadCameraIntersection(out Vector2 intersectionCenter, out Vector2 intersectionSize);
        if (isIntersecting) {
            quadHighRes.position = new Vector3(intersectionCenter.x, quadHighRes.position.y, intersectionCenter.y);
            quadHighRes.localScale = new Vector3(intersectionSize.x, intersectionSize.y, 1f);

            // Calculate the size of the intersection in pixels
            Vector2 intersectionSizeInPixels = WorldToScreenSize(intersectionSize, cam.orthographicSize, cam.aspect);
            QuadSizeInPixelsChanged?.Invoke(intersectionSizeInPixels);
        }
    }

    public static Vector2 WorldToScreenSize(Vector2 worldSize, float orthographicSize, float aspect) {
        float screenHeight = Screen.height;
        float screenWidth = Screen.width;

        float orthographicSizeDoubled = 2f * orthographicSize;
        // Calculate the size in pixels
        float pixelHeight = (worldSize.y / orthographicSizeDoubled) * screenHeight;
        float pixelWidth = (worldSize.x / (aspect * orthographicSizeDoubled)) * screenWidth;

        return new Vector2(pixelWidth, pixelHeight);
    }

    public static Vector2 ScreenToWorldSize(Vector2 screenSize, float orthographicSize, float aspect) {
        float screenHeight = Screen.height;
        float screenWidth = Screen.width;

        float orthographicSizeDoubled = 2f * orthographicSize;
        // Calculate the size in world units
        float worldHeight = (screenSize.y / screenHeight) * orthographicSizeDoubled;
        float worldWidth = (screenSize.x / screenWidth) * (aspect * orthographicSizeDoubled);

        return new Vector2(worldWidth, worldHeight);
    }

    private bool CalculateQuadCameraIntersection(out Vector2 intersectionCenter, out Vector2 intersectionSize) {
        Vector2 cameraCenter = new Vector2(cameraTransform.position.x, cameraTransform.position.z);
        Vector2 cameraSize = 2f * new Vector2(camWidth, camHeight);
        Vector2 quadCenter = new Vector2(quadLowRes.position.x, quadLowRes.position.z);
        Vector2 quadSize = new Vector2(quadLowRes.localScale.x, quadLowRes.localScale.y);

        return CalculateQuadIntersection(cameraCenter, cameraSize, quadCenter, quadSize, out intersectionCenter, out intersectionSize);
    }

    public static bool CalculateQuadIntersection(Vector2 center1, Vector2 size1, Vector2 center2, Vector2 size2, out Vector2 intersectionCenter, out Vector2 intersectionSize) {
        // Calculate the half sizes
        Vector2 halfSize1 = size1 / 2;
        Vector2 halfSize2 = size2 / 2;

        // Calculate the min and max points of both quads
        Vector2 min1 = center1 - halfSize1;
        Vector2 max1 = center1 + halfSize1;
        Vector2 min2 = center2 - halfSize2;
        Vector2 max2 = center2 + halfSize2;

        // Calculate the intersection min and max points
        Vector2 intersectionMin = new Vector2(Mathf.Max(min1.x, min2.x), Mathf.Max(min1.y, min2.y));
        Vector2 intersectionMax = new Vector2(Mathf.Min(max1.x, max2.x), Mathf.Min(max1.y, max2.y));

        // Check if there is an intersection
        if (intersectionMin.x < intersectionMax.x && intersectionMin.y < intersectionMax.y) {
            // Calculate the center and size of the intersection
            intersectionCenter = (intersectionMin + intersectionMax) / 2;
            intersectionSize = intersectionMax - intersectionMin;
            return true;
        } else {
            // No intersection
            intersectionCenter = Vector2.zero;
            intersectionSize = Vector2.zero;
            return false;
        }
    }

    private void GetCurrentCameraParameters() {
        camWidth = cam.orthographicSize * cam.aspect;
        camHeight = cam.orthographicSize;
    }

}
