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

    // Control for enabling / disabling reticles
    private bool isReticlesActive = true;

    private void Awake()
    {
        if (cam == null)
            cam = Camera.main;
    }

    private void LateUpdate()
    {
        // Only update the reticles if the bool is true
        if (!isReticlesActive)
        {
            return;
        }

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

    public void HideReticles()
    {
        isReticlesActive = false;
        outerReticleUI.gameObject.SetActive(false);
        innerReticleUI.gameObject.SetActive(false);
    }
}
