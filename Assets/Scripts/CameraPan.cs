using System;
using UnityEngine;

public class CameraPan : MonoBehaviour {
    private static readonly Vector3 IILEGAL_POS = -Vector3.one;

    private Camera cam;
    private Transform cameraTransform;
    private Vector3 delta;
    private Vector3 previousWSPosition = IILEGAL_POS;
    private Vector3 previousSSPosition = IILEGAL_POS;
    private bool isPanning = false;
    private bool isFirstFrame = true;

    private float timeDelta = 0f;
    private const float PAN_TIME_DELTA = 0.2f;


    public GameObject targetQuad; // The GameObject where the PDF page is loaded

    public event Action TranslationOngoing;
    public event Action TranslationComplete;


    void Start() {
        cam = Camera.main;
        cameraTransform = cam.transform;
    }

    void Update() {
        HandleMouseInput();
        if (isPanning) {
            Pan();
            timeDelta += Time.deltaTime;
            if (timeDelta > PAN_TIME_DELTA) {
                TranslationOngoing?.Invoke();
                timeDelta = 0f;
            }
        }
    }

    private void HandleMouseInput() {
        if (Input.GetMouseButtonDown(0)) {
            Vector3 mousePosition = Input.mousePosition;
            Ray ray = cam.ScreenPointToRay(mousePosition);
            RaycastHit hit;
            bool hasHitPage = Physics.Raycast(ray, out hit) && hit.transform.gameObject == targetQuad;
            if (hasHitPage) {
                isPanning = true;
                isFirstFrame = true;
            }
        }

        if (Input.GetMouseButtonUp(0)) {
            isPanning = false;
            isFirstFrame = true;
            previousWSPosition = IILEGAL_POS;
            TranslationComplete?.Invoke();
        }

        if (isPanning) {
            OnMouseMoved(Input.mousePosition);
        }
    }

    public void OnMouseMoved(Vector3 mousePosition) {
        Vector3 currentWSPositionTemp = cam.ScreenToWorldPoint(mousePosition);
        Vector3 previousWSPositionTemp = cam.ScreenToWorldPoint(previousSSPosition);
        Vector3 currentWSPosition = new Vector3(currentWSPositionTemp.x, currentWSPositionTemp.z, 1f);
        Vector3 previousWSPosition = new Vector3(previousWSPositionTemp.x, previousWSPositionTemp.z, 1f);
        if (!isFirstFrame) {
            Vector3 deltaTemp = currentWSPosition - previousWSPosition;
            delta = new Vector3(-deltaTemp.x, 0f, -deltaTemp.y);
        } else {
            isFirstFrame = false;
        }

        previousSSPosition = mousePosition;
    }

    public void Pan() {
        cameraTransform.position += delta;
        TranslationOngoing?.Invoke();
    }
}