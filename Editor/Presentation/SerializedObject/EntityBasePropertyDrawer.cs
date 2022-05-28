using UnityEditor;
using Syadeu.Presentation.Entities;
using SyadeuEditor.Utilities;
using UnityEngine;
using Syadeu.Collections.Editor;

namespace SyadeuEditor.Presentation
{
    [CustomPropertyDrawer(typeof(EntityBase), true)]
    public class EntityBasePropertyDrawer : EntityDataBasePropertyDrawer
    {
        protected SerializedProperty GetPrefabProperty(SerializedProperty property)
        {
            const string c_Str = nameof(EntityBase.Prefab);
            return property.FindPropertyRelative(c_Str);
        }
        protected SerializedProperty GetStaticBatchingProperty(SerializedProperty property)
        {
            const string c_Str = nameof(EntityBase.StaticBatching);
            return property.FindPropertyRelative(c_Str);
        }
        protected SerializedProperty GetCenterProperty(SerializedProperty property)
        {
            const string c_Str = nameof(EntityBase.Center);
            return property.FindPropertyRelative(c_Str);
        }
        protected SerializedProperty GetSizeProperty(SerializedProperty property)
        {
            const string c_Str = nameof(EntityBase.Size);
            return property.FindPropertyRelative(c_Str);
        }
        protected SerializedProperty GetEnableCullProperty(SerializedProperty property)
        {
            const string c_Str = "m_EnableCull";
            return property.FindPropertyRelative(c_Str);
        }

        protected override float PropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = DefaultHeight(property, label);
            // EntityDataBasePropertyDrawer
            {
                height += EntityDataBaseHeight(property, label);
            }
            
            height += EntityBaseHeight(property, label);
            height += GetHeightFrom(GetEnableCullProperty(property));

            return height;
        }
        protected override void OnPropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            DrawDefault(ref rect, property, label);
            // EntityDataBasePropertyDrawer
            {
                DrawEntityDataBase(ref rect, property, label);
            }

            DrawEntityBase(ref rect, property, label);

            DrawFrom(ref rect, GetEnableCullProperty(property));
        }

        protected float EntityBaseHeight(SerializedProperty property, GUIContent label)
        {
            float height = 0;

            // Model Section
            {
                height += 15 + 6;
                height += CoreGUI.GetLineHeight(1);
                height += EditorGUI.GetPropertyHeight(GetPrefabProperty(property));
            }

            height += EditorGUI.GetPropertyHeight(GetCenterProperty(property));
            height += EditorGUI.GetPropertyHeight(GetSizeProperty(property));

            height += 6;

            return height;
        }

        private static readonly GUIContent
            s_ModelContent = new GUIContent("Model"),
            s_DisableCullContent = new GUIContent("Disable Cull"),
            s_EnableCullContent = new GUIContent("Enable Cull"),
            s_StaticBatchingContent = new GUIContent("Static Batching");
        protected void DrawEntityBase(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            SerializedProperty
                enableCullProp = GetEnableCullProperty(property),
                staticBatchingProp = GetStaticBatchingProperty(property),
                prefabProp = GetPrefabProperty(property),

                centerProp = GetCenterProperty(property),
                sizeProp = GetSizeProperty(property);

            // Model Section
            {
                AutoRect modelAutoRect = new AutoRect(
                    rect.Pop(
                    15 + 6 + CoreGUI.GetLineHeight(1) + EditorGUI.GetPropertyHeight(prefabProp) + 6 +
                    EditorGUI.GetPropertyHeight(centerProp) + EditorGUI.GetPropertyHeight(sizeProp)
                    ));
                modelAutoRect.Pop(3);
                modelAutoRect.Indent(3);

                CoreGUI.Label(modelAutoRect.Pop(15), s_ModelContent, 15);
                modelAutoRect.Pop(6);

                CoreGUI.DrawRect(EditorGUI.IndentedRect(modelAutoRect.TotalRect), Color.black);
                EditorGUI.indentLevel++;

                // Enable Cull / Static Batching
                {
                    Rect boxRect = modelAutoRect.Pop(CoreGUI.GetLineHeight(1));
                    boxRect = EditorGUI.IndentedRect(boxRect);
                    CoreGUI.DrawBlock(boxRect, Color.black);

                    Rect[] rects = AutoRect.DivideWithRatio(boxRect, .5f, .5f);
                    enableCullProp.boolValue = CoreGUI.BoxToggleButton(
                        rects[0],
                        enableCullProp.boolValue,
                        enableCullProp.boolValue ? s_DisableCullContent : s_EnableCullContent,
                        ColorPalettes.PastelDreams.TiffanyBlue,
                        ColorPalettes.PastelDreams.HotPink);

                    staticBatchingProp.boolValue = CoreGUI.BoxToggleButton(
                        rects[1],
                        staticBatchingProp.boolValue,
                        s_StaticBatchingContent,
                        ColorPalettes.PastelDreams.TiffanyBlue,
                        ColorPalettes.PastelDreams.HotPink
                        );
                }

                // Prefab
                {
                    Rect prefabRect = modelAutoRect.Pop(EditorGUI.GetPropertyHeight(prefabProp));
                    EditorGUI.PropertyField(
                        prefabRect,
                        prefabProp
                        );
                }

                // Center / Size
                {
                    EditorGUI.PropertyField(
                        modelAutoRect.Pop(EditorGUI.GetPropertyHeight(centerProp)), centerProp);
                    EditorGUI.PropertyField(
                        modelAutoRect.Pop(EditorGUI.GetPropertyHeight(sizeProp)), sizeProp);
                }

                EditorGUI.indentLevel--;
            }
            
            /*                                                                                      */
            
            rect.Pop(3);
            CoreGUI.Line(EditorGUI.IndentedRect(rect.Pop(3)));
        }
    }
}
