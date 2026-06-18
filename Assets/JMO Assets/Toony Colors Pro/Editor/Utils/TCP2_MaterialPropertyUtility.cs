using UnityEditor;
using UnityEngine.Rendering;

namespace ToonyColorsPro.Utilities
{
    internal static class TCP2_MaterialPropertyUtility
    {
        public static ShaderPropertyType GetPropertyType(MaterialProperty property)
        {
#if UNITY_6000_2_OR_NEWER
            return property.propertyType;
#else
            return ConvertPropertyType(property.type);
#endif
        }

        public static ShaderPropertyFlags GetPropertyFlags(MaterialProperty property)
        {
#if UNITY_6000_2_OR_NEWER
            return property.propertyFlags;
#else
            return ConvertPropertyFlags(property.flags);
#endif
        }

#if !UNITY_6000_2_OR_NEWER
        private static ShaderPropertyType ConvertPropertyType(MaterialProperty.PropType propertyType)
        {
            switch (propertyType)
            {
                case MaterialProperty.PropType.Color:
                    return ShaderPropertyType.Color;
                case MaterialProperty.PropType.Vector:
                    return ShaderPropertyType.Vector;
                case MaterialProperty.PropType.Float:
                    return ShaderPropertyType.Float;
                case MaterialProperty.PropType.Range:
                    return ShaderPropertyType.Range;
                case MaterialProperty.PropType.Texture:
                    return ShaderPropertyType.Texture;
                case MaterialProperty.PropType.Int:
                    return ShaderPropertyType.Int;
                default:
                    return ShaderPropertyType.Float;
            }
        }

        private static ShaderPropertyFlags ConvertPropertyFlags(MaterialProperty.PropFlags propertyFlags)
        {
            ShaderPropertyFlags result = ShaderPropertyFlags.None;

            if ((propertyFlags & MaterialProperty.PropFlags.HideInInspector) != 0)
            {
                result |= ShaderPropertyFlags.HideInInspector;
            }

            if ((propertyFlags & MaterialProperty.PropFlags.PerRendererData) != 0)
            {
                result |= ShaderPropertyFlags.PerRendererData;
            }

            if ((propertyFlags & MaterialProperty.PropFlags.NoScaleOffset) != 0)
            {
                result |= ShaderPropertyFlags.NoScaleOffset;
            }

            if ((propertyFlags & MaterialProperty.PropFlags.Normal) != 0)
            {
                result |= ShaderPropertyFlags.Normal;
            }

            if ((propertyFlags & MaterialProperty.PropFlags.HDR) != 0)
            {
                result |= ShaderPropertyFlags.HDR;
            }

            if ((propertyFlags & MaterialProperty.PropFlags.Gamma) != 0)
            {
                result |= ShaderPropertyFlags.Gamma;
            }

            if ((propertyFlags & MaterialProperty.PropFlags.NonModifiableTextureData) != 0)
            {
                result |= ShaderPropertyFlags.NonModifiableTextureData;
            }

            return result;
        }
#endif
    }
}
