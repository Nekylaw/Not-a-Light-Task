using UnityEngine;
using System.Collections;

public class StelaFocusComponent : MonoBehaviour
{
    public Transform Stela;
    public float Duration = 2f;
    public float ZoomFOV = 30f;
    public float FocusTime = 1f;

    private bool hasPlayed = false;
    private Camera mainCam;
    private PlayerController _playerController;
    private float originalFOV;
    private Quaternion originalRotation;

    private void OnTriggerEnter(Collider other)
    {
        if (hasPlayed) return;

        if (!other.TryGetComponent<PlayerController>(out _playerController))
            return;

        mainCam = Camera.main;
        originalFOV = mainCam.fieldOfView;
        originalRotation = mainCam.transform.rotation;

        StartCoroutine(PlayCinematic());
        hasPlayed = true;
    }

    IEnumerator PlayCinematic()
    {
        ToggleControls(true); ;

        float elapsed = 0f;
        Quaternion startRot = mainCam.transform.rotation;
        Quaternion targetRot = Quaternion.LookRotation(Stela.position - mainCam.transform.position);
        float startFOV = mainCam.fieldOfView;

        while (elapsed < Duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / Duration;
            mainCam.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            mainCam.fieldOfView = Mathf.Lerp(startFOV, ZoomFOV, t);
            yield return null;
        }

        yield return new WaitForSeconds(FocusTime);

        elapsed = 0f;
        while (elapsed < Duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / Duration;
            mainCam.transform.rotation = Quaternion.Slerp(targetRot, originalRotation, t);
            mainCam.fieldOfView = Mathf.Lerp(ZoomFOV, originalFOV, t);
            yield return null;
        }

        ToggleControls(false);
    }

    private void ToggleControls(bool lockControls)
    {
        _playerController.enabled = !lockControls;

        var cameraModifiers = _playerController.GetComponentsInChildren<ICameraModifier>();
        if (cameraModifiers != null && cameraModifiers.Length > 0)
        {
            foreach (var modifier in cameraModifiers)
                modifier.IsCameraLocked = lockControls;
        }
    }


}
