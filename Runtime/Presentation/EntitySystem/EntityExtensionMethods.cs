using Syadeu.Mono;
using Syadeu.ThreadSafe;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Syadeu.Presentation
{
    public static class EntityExtensionMethods
    {
        public static IEntity GetEntity(this DataGameObject obj)
        {
            if (!PresentationSystem<EntitySystem>.IsValid()) return null;
            return PresentationSystem<EntitySystem>.System.GetEntity(obj.m_Idx);
        }
        public static void Destory(this IEntity entity)
        {
            entity.gameObject.Destory();
        }

        public static ref GridManager.GridCell GetCurrentCell(this IEntity entity)
        {
            Vector3 pos = entity.transform.position;
            if (!GridManager.HasGrid(pos))
            {
                CoreSystem.Logger.LogError(Channel.Entity, $"This entity({entity.Name}) is not on the Grid");
                throw new System.Exception();
            }
            ref GridManager.Grid grid = ref GridManager.GetGrid(pos);
            if (!grid.HasCell(pos))
            {
                // 이건 버그임
                throw new System.Exception();
            }

            return ref grid.GetCell(pos);
        }
    }
}
