using UnityEngine;

public interface MovementEventsListener {
    public void OnZoomLevelChanged(float zoom) { }
    public void OnZoomComplete(float zoomLevel, ref RaycastHit zoomCenterHit) { }
    public void OnTranslationOngoing() { }
    public void OnTranslationComplete() { }
}
