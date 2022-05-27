using Syadeu.Collections;
using Syadeu.Presentation;
using Syadeu.Presentation.Actor;
using SyadeuEditor.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace SyadeuEditor.Presentation
{
    [System.Obsolete("Use Unity Serialized -> PropertyDrawer<T>", true)]
    public sealed class ActorControllerAttributeDrawer : ObjectBaseDrawer<ActorControllerAttribute>
    {
        private ArrayDrawer m_ProvidersDrawer;
        private FieldInfo m_ProvidersField;
        private readonly List<System.Type> m_CurrentProviders = new List<System.Type>();

        public ActorControllerAttributeDrawer(ObjectBase objectBase) : base(objectBase)
        {
            m_ProvidersDrawer = GetDrawer<ArrayDrawer>("Providers");
            m_ProvidersField = GetField("m_Providers");

            Check();
        }

        private void Check()
        {
            m_CurrentProviders.Clear();
            Reference<IActorProvider>[] temp = (Reference<IActorProvider>[])m_ProvidersField.GetValue(TargetObject);

            var iter = temp
                .Where((other) => !other.IsEmpty() && other.IsValid())
                .Select((other) => other.GetObject().GetType());

            m_CurrentProviders.AddRange(iter);
        }
        protected override void DrawGUI()
        {
            Check();

            DrawHeader();
            DrawDescription();

            for (int i = 0; i < Drawers.Length; i++)
            {
                if (Drawers[i].Equals(m_ProvidersDrawer))
                {
                    DrawProviders(m_ProvidersDrawer);
                    continue;
                }

                DrawField(Drawers[i]);
            }
        }

        private bool IsValidProvider(ActorProviderRequireAttribute requireAttribute)
        {
            for (int i = 0; i < requireAttribute.m_RequireTypes.Length; i++)
            {
                foreach (var item in m_CurrentProviders)
                {
                    if (requireAttribute.m_RequireTypes[i].IsAssignableFrom(item)) return true;
                }
            }
            return false;
        }
        private string ListToString(IList<System.Type> types)
        {
            string temp = string.Empty;
            for (int i = 0; i < types.Count; i++)
            {
                if (!string.IsNullOrEmpty(temp))
                {
                    temp += $", {types[i].Name}";
                }
                else temp += types[i].Name;
            }
            return temp;
        }
        private void DrawProviders(ArrayDrawer providersDrawer)
        {
            //List<System.Type> temp = new List<System.Type>();
            //for (int i = 0; i < providersDrawer.Count; i++)
            //{
            //    ReferenceDrawer refDrawer = (ReferenceDrawer)providersDrawer[i];
            //    IFixedReference reference = refDrawer.Getter.Invoke();
            //    if (reference.IsEmpty() || !reference.IsValid())
            //    {
            //        continue;
            //    }

            //    ActorProviderRequireAttribute actorProviderRequire 
            //        = reference.GetObject().GetType().GetCustomAttribute<ActorProviderRequireAttribute>();

            //    if (actorProviderRequire == null)
            //    {
            //        continue;
            //    }

            //    if (!IsValidProvider(actorProviderRequire))
            //    {
            //        EditorGUILayout.HelpBox($"Require {ListToString(actorProviderRequire.m_RequireTypes)}", MessageType.Error);
            //    }
            //}

            //providersDrawer.OnGUI();
        }
    }
}
