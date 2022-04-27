// Copyright 2021 Seung Ha Kim
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

using Newtonsoft.Json;
using Newtonsoft.Json.Utilities;
using Syadeu.Collections;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Proxy;
using Syadeu.Presentation.Render;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Entities
{
    [DisplayName("Entity: UI Object Entity")]
    [InternalLowLevelEntity]
    public sealed class UIObjectEntity : EntityBase,
        INotifyComponent<UIObjectCanvasGroupComponent>
    {
        [Header("Graphics")]
        [SerializeField, JsonProperty(Order = 0, PropertyName = "EnableAutoFade")]
        internal bool m_EnableAutoFade = false;
        [SerializeField, JsonProperty(Order = 1, PropertyName = "InitialAlpha")]
        internal float m_InitialAlpha = 1;

        [Space]
        [SerializeField, JsonProperty(Order = 2, PropertyName = "Events")]
        internal EntityEventProperty m_Events = new EntityEventProperty();

        [Preserve]
        static void AOTCodeGeneration()
        {
            AotHelper.EnsureType<InstanceID<UIObjectEntity>>();
            AotHelper.EnsureType<FixedReference<UIObjectEntity>>();
            AotHelper.EnsureType<Reference<UIObjectEntity>>();
            AotHelper.EnsureList<Reference<UIObjectEntity>>();
            AotHelper.EnsureType<Entity<UIObjectEntity>>();
            AotHelper.EnsureList<Entity<UIObjectEntity>>();
            AotHelper.EnsureType<UIObjectEntity>();
            AotHelper.EnsureList<UIObjectEntity>();
        }
    }
    internal sealed class UIObjectProcessor : EntityProcessor<UIObjectEntity>, 
        IEntityOnProxyCreated, IEntityOnProxyRemoved
    {
        private WorldCanvasSystem m_WorldCanvasSystem;

        protected override void OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, WorldCanvasSystem>(Bind);
        }
        private void Bind(WorldCanvasSystem other)
        {
            m_WorldCanvasSystem = other;
        }
        protected override void OnDispose()
        {
            m_WorldCanvasSystem = null;
        }

        protected override void OnCreated(UIObjectEntity e)
        {
            Entity<IEntityData> entity = Entity<IEntityData>.GetEntityWithoutCheck(e.Idx);
            entity.AddComponent<UIObjectCanvasGroupComponent>();
            ref var com = ref entity.GetComponent<UIObjectCanvasGroupComponent>();
            com = (new UIObjectCanvasGroupComponent() { m_Enabled = true });
            com.m_Parent = e.Idx;
            com.Alpha = e.m_InitialAlpha;

            e.m_Events.ExecuteOnCreated(entity);
        }
        protected override void OnDestroy(UIObjectEntity obj)
        {
            Entity<IEntityData> entity = Entity<IEntityData>.GetEntityWithoutCheck(obj.Idx);

            obj.m_Events.ExecuteOnDestroy(entity);
        }

        public void OnProxyCreated(EntityBase entityBase, Entity<IEntity> entity, RecycleableMonobehaviour monoObj)
        {
            UIObjectEntity uiObject = (UIObjectEntity)entityBase;
            var cg = monoObj.GetComponentUnity<CanvasGroup>();
            if (cg == null)
            {
                cg = monoObj.AddComponent<CanvasGroup>();
            }

            if (!entity.HasComponent<UIObjectCanvasGroupComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"UI Entity({entity.RawName}) dosen\'t have any {nameof(UIObjectCanvasGroupComponent)}.");

                return;
            }

            m_WorldCanvasSystem.InternalSetProxy(entityBase, entity.ToEntity<UIObjectEntity>(), cg);
        }
        public void OnProxyRemoved(EntityBase entityBase, Entity<IEntity> entity, RecycleableMonobehaviour monoObj)
        {
            //var cg = monoObj.GetComponentUnity<CanvasGroup>();

            //m_WorldCanvasSystem.InternalSetProxy(entityBase, entity.Cast<IEntity, UIObjectEntity>(), cg, false);
        }
    }
}
