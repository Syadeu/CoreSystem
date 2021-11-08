﻿#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Collections.Proxy;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Render;
using System.ComponentModel;

namespace Syadeu.Presentation.TurnTable
{
    [DisplayName("TriggerAction: TRPG Set Camera")]
    public sealed class TRPGSetCameraAction : TriggerAction
    {
        [JsonProperty(Order = 0, PropertyName = "Follow")]
        private bool m_Follow = false;

        protected override void OnExecute(EntityData<IEntityData> entity)
        {
            if (!(entity.Target is IEntity))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"");
                return;
            }

            RenderSystem renderSystem = PresentationSystem<DefaultPresentationGroup, RenderSystem>.System;
            var movement = renderSystem.CameraComponent.GetCameraComponent<TRPGCameraMovement>();
            ITransform tr = entity.As<IEntityData, IEntity>().transform;

            if (m_Follow)
            {
                movement.SetTarget(tr);

                return;
            }

            movement.TargetPosition = tr.position;
        }
    }
}