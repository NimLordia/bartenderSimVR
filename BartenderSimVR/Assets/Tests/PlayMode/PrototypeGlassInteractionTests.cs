using System.Collections;
using BartenderSimVR.Prototype;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace BartenderSimVR.Tests
{
    /// <summary>Exercises real XRI selection and physics without a headset or tracked input.</summary>
    [PrebuildSetup("BartenderSimVR.Tests.Editor.HeadlessXRTestSetup")]
    [PostBuildCleanup("BartenderSimVR.Tests.Editor.HeadlessXRTestSetup")]
    public sealed class PrototypeGlassInteractionTests
    {
        GameObject fixture;
        XRInteractionManager manager;
        XRDirectInteractor left;
        XRDirectInteractor right;
        XRGrabInteractable glass;
        Rigidbody body;
        PrototypeGlassRecovery recovery;
        Transform returnPoint;
        float previousCaptureDeltaTime;

        [SetUp]
        public void SetUp()
        {
            // Uncapped batch frames can be below XRI's 1 ms throw-sampling threshold.
            // Advance a realistic render timestep while retaining actual fixed-step physics.
            previousCaptureDeltaTime = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 72f;
            fixture = new GameObject("Glass Interaction Test Fixture");
            manager = Child("XR Interaction Manager").AddComponent<XRInteractionManager>();
            returnPoint = Child("Return Point").transform;
            returnPoint.SetPositionAndRotation(new Vector3(0.24f, 1.103f, 0.70f), Quaternion.Euler(0f, 35f, 0f));

            var floor = Child("Floor");
            floor.transform.position = new Vector3(0f, -0.05f, 0f);
            floor.AddComponent<BoxCollider>().size = new Vector3(8f, 0.1f, 8f);
            var counter = Child("Return Support");
            counter.transform.position = new Vector3(returnPoint.position.x, 0.5f, returnPoint.position.z);
            counter.AddComponent<BoxCollider>().size = new Vector3(0.4f, 1f, 0.4f);

            var glassObject = Child("Test Glass");
            glassObject.SetActive(false);
            glassObject.transform.SetPositionAndRotation(returnPoint.position, returnPoint.rotation);
            var collider = glassObject.AddComponent<CapsuleCollider>();
            collider.radius = 0.045f;
            collider.height = 0.20f;
            body = glassObject.AddComponent<Rigidbody>();
            body.mass = 0.18f;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            glass = glassObject.AddComponent<XRGrabInteractable>();
            glass.interactionManager = manager;
            glass.colliders.Add(collider);
            glass.selectMode = InteractableSelectMode.Single;
            glass.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            glass.useDynamicAttach = true;
            glass.throwOnDetach = false;
            recovery = glassObject.AddComponent<PrototypeGlassRecovery>();
            recovery.ReturnPoint = returnPoint;
            glassObject.SetActive(true);

            left = CreateInteractor("Left Test Hand");
            right = CreateInteractor("Right Test Hand");
            Physics.SyncTransforms();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(fixture);
            Time.captureDeltaTime = previousCaptureDeltaTime;
            yield return null;
        }

        [UnityTest]
        public IEnumerator AlternatingHandsCanGrabReleaseAndRecoverTwentyTimes()
        {
            var entered = 0;
            var exited = 0;
            glass.selectEntered.AddListener(_ => entered++);
            glass.selectExited.AddListener(_ => exited++);

            for (var cycle = 0; cycle < 20; cycle++)
            {
                var hand = cycle % 2 == 0 ? left : right;
                Select(hand);
                Assert.That(glass.isSelected, Is.True, "Grab failed on cycle " + cycle);
                Assert.That(glass.interactorsSelecting.Count, Is.EqualTo(1));
                Assert.That(glass.interactorsSelecting[0], Is.SameAs(hand));
                yield return null;
                Assert.That(glass.isSelected, Is.True, "Selection did not survive a frame on cycle " + cycle);

                Release(hand);
                yield return null; // XRI completes its deferred detach before recovery.
                Assert.That(glass.isSelected, Is.False);
                Assert.That(left.interactablesSelected.Count, Is.Zero);
                Assert.That(right.interactablesSelected.Count, Is.Zero);
                Assert.That(recovery.TryReturn(), Is.True);
                yield return new WaitForFixedUpdate();
                Assert.That(Vector3.Distance(body.position, returnPoint.position), Is.LessThan(0.02f));
            }

            Assert.That(entered, Is.EqualTo(20), "Duplicate or missed selection events.");
            Assert.That(exited, Is.EqualTo(20), "Duplicate or missed release events.");
            Assert.That(recovery.ReturnCount, Is.EqualTo(20));
        }

        [UnityTest]
        public IEnumerator HeldGlassIgnoresManualAndAutomaticRecoveryUntilReleased()
        {
            Select(left);
            left.transform.position = new Vector3(4f, 0.1f, 0f);
            yield return new WaitForSeconds(1f);

            Assert.That(glass.isSelected, Is.True);
            Assert.That(recovery.IsOutsideSafeBounds(body.position), Is.True);
            Assert.That(recovery.ReturnCount, Is.Zero, "A held glass was returned automatically.");
            var heldPosition = body.position;
            Assert.That(recovery.TryReturn(), Is.False);
            Assert.That(body.position, Is.EqualTo(heldPosition), "Manual reset moved a held glass.");

            Release(left);
            yield return null;
            Assert.That(recovery.TryReturn(), Is.True);
            Assert.That(glass.isSelected, Is.False);
            Assert.That(recovery.ReturnCount, Is.EqualTo(1));
        }

        [Test]
        public void RecoveryRestoresPoseAndClearsThrownLinearAndAngularVelocity()
        {
            body.position = new Vector3(3f, 2f, -2f);
            body.rotation = Quaternion.Euler(40f, 120f, 80f);
            body.linearVelocity = new Vector3(7f, -4f, 3f);
            body.angularVelocity = new Vector3(2f, 5f, -3f);

            Assert.That(recovery.TryReturn(), Is.True);
            Assert.That(Vector3.Distance(body.position, returnPoint.position), Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(body.rotation, returnPoint.rotation), Is.LessThan(0.001f));
            Assert.That(body.linearVelocity.sqrMagnitude, Is.LessThan(0.000001f));
            Assert.That(body.angularVelocity.sqrMagnitude, Is.LessThan(0.000001f));
            Assert.That(recovery.ReturnCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ReleaseAndReturnInOneFrameCancelsDeferredThrow()
        {
            glass.throwOnDetach = true;

            // First show this moving-hand fixture produces a real XRI throw on release.
            Select(left);
            yield return MoveWhileHeld(left);
            Release(left);
            yield return null;
            Assert.That(body.linearVelocity.sqrMagnitude, Is.GreaterThan(0.01f),
                "The fixture did not produce throw velocity, so it cannot exercise deferred detachment.");
            Assert.That(recovery.TryReturn(), Is.True);
            yield return new WaitForFixedUpdate();

            // The reset is accepted in the very same frame as release, before XRI LateUpdate.
            Select(right);
            yield return MoveWhileHeld(right);
            Release(right);
            Assert.That(recovery.TryReturn(), Is.True);
            yield return null;

            Assert.That(glass.isSelected, Is.False);
            Assert.That(Vector3.Distance(body.position, returnPoint.position), Is.LessThan(0.005f),
                "Deferred throw moved the returned glass off its pad.");
            Assert.That(body.linearVelocity.sqrMagnitude, Is.LessThan(0.000001f),
                "XRI reapplied throw velocity after the reset.");
            Assert.That(body.angularVelocity.sqrMagnitude, Is.LessThan(0.000001f));
            Assert.That(recovery.ReturnCount, Is.EqualTo(2), "One accepted reset must count as one return.");
            yield return new WaitForFixedUpdate();
            Assert.That(Vector3.Distance(body.position, returnPoint.position), Is.LessThan(0.005f));
        }

        [UnityTest]
        public IEnumerator ReleasedUprightFloorDropReturnsAfterDelayAndCanBeGrabbedAgain()
        {
            // Match the generated scene: XRI must save and restore an initially gravity-driven body.
            body.useGravity = true;
            body.WakeUp();
            Select(left);
            left.transform.position = new Vector3(-0.60f, 0.60f, 0f);
            yield return new WaitForSeconds(0.3f); // Complete the normal 0.15 s attach easing.
            Assert.That(Vector3.Distance(body.position, left.transform.position), Is.LessThan(0.05f),
                "Tracked grabbing did not move the glass into clear floor space.");
            Assert.That(recovery.IsOutsideSafeBounds(body.position), Is.False);
            Release(left);
            Assert.That(glass.isSelected, Is.False);
            Assert.That(body.useGravity, Is.True, "XRI did not restore gravity on release.");
            Assert.That(body.isKinematic, Is.False, "The released glass cannot fall while kinematic.");
            body.WakeUp();
            yield return new WaitForFixedUpdate();

            var deadline = Time.time + 2f;
            while (!recovery.IsOutsideSafeBounds(body.position) && Time.time < deadline)
                yield return null;
            Assert.That(recovery.IsOutsideSafeBounds(body.position), Is.True,
                "The floor drop never reached recovery. Position=" + body.position + ", velocity=" + body.linearVelocity +
                ", gravity=" + body.useGravity + ", sleeping=" + body.IsSleeping() + ", simulation=" + Physics.simulationMode);
            yield return new WaitForSeconds(0.3f);
            Assert.That(recovery.ReturnCount, Is.Zero, "Recovery ignored its delay.");

            deadline = Time.time + 2f;
            while (recovery.ReturnCount == 0 && Time.time < deadline)
                yield return null;
            Assert.That(recovery.ReturnCount, Is.EqualTo(1), "The upright glass remained stuck on the floor.");
            Assert.That(Vector3.Distance(body.position, returnPoint.position), Is.LessThan(0.03f));
            yield return new WaitForSeconds(0.2f);
            Assert.That(recovery.ReturnCount, Is.EqualTo(1), "The returned glass immediately entered a recovery loop.");

            Select(right);
            yield return null;
            Assert.That(glass.isSelected, Is.True, "The recovered glass could not be picked up again.");
            Release(right);
        }

        GameObject Child(string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(fixture.transform, false);
            return child;
        }

        XRDirectInteractor CreateInteractor(string name)
        {
            var hand = Child(name);
            hand.SetActive(false);
            hand.transform.position = new Vector3(0f, 1.2f, -2f);
            var trigger = hand.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.07f;
            var interactor = hand.AddComponent<XRDirectInteractor>();
            interactor.interactionManager = manager;
            interactor.improveAccuracyWithSphereCollider = true;
            interactor.keepSelectedTargetValid = true;
            interactor.selectActionTrigger = XRBaseInputInteractor.InputTriggerType.State;
            interactor.selectInput.inputSourceMode = XRInputButtonReader.InputSourceMode.ManualValue;
            interactor.activateInput.inputSourceMode = XRInputButtonReader.InputSourceMode.Unused;
            hand.SetActive(true);
            return interactor;
        }

        void Select(XRDirectInteractor hand)
        {
            hand.transform.position = body.position;
            hand.selectInput.manualPerformed = true;
            hand.selectInput.manualValue = 1f;
            manager.SelectEnter((IXRSelectInteractor)hand, (IXRSelectInteractable)glass);
        }

        void Release(XRDirectInteractor hand)
        {
            hand.selectInput.manualPerformed = false;
            hand.selectInput.manualValue = 0f;
            manager.SelectExit((IXRSelectInteractor)hand, (IXRSelectInteractable)glass);
            hand.transform.position = new Vector3(0f, 1.2f, -2f);
        }

        IEnumerator MoveWhileHeld(XRDirectInteractor hand)
        {
            for (var frame = 0; frame < 6; frame++)
            {
                hand.transform.position += new Vector3(0.10f, 0.06f, 0f);
                yield return new WaitForFixedUpdate();
                yield return null;
            }
        }
    }
}
