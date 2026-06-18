using UnityEngine;

namespace FlowBlast.Core.Utilities
{
    public static class TransformHierarchyUtility
    {
        public static Transform FindChildRecursive(Transform parent, string childName)
        {
            if (parent == null || string.IsNullOrEmpty(childName))
            {
                return null;
            }

            Transform directChild = parent.Find(childName);

            if (directChild != null)
            {
                return directChild;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindChildRecursive(parent.GetChild(i), childName);

                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
