// Copyright 2022 Seung Ha Kim
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Syadeu.Presentation;
using Syadeu.Presentation.Actor;
using SyadeuEditor.Utilities;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    [CustomPropertyDrawer(typeof(ActorItemAttribute))]
    public sealed class ActorItemAttributePropertyDrawer : ObjectBasePropertyDrawer
    {
        static class Helper
        {
            public static SerializedProperty GetItemTypeProperty(SerializedProperty property)
            {
                const string c_Str = "m_ItemType";
                return property.FindPropertyRelative(c_Str);
            }
            public static SerializedProperty GetGraphicsInfoProperty(SerializedProperty property)
            {
                const string c_Str = "m_GraphicsInfo";
                return property.FindPropertyRelative(c_Str);
            }
            public static SerializedProperty GetGeneralInfoProperty(SerializedProperty property)
            {
                const string c_Str = "m_GeneralInfo";
                return property.FindPropertyRelative(c_Str);
            }
            public static SerializedProperty GetWeaponInfoProperty(SerializedProperty property)
            {
                const string c_Str = "m_WeaponInfo";
                return property.FindPropertyRelative(c_Str);
            }
            public static SerializedProperty GetInteractionProperty(SerializedProperty property)
            {
                const string c_Str = "m_Interaction";
                return property.FindPropertyRelative(c_Str);
            }
        }

        protected override float PropertyHeight(SerializedProperty property, GUIContent label)
        {
            return DefaultHeight(property, label);
        }
        protected override void OnPropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            DrawDefault(ref rect, property, label);

            Space(ref rect, 10);
            Line(ref rect);
            Space(ref rect, 10);

            SerializedProperty itemTypeProp = Helper.GetItemTypeProperty(property);
            PropertyField(ref rect, itemTypeProp);

            Reference<ActorItemType> itemTypeReference = SerializedPropertyHelper.ReadReference<ActorItemType>(itemTypeProp);
            if (itemTypeReference.IsEmpty() || !itemTypeReference.IsValid())
            {
                HelpBox(ref rect, "Item requires ItemType.", MessageType.Error);
                return;
            }
            ActorItemType actorItemType = itemTypeReference.GetObject();

            SerializedProperty
                graphcisProp = Helper.GetGraphicsInfoProperty(property),
                generalProp = Helper.GetGeneralInfoProperty(property),
                weaponProp = Helper.GetWeaponInfoProperty(property),
                interactionProp = Helper.GetInteractionProperty(property);

            PropertyField(ref rect, graphcisProp);
            PropertyField(ref rect, generalProp);
            
            if (actorItemType.ItemCategory == ItemCategory.Weapon)
            {
                PropertyField(ref rect, weaponProp);
            }
            
            PropertyField(ref rect, interactionProp);
        }
    }
}
