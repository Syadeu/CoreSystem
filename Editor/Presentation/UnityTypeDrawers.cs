using System;
using System.Reflection;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    public sealed class Float3Drawer : ObjectDrawer<float3>
    {
        private bool m_DrawName;

        public Float3Drawer(object parentObject, MemberInfo memberInfo, bool drawName) : base(parentObject, memberInfo)
        {
            m_DrawName = drawName;
        }

        public Float3Drawer(object parentObject, Type declaredType, Action<float3> setter, Func<float3> getter) : base(parentObject, declaredType, setter, getter)
        {
        }

        public override float3 Draw(float3 currentValue)
        {
            if (m_DrawName) return EditorGUILayout.Vector3Field(Name, currentValue);
            return EditorGUILayout.Vector3Field(string.Empty, currentValue);
        }
    }
    public sealed class Int3Drawer : ObjectDrawer<int3>
    {
        private bool m_DrawName;

        public Int3Drawer(object parentObject, MemberInfo memberInfo, bool drawName) : base(parentObject, memberInfo)
        {
            m_DrawName = drawName;
    }

        public Int3Drawer(object parentObject, Type declaredType, Action<int3> setter, Func<int3> getter) : base(parentObject, declaredType, setter, getter)
        {
        }

        public override int3 Draw(int3 currentValue)
        {
            Vector3Int temp;
            if (m_DrawName) temp = EditorGUILayout.Vector3IntField(Name, new Vector3Int(currentValue.x, currentValue.y, currentValue.z));
            else
            {
                temp = EditorGUILayout.Vector3IntField(string.Empty, new Vector3Int(currentValue.x, currentValue.y, currentValue.z));
            }

            return new int3(temp.x, temp.y, temp.z);
        }
    }
    public sealed class Float2Drawer : ObjectDrawer<float2>
    {
        private bool m_DrawName;

        public Float2Drawer(object parentObject, MemberInfo memberInfo, bool drawName) : base(parentObject, memberInfo)
        {
            m_DrawName = drawName;
        }

        public Float2Drawer(object parentObject, Type declaredType, Action<float2> setter, Func<float2> getter) : base(parentObject, declaredType, setter, getter)
        {
        }

        public override float2 Draw(float2 currentValue)
        {
            if (m_DrawName) return EditorGUILayout.Vector2Field(Name, currentValue);
            return EditorGUILayout.Vector2Field(string.Empty, currentValue);
        }
    }
    public sealed class Int2Drawer : ObjectDrawer<int2>
    {
        private bool m_DrawName;

        public Int2Drawer(object parentObject, MemberInfo memberInfo, bool drawName) : base(parentObject, memberInfo)
        {
            m_DrawName = drawName;
        }

        public Int2Drawer(object parentObject, Type declaredType, Action<int2> setter, Func<int2> getter) : base(parentObject, declaredType, setter, getter)
        {
        }

        public override int2 Draw(int2 currentValue)
        {
            Vector2Int temp;
            if (m_DrawName)
            {
                temp = EditorGUILayout.Vector2IntField(Name, new Vector2Int(currentValue.x, currentValue.y));
            }
            else temp = EditorGUILayout.Vector2IntField(string.Empty, new Vector2Int(currentValue.x, currentValue.y));

            return new int2(temp.x, temp.y);
        }
    }
    public sealed class quaternionDrawer : ObjectDrawer<quaternion>
    {
        private bool m_DrawName;

        public quaternionDrawer(object parentObject, MemberInfo memberInfo, bool drawName) : base(parentObject, memberInfo)
        {
            m_DrawName = drawName;
        }

        public quaternionDrawer(object parentObject, Type declaredType, Action<quaternion> setter, Func<quaternion> getter) : base(parentObject, declaredType, setter, getter)
        {
        }

        public override quaternion Draw(quaternion currentValue)
        {
            Vector4 temp;
            if (m_DrawName)
            {
                temp = EditorGUILayout.Vector4Field(Name, currentValue.value);
            }
            else temp = EditorGUILayout.Vector4Field(string.Empty, currentValue.value);
            return new quaternion(temp);
        }
    }

    public sealed class Vector3Drawer : ObjectDrawer<Vector3>
    {
        public Vector3Drawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }
        public override Vector3 Draw(Vector3 currentValue)
        {
            return EditorGUILayout.Vector3Field(Name, currentValue);
        }
    }
    public sealed class Vector3IntDrawer : ObjectDrawer<Vector3Int>
    {
        public Vector3IntDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }
        public override Vector3Int Draw(Vector3Int currentValue)
        {
            return EditorGUILayout.Vector3IntField(Name, currentValue);
        }
    }
    public sealed class Vector2Drawer : ObjectDrawer<Vector2>
    {
        public Vector2Drawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }

        public override Vector2 Draw(Vector2 currentValue)
        {
            return EditorGUILayout.Vector2Field(Name, currentValue);
        }
    }
    public sealed class Vector2IntDrawer : ObjectDrawer<Vector2Int>
    {
        public Vector2IntDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }

        public override Vector2Int Draw(Vector2Int currentValue)
        {
            return EditorGUILayout.Vector2IntField(Name, currentValue);
        }
    }

    public sealed class RectDrawer : ObjectDrawer<Rect>
    {
        public RectDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }

        public override Rect Draw(Rect currentValue)
        {
            return EditorGUILayout.RectField(Name, currentValue); 
        }
    }
    public sealed class RectIntDrawer : ObjectDrawer<RectInt>
    {
        public RectIntDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }

        public override RectInt Draw(RectInt currentValue)
        {
            return EditorGUILayout.RectIntField(Name, currentValue);
        }
    }
    public sealed class QuaternionDrawer : ObjectDrawer<Quaternion>
    {
        public QuaternionDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }
        public override Quaternion Draw(Quaternion currentValue)
        {
            Vector4 temp = new Vector4(currentValue.x, currentValue.y, currentValue.z, currentValue.w);
            Vector4 output = EditorGUILayout.Vector4Field(Name, temp);
            return new Quaternion(output.x, output.y, output.z, output.w);
        }
    }

    public sealed class ColorDrawer : ObjectDrawer<Color>
    {
        public ColorDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }
        public override Color Draw(Color currentValue)
        {
            return EditorGUILayout.ColorField(Name, currentValue);
        }
    }
    public sealed class Color32Drawer : ObjectDrawer<Color32>
    {
        public Color32Drawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }
        public override Color32 Draw(Color32 currentValue)
        {
            return EditorGUILayout.ColorField(Name, currentValue);
        }
    }
}
