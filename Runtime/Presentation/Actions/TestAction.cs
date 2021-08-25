using Syadeu.Database;
using Syadeu.Presentation.Entities;
using UnityEngine;

namespace Syadeu.Presentation.Actions
{
    public sealed class TestAction : ActionBase<TestAction>
    {
        public Reference<EntityDataBase> Test;
        public PrefabReference<GameObject> TestGame;

        protected override void OnExecute(EntityData<IEntityData> entity)
        {
            "executed".ToLog();
        }

        protected override void OnInitialize()
        {
            "initialized".ToLog();
        }

        protected override void OnTerminate()
        {
            "terminated".ToLog();
        }
    }
}
