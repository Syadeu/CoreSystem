using Syadeu.Database;
using Syadeu.Presentation.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Burst;

namespace Syadeu.Presentation.Components
{
    [Obsolete("Use IJobParallelForEntities")]
    public sealed class QueryBuilder<TComponent>
        where TComponent : unmanaged, IEntityComponent
    {
        private static EntityComponentSystem s_System = null;

        static QueryBuilder()
        {
            PoolContainer<ParallelJob>.Initialize(JobFactory, 32);
        }
        internal static QueryBuilder<TComponent> QueryFactory()
        {
            return new QueryBuilder<TComponent>();
        }
        private static ParallelJob JobFactory()
        {
            return new ParallelJob();
        }

        private EntityComponentDelegate<EntityData<IEntityData>, TComponent> FunctionPointer;
        unsafe internal EntityData<IEntityData>* Entities;
        unsafe internal TComponent* Components;
        internal int Length;

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

        private ParallelJob LocalInitialize()
        {
            ParallelJob job = PoolContainer<ParallelJob>.Dequeue();
            job.FunctionPointer = FunctionPointer;
            job.Builder = this;

            unsafe
            {
                job.Entities = Entities;
                job.Components = Components;
            }

            return job;
        }
        public void Run() => LocalInitialize().Run(0, Length);
        public void Schedule() => LocalInitialize().Schedule(0, Length);

        unsafe private class ParallelJob
        {
            public QueryBuilder<TComponent> Builder = null;

            public EntityData<IEntityData>* Entities;
            public TComponent* Components;

            private int Count, Processed = 0;
            public EntityComponentDelegate<EntityData<IEntityData>, TComponent> FunctionPointer;

            public void Schedule(int start, int count)
            {
                Count = count;
                var result = Parallel.For(start, count, Init, Execute, Finally);
            }
            public void Run(int start, int count)
            {
                for (int i = start; i < count; i++)
                {
                    Execute(i);
                }

                PoolContainer<ParallelJob>.Enqueue(this);
                PoolContainer<QueryBuilder<TComponent>>.Enqueue(Builder);
                Builder = null;
            }

            private void Execute(int i)
            {
                if (Entities[i].IsEmpty()) return;

                FunctionPointer.Invoke(Entities[i], in Components[i]);
            }

            private int Init()
            {
                return 0;
            }
            private int Execute(int i, ParallelLoopState state, int processed)
            {
                if (!Entities[i].IsEmpty())
                {
                    FunctionPointer.Invoke(Entities[i], in Components[i]);
                }

                return processed += 1;
            }
            private void Finally(int processed)
            {
                Interlocked.Add(ref Processed, processed);
                
                if (Processed == Count)
                {
                    $"{Processed} Processed, Done".ToLog();
                    PoolContainer<ParallelJob>.Enqueue(this);

                    PoolContainer<QueryBuilder<TComponent>>.Enqueue(Builder);
                    Builder = null;
                }
            }
        }
    }
}
