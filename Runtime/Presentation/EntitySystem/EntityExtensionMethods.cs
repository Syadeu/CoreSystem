using Syadeu.Mono;
using Syadeu.Presentation.Entities;
using Syadeu.ThreadSafe;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Syadeu.Presentation
{
    public static class EntityExtensionMethods
    {
        //public static void Destroy(this IEntity entity)
        //{
        //    entity.gameObject.Destory();
        //}
        //public static void Destroy(this IObject obj)
        //{
        //    PresentationSystem<EntitySystem>.System.DestroyObject(obj.Idx);
        //}

        //[Obsolete("", true)]
        //public static ref GridManager.GridCell GetCurrentCell(this IEntity entity)
        //{
        //    Vector3 pos = entity.transform.position;
        //    if (!GridManager.HasGrid(pos))
        //    {
        //        CoreSystem.Logger.LogError(Channel.Entity, $"This entity({entity.Name}) is not on the Grid");
        //        throw new System.Exception();
        //    }
        //    ref GridManager.Grid grid = ref GridManager.GetGrid(pos);
        //    if (!grid.HasCell(pos))
        //    {
        //        // 이건 버그임
        //        throw new System.Exception();
        //    }

        //    return ref grid.GetCell(pos);
        //}
    }
}
