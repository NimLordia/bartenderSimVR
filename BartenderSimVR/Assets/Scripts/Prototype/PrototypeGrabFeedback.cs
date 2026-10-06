using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace BartenderSimVR.Prototype
{
    [RequireComponent(typeof(XRGrabInteractable))]
    public sealed class PrototypeGrabFeedback : MonoBehaviour
    {
        [SerializeField] MeshRenderer glassRenderer;
        [SerializeField] Material idleMaterial;
        [SerializeField] Material hoverMaterial;
        [SerializeField] Material heldMaterial;

        XRGrabInteractable grab;

        public void Configure(MeshRenderer target, Material idle, Material hover, Material held)
        {
            glassRenderer = target;
            idleMaterial = idle;
            hoverMaterial = hover;
            heldMaterial = held;
        }

        void OnEnable()
        {
            grab = GetComponent<XRGrabInteractable>();
            grab.hoverEntered.AddListener(OnHoverEntered);
            grab.hoverExited.AddListener(OnHoverExited);
            grab.selectEntered.AddListener(OnSelectEntered);
            grab.selectExited.AddListener(OnSelectExited);
            RefreshAppearance();
        }

        void OnDisable()
        {
            grab.hoverEntered.RemoveListener(OnHoverEntered);
            grab.hoverExited.RemoveListener(OnHoverExited);
            grab.selectEntered.RemoveListener(OnSelectEntered);
            grab.selectExited.RemoveListener(OnSelectExited);
        }

        void OnHoverEntered(HoverEnterEventArgs _) => RefreshAppearance();
        void OnHoverExited(HoverExitEventArgs _) => RefreshAppearance();
        void OnSelectEntered(SelectEnterEventArgs _) => RefreshAppearance();
        void OnSelectExited(SelectExitEventArgs _) => RefreshAppearance();

        void RefreshAppearance()
        {
            if (glassRenderer != null)
                glassRenderer.sharedMaterial = grab.isSelected ? heldMaterial : grab.isHovered ? hoverMaterial : idleMaterial;
        }
    }
}
