﻿using Newtonsoft.Json;
using Newtonsoft.Json.Utilities;
using Syadeu.Database;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Entities
{
    public sealed class ObjectEntity : EntityBase
    {
        protected override ObjectBase Copy()
        {
            ObjectEntity clone = (ObjectEntity)base.Copy();
            return clone;
        }

        [Preserve]
        static void AOTCodeGeneration()
        {
            AotHelper.EnsureType<Reference<ObjectEntity>>();
            AotHelper.EnsureList<Reference<ObjectEntity>>();
            AotHelper.EnsureType<Entity<ObjectEntity>>();
            AotHelper.EnsureList<Entity<ObjectEntity>>();
            AotHelper.EnsureType<EntityData<ObjectEntity>>();
            AotHelper.EnsureList<EntityData<ObjectEntity>>();
            AotHelper.EnsureType<ObjectEntity>();
            AotHelper.EnsureList<ObjectEntity>();
        }
    }
}
