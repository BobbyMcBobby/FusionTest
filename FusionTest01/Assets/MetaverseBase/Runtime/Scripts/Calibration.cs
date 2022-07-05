using UnityEngine;

// Modfied from:
// https://stackoverflow.com/questions/62467088/oculus-quest-real-world-alignment
// https://twitter.com/IRCSS/status/1231523329183559681/photo/1

// The original code had functionality for scaling the scene which were removed because it messed with positioning
// This script requires that an OVRCameraRig be in the scene and have the OVRManager script attached

// To calibrate
// 1. Put the right Oculus Touch controller at the Pivot A in the real world and press the A button, the virtual scene will adjust so the virtual Pivot A matches.
// 2. Put the right Oculus Touch controller at the Pivot B in the real world and press the A button, this will set the virtual scene angle.
// 3. Put the right Oculus Touch controller back to the Pivot A in the real world and press the A button, this will confirm the pivot positions.

namespace MILab.MetaverseBase
{
    public class Calibration : MonoBehaviour
    {
        // Serialized in inspector for debugging, but they will be set in Start function
        [SerializeField]
        private Transform HandTransform;
        [SerializeField]
        private Transform PivotATransform;
        [SerializeField]
        private Transform PivotBTransform;
        [SerializeField]
        private AligmentState alignmentState = AligmentState.None;

        private Vector3 resetPosition;
        private Quaternion resetRotation;
        private OVRHand rightHand;
        private OVRHand leftHand;

        public enum AligmentState
        {
            None,
            PivotOneSet,
            PivotTwoSet,
            PivotThreeSet,
        }

        void Start()
        {
            PivotATransform = transform.Find("PivotA");
            PivotBTransform = transform.Find("PivotB");

            HandTransform = OVRManager.instance.transform.Find("TrackingSpace/RightHandAnchor/RightControllerAnchor");
            rightHand = OVRManager.instance.transform.Find("TrackingSpace/RightHandAnchor").GetComponentInChildren<OVRHand>();
            leftHand = OVRManager.instance.transform.Find("TrackingSpace/LeftHandAnchor").GetComponentInChildren<OVRHand>();

            resetPosition = OVRManager.instance.transform.position;
            resetRotation = OVRManager.instance.transform.rotation;
        }

        void Update()
        {
            // bools for inputs
            bool setFlag = OVRInput.GetDown(OVRInput.Button.One) && !rightHand.GetFingerIsPinching(OVRHand.HandFinger.Index); // Mapped to A button and ignores pinches
            bool resetFlag = OVRInput.GetDown(OVRInput.Button.Two); // Mapped to B button, ignores pinches since those aren't mapped to B button

            switch (alignmentState)
            {
                case AligmentState.None:
                    if (setFlag)
                    {
                        // Move Player so that hand is at Pivot A
                        AdjustPosition();
                        alignmentState = AligmentState.PivotOneSet;
                    }
                    break;



                case AligmentState.PivotOneSet:
                    if (resetFlag)
                    {
                        // Reset
                        ResetTransform();
                    }
                    else if (setFlag)
                    {
                        // Face player forward   
                        AdjustRotation();
                        alignmentState = AligmentState.PivotTwoSet;
                    }
                    break;



                case AligmentState.PivotTwoSet:
                    if (resetFlag)
                    {
                        // Reset
                        ResetTransform();
                    }
                    else if (setFlag)
                    {
                        // Move Player again so that the hand is at PivotA
                        AdjustPosition();
                        alignmentState = AligmentState.PivotThreeSet;

                        // Hide pivot points
                        PivotATransform.gameObject.SetActive(false);
                        PivotBTransform.gameObject.SetActive(false);
                    }
                    break;



                case AligmentState.PivotThreeSet:
                    if (resetFlag)
                    {
                        // Show the Pivot points
                        PivotATransform.gameObject.SetActive(true);
                        PivotBTransform.gameObject.SetActive(true);
                        ResetTransform();
                    }
                    break;
            }
        }

        private void ResetTransform()
        {
            // Reset
            OVRManager.instance.transform.localScale = new Vector3(1, 1, 1);
            OVRManager.instance.transform.rotation = resetRotation;
            OVRManager.instance.transform.position = resetPosition;
            alignmentState = AligmentState.None;
        }

        private void AdjustPosition()
        {
            Vector3 handOffset = HandTransform.position - OVRManager.instance.transform.position;
            Vector3 newPosition = PivotATransform.position - handOffset;

            // Do not adjust Y position if using floor level tracking
            if (OVRManager.instance.GetComponent<OVRManager>().trackingOriginType == OVRManager.TrackingOrigin.FloorLevel)
            {
                newPosition.y = resetPosition.y;
            }

            OVRManager.instance.transform.position = newPosition;
        }

        private void AdjustRotation()
        {
            // Rotate player so they face forward
            Vector3 pivotAtoRealB = HandTransform.position - PivotATransform.position;
            Vector3 pivotAtoVirtualB = PivotBTransform.position - PivotATransform.position;

            float turnAngle = Vector3.SignedAngle(pivotAtoRealB, pivotAtoVirtualB, Vector3.up);

            OVRManager.instance.transform.RotateAround(PivotATransform.position, Vector3.up, turnAngle);
        }
    }
}