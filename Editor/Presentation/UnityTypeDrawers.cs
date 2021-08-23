using System.Reflection;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    public sealed class Float3Drawer : ObjectDrawer<float3>
    {
        public Float3Drawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }
        public override float3 Draw(float3 currentValue)
        {
            return EditorGUILayout.Vector3Field(Name, currentValue);
        }
    }
    public sealed class Int3Drawer : ObjectDrawer<int3>
    {
        public Int3Drawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }

        public override int3 Draw(int3 currentValue)
        {
            Vector3Int temp = EditorGUILayout.Vector3IntField(Name, new Vector3Int(currentValue.x, currentValue.y, currentValue.z));

            return new int3(temp.x, temp.y, temp.z);
        }
    }
    public sealed class Float2Drawer : ObjectDrawer<float2>
    {
        public Float2Drawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }

        public override float2 Draw(float2 currentValue)
        {
            return EditorGUILayout.Vector2Field(Name, currentValue);
        }
    }
    public sealed class Int2Drawer : ObjectDrawer<int2>
    {
        public Int2Drawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }
        public override int2 Draw(int2 currentValue)
        {
            Vector2Int temp = EditorGUILayout.Vector2IntField(Name, new Vector2Int(currentValue.x, currentValue.y));
            return new int2(temp.x, temp.y);
        }
    }
    public sealed class quaternionDrawer : ObjectDrawer<quaternion>
    {
        public quaternionDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }
        public override quaternion Draw(quaternion currentValue)
        {
            Vector4 temp =
                    EditorGUILayout.Vector4Field(Name, currentValue.value);
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
