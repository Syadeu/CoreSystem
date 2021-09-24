using Syadeu.Presentation.Entities;
using System.Threading.Tasks;
using Unity.Burst;

namespace Syadeu.Presentation.Components
{
    public sealed class QueryBuilder<TComponent>
        where TComponent : unmanaged, IEntityComponent
    {
        private static EntityComponentSystem s_System = null;

        internal int ComponentIndex;
        private EntityComponentDelegate<EntityData<IEntityData>, TComponent> FunctionPointer;

        public static QueryBuilder<TComponent> ForEach(EntityComponentDelegate<EntityData<IEntityData>, TComponent> action)
        {
            if (s_System == null)
            {
                s_System = SharedStatic<EntityComponentConstrains>.GetOrCreate<EntityComponentSystem>().Data.SystemID.System;
            }

            QueryBuilder<TComponent> builder = s_System.CreateQueryBuilder<TComponent>();

            builder.FunctionPointer = action;

            return builder;
        }

        public void Run()
        {
            ParallelJob job = new ParallelJob
            {
                FunctionPointer = FunctionPointer
            };

            unsafe
            {
                job.Entities = s_System.m_ComponentBuffer[ComponentIndex].entity;
                job.Components = (TComponent*)s_System.m_ComponentBuffer[ComponentIndex].buffer;
            }

            int length = s_System.m_ComponentBuffer[ComponentIndex].length;
            for (int i = 0; i < length; i++)
            {
                job.Execute(i);
            }
        }
        public void Schedule()
        {
            ParallelJob job = new ParallelJob
            {
                FunctionPointer = FunctionPointer
            };

            unsafe
            {
                job.Entities = s_System.m_ComponentBuffer[ComponentIndex].entity;
                job.Components = (TComponent*)s_System.m_ComponentBuffer[ComponentIndex].buffer;
            }

            Parallel.For(0, s_System.m_ComponentBuffer[ComponentIndex].length, job.Execute);
        }

        unsafe private class ParallelJob
        {
            public EntityData<IEntityData>* Entities;
            public TComponent* Components;

            public EntityComponentDelegate<EntityData<IEntityData>, TComponent> FunctionPointer;

            public void Execute(int i)
            {
                if (Entities[i].IsEmpty()) return;

                FunctionPointer.Invoke(Entities[i], in Components[i]);
            }
        }
    }
}
