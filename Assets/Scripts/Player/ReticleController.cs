using System.Runtime.CompilerServices;
using UnityEngine;

public class ReticleController : MonoBehaviour
{
    [Header("World Targets")]
    [SerializeField] private Transform outerReticleTarget;
    [SerializeField] private Transform innerReticleTarget;

    [Header("UI Reticles")]
    [SerializeField] private RectTransform outerReticleUI;
    [SerializeField] private RectTransform innerReticleUI;

    [Header("Camera")]
    [SerializeField] private Camera cam;

    private void Awake()
    {
        if (cam == null)
            cam = Camera.main;
    }

    private void LateUpdate()
    {
        UpdateReticle(outerReticleTarget, outerReticleUI);
        UpdateReticle(innerReticleTarget, innerReticleUI);
    }

    private void UpdateReticle(Transform reticleTarget, RectTransform reticleUI)
    {
        if (reticleTarget == null || reticleUI == null || cam == null)
        {
            return;
        }

        Vector3 screenPos = cam.WorldToScreenPoint(reticleTarget.position);

        if (!reticleUI.gameObject.activeSelf)
        {
            reticleUI.gameObject.SetActive(true);
        }

        reticleUI.position = screenPos;
    }

}
