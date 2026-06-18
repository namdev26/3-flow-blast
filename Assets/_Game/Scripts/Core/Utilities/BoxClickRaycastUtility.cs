using FlowBlast.Presentation.Box;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FlowBlast.Core.Utilities
{
    public static class BoxClickRaycastUtility
    {
        private const float MaxRayDistance = 200f;

        public static bool TryGetClickedBoxView(Camera camera, out BoxView boxView)
        {
            boxView = null;

            if (camera == null || !TryGetPointerScreenPosition(out Vector2 screenPosition))
            {
                return false;
            }

            Ray ray = camera.ScreenPointToRay(screenPosition);

            if (!Physics.Raycast(ray, out RaycastHit hit, MaxRayDistance))
            {
                return false;
            }

            boxView = hit.collider.GetComponentInParent<BoxView>();
            return boxView != null;
        }

        private static bool TryGetPointerScreenPosition(out Vector2 screenPosition)
        {
            if (Pointer.current != null)
            {
                screenPosition = Pointer.current.position.ReadValue();
                return true;
            }

            if (Touchscreen.current != null)
            {
                screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }

            if (Mouse.current != null)
            {
                screenPosition = Mouse.current.position.ReadValue();
                return true;
            }

            screenPosition = Vector2.zero;
            return false;
        }
    }
}
