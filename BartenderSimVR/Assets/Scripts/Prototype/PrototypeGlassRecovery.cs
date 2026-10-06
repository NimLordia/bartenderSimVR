using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace BartenderSimVR.Prototype
{
    /// <summary>Returns the first test glass after a floor drop or a reset request.</summary>
    [RequireComponent(typeof(Rigidbody), typeof(XRGrabInteractable))]
    public sealed class PrototypeGlassRecovery : MonoBehaviour
    {
        [SerializeField] Transform returnPoint;
        [SerializeField] InputActionReference resetAction;
        [SerializeField] Vector3 safeBoundsCenter = new Vector3(0f, 1.5f, 0f);
        [SerializeField] Vector3 safeBoundsSize = new Vector3(3.6f, 2.5f, 3.6f);
        [SerializeField, Min(0.1f)] float returnDelay = 0.75f;

        Rigidbody body;
        XRGrabInteractable grab;
        float outsideBoundsTime;
        int releasedFrame = -1;
        int lateRecoveryFrame = -1;
        bool returnAfterDetach;

        public Transform ReturnPoint { get => returnPoint; set => returnPoint = value; }
        public InputActionReference ResetAction { get => resetAction; set => resetAction = value; }
        public int ReturnCount { get; private set; }

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            grab = GetComponent<XRGrabInteractable>();
        }

        void OnEnable()
        {
            grab.selectExited.AddListener(OnReleased);
        }

        void OnDisable()
        {
            grab.selectExited.RemoveListener(OnReleased);
            returnAfterDetach = false;
        }

        void OnReleased(SelectExitEventArgs _) => releasedFrame = Time.frameCount;

        void LateUpdate()
        {
            // XRI's manager finishes throw detachment in LateUpdate at execution order -105.
            // This default-order LateUpdate runs afterwards, including release + B/Y in one frame.
            lateRecoveryFrame = Time.frameCount;
            if (returnAfterDetach)
            {
                returnAfterDetach = false;
                if (!grab.isSelected && returnPoint != null)
                    RestorePose();
            }

            // The scene's InputActionManager owns action lifetime. Never move a held glass.
            if (grab.isSelected)
            {
                outsideBoundsTime = 0f;
                return;
            }

            if (resetAction != null && resetAction.action.WasPressedThisFrame())
            {
                TryReturn();
                return;
            }

            if (IsOutsideSafeBounds(transform.position))
            {
                outsideBoundsTime += Time.deltaTime;
                if (outsideBoundsTime >= returnDelay)
                    TryReturn();
            }
            else
            {
                outsideBoundsTime = 0f;
            }
        }

        public bool IsOutsideSafeBounds(Vector3 position)
        {
            return !new Bounds(safeBoundsCenter, safeBoundsSize).Contains(position);
        }

        public bool TryReturn()
        {
            if (grab == null)
                grab = GetComponent<XRGrabInteractable>();
            if (body == null)
                body = GetComponent<Rigidbody>();
            if (returnPoint == null || grab.isSelected)
                return false;

            RestorePose();
            // Preserve the immediate public return while also cancelling XRI's queued throw
            // when another component requests a reset before the release frame has finished.
            returnAfterDetach = releasedFrame == Time.frameCount && lateRecoveryFrame != Time.frameCount;
            outsideBoundsTime = 0f;
            ReturnCount++;
            return true;
        }

        void RestorePose()
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.position = returnPoint.position;
            body.rotation = returnPoint.rotation;
            transform.SetPositionAndRotation(returnPoint.position, returnPoint.rotation);
            body.WakeUp();
        }
    }
}
