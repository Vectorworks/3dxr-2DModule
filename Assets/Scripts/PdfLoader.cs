using UnityEngine;
using Paroxe.PdfRenderer;
using System.Runtime.CompilerServices;
using System;


public class PdfLoader : MonoBehaviour {
    [SerializeField] private GameObject lowResQuad;
    [SerializeField] private GameObject highResQuad;

    private int currentPageNum = 0;
    private PDFRenderer pdfRenderer;
    private PDFDocument document;
    private PDFPage currentPage;

    private Vector2 prevSize = -Vector2.one;

    [SerializeField] private CameraZoom cameraZoom;
    [SerializeField] private CameraPan cameraPan;
    [SerializeField] private QuadSwap quadSwap;

    private Vector3 previousMousePosition;

    private Transform highResQuadTransform;
    private Transform lowResQuadTransform;

    [SerializeField] private float zoomLevel = 1f;

    [SerializeField] public float scale = 1f;
    private float prevScale = 1f;

    private Texture2D texDefault;
    private Texture2D texHighRes;

    private const int MAGIC_NUM_TODO = 1; // TODO screen resolution dependent
    private const float PAGE_SCALE_ON_SCREEN = 0.95f;

    void Start() {
        pdfRenderer = new PDFRenderer();
        document = new PDFDocument("Assets/StreamingAssets/Introduction_to_the_Universal_Render_Pipeline_for_advanced_Unity_creators_Unity_6_edition.pdf");
        //document = new PDFDocument("Assets/StreamingAssets/uv1.pdf");
        LoadPage();

        highResQuadTransform = highResQuad.transform;
        lowResQuadTransform = lowResQuad.transform;

        Debug.Log($"Num pages: {document.GetPageCount()}");
    }

    private void OnEnable() {
        cameraZoom.ZoomStarted += OnZoomStarted;
        cameraZoom.ZoomLevelChanged += OnZoomLevelChanged;
        cameraZoom.ZoomComplete += OnZoomComplete;
        cameraPan.TranslationOngoing += OnTranslationOngoing;
        cameraPan.TranslationComplete += OnTranslationComplete;
        quadSwap.QuadSizeInPixelsChanged += OnQuadSizeInPixelsChanged;
    }

    private void OnDisable() {
        cameraZoom.ZoomStarted -= OnZoomStarted;
        cameraZoom.ZoomLevelChanged -= OnZoomLevelChanged;
        cameraZoom.ZoomComplete -= OnZoomComplete;
        cameraPan.TranslationOngoing -= OnTranslationOngoing;
        cameraPan.TranslationComplete -= OnTranslationComplete;
        quadSwap.QuadSizeInPixelsChanged -= OnQuadSizeInPixelsChanged;
    }

    // Update is called once per frame
    void Update() {
        if (Input.GetKeyDown("[")) {
            currentPageNum++;
            ResetState();
            LoadPage();
        }

        if (Input.GetKeyDown("]")) {
            currentPageNum--;
            ResetState();
            LoadPage();
        }

        // TODO - don't pass it like that maybe?
        pdfRenderer.scale = scale;

        // TODO - we shouldn't be doing this in Update, only in the OnQuadSizeInPixelsChanged callback
        if (scale != prevScale) {
            Debug.Log($"Scale changed, current = {scale}, previous = {prevScale}, reloading page part in Update()");
            LoadPagePart();
            prevScale = scale;
        }
    }

    private void OnZoomLevelChanged(float zoomLevel) {
        this.zoomLevel = zoomLevel;
    }

    private void LoadPage() {
        // TODO check if we need to load the page each time
        PDFPage p = document.GetPage(currentPageNum);
        currentPage = p;
        Vector2 size = p.GetPageSize();
        // 
        Vector2 proportions = size / new Vector2(Screen.width, Screen.height);
        float maxProportion = Mathf.Max(proportions.x, proportions.y);
        size /= maxProportion;

        // Resize to fit the screen with some small margin
        Vector2 quadScale1 = PAGE_SCALE_ON_SCREEN * QuadSwap.ScreenToWorldSize(size, Camera.main.orthographicSize, Camera.main.aspect);
        lowResQuad.transform.localScale = new Vector3(quadScale1.x, quadScale1.y, 1f);

        if (prevSize == size) {
            pdfRenderer.RenderPageToExistingTexture(p, texDefault);
        } else {
            if (texDefault != null) {
                DestroyImmediate(texDefault);
                Resources.UnloadAsset(texDefault);
            }
            size = PAGE_SCALE_ON_SCREEN * size;

            texDefault = pdfRenderer.RenderPageToTexture(p, width: Mathf.RoundToInt(size.x), height: Mathf.RoundToInt(size.y));
            texDefault.filterMode = FilterMode.Point;
            prevSize = size;
        }

        lowResQuad.GetComponent<MeshRenderer>().material.mainTexture = texDefault;
    }

    private void ResetState() {
        Debug.Log("Resetting state for new page load");

        zoomLevel = 1f;

        scale = 1f;
        prevScale = 1f;
        prevSize = -Vector2.one;
        texDefault = null;
        texHighRes = null;
        pdfRenderer.scale = 1f;
        pdfRenderer.lowResQuadSizePixels = Vector2.zero;
        highResQuad.SetActive(false);

        cameraZoom.Reset();
        cameraPan.Reset();
    }

    private void ReloadPageHighRes(Vector2 topLeft, Vector2 bottomRight) {
        Vector2 size = currentPage.GetPageSize();
        this.scale = zoomLevel;
        this.prevScale = zoomLevel;
        pdfRenderer.scale = zoomLevel;

        double timeStart = Time.realtimeSinceStartupAsDouble;
        if (prevSize == size && texHighRes != null) {
            pdfRenderer.RenderPagePartToExistingTexture(currentPage, texHighRes, topLeft, bottomRight);
        } else {

            Vector2 worldSizeLowRes = lowResQuad.transform.localScale;
            Camera cam = Camera.main;
            Vector2 quadLowResScreenSize = QuadSwap.WorldToScreenSize(worldSizeLowRes, cam.orthographicSize, cam.aspect);
            // TODO - don't pass stuff around like that; also if wrong , it will break the rendering
            pdfRenderer.lowResQuadSizePixels = quadLowResScreenSize;

            Debug.Log("Creating new texture");
            // TODO - there's a bug with 1 or 2 pixels difference than the base image, maybe we need to pass the relative sizes as double?
            // TODO - check if all textures are properly disposed
            // TODO - when initiating the reload with a zoom, there's a problem with the page offset; with pan this doesn't happen
            texHighRes = pdfRenderer.RenderPagePartToTexture(currentPage, (int)size.x, (int)size.y, topLeft, bottomRight);
            texHighRes.filterMode = FilterMode.Point;
            prevSize = size;
        }
        Debug.Log("Time to render high res: " + (Time.realtimeSinceStartupAsDouble - timeStart) + " seconds");

        highResQuad.GetComponent<MeshRenderer>().material.mainTexture = texHighRes;
    }

    private void OnZoomStarted() {
        // When we are zooming, especially when zoomung out, if we use point filtering the texture will look jagged
        if (texHighRes != null) {
            texHighRes.filterMode = FilterMode.Bilinear;
        }
    }

    public void OnZoomComplete() {
        // If we are not actively zooming the texture should be pixel perfect, so point filtering suffices
        if (texHighRes != null) {
            texHighRes.filterMode = FilterMode.Point;
        }
    }

    // TODO Unused
    public void OnTranslationOngoing() {
    }

    // TODO Unused
    private void OnTranslationComplete() {
        //Debug.Log("Translation complete");
        //LoadPagePart();
    }

    private void OnQuadSizeInPixelsChanged(Vector2 sizeInPixels) {
        Debug.Log("Quad size in pixels changed");

        if (texHighRes != null) {
            Debug.Log("Reinit texture");
            texHighRes.Reinitialize((int)sizeInPixels.x, (int)sizeInPixels.y);
        }

        LoadPagePart();
    }

    public void LoadPagePart() {
        Debug.Log("Load page part");
        Vector4 whFactors = GetRelativeRectOnPage();
        Vector2 topLeft = new Vector2(whFactors.x, whFactors.y);
        Vector2 bottomRight = new Vector2(whFactors.z, whFactors.w);

        ReloadPageHighRes(topLeft, bottomRight);
    }

    public Vector4 GetRelativeRectOnPage() {
        Vector3 hrqCenter = highResQuad.transform.position;
        Vector3 hrqExtents = highResQuad.transform.localScale * 0.5f;
        Vector2 topLeftHRQAbsolute = new Vector2(hrqCenter.x - hrqExtents.x, hrqCenter.z + hrqExtents.y);
        Vector2 bottomRightHRQAbsolute = new Vector2(hrqCenter.x + hrqExtents.x, hrqCenter.z - hrqExtents.y);

        Vector3 lrqCenter = lowResQuad.transform.position;
        Vector3 lrqExtents = lowResQuad.transform.localScale * 0.5f;

        Vector2 topLeftLRQAbsolute = new Vector2(lrqCenter.x - lrqExtents.x, lrqCenter.z + lrqExtents.y);
        Vector2 bottomRightLRQAbsolute = new Vector2(lrqCenter.x + lrqExtents.x, lrqCenter.z - lrqExtents.y);

        Vector2 lrqSize = new Vector2(lrqExtents.x * 2, lrqExtents.y * 2);

        Vector2 topLeftRelative = (topLeftHRQAbsolute - topLeftLRQAbsolute) / lrqSize;
        Vector2 bottomRightRelative = (bottomRightHRQAbsolute - topLeftLRQAbsolute) / lrqSize;

        // Invert the y coordinates to make them vary between 0 and 1
        topLeftRelative.y = -topLeftRelative.y;
        bottomRightRelative.y = -bottomRightRelative.y;

        return new Vector4(topLeftRelative.x, topLeftRelative.y, bottomRightRelative.x, bottomRightRelative.y);
    }
}

