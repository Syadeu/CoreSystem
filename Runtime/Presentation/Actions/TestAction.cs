using Syadeu.Collections;
using Syadeu.Presentation.Entities;
using UnityEngine;

namespace Syadeu.Presentation.Actions
{
    public sealed class TestAction : TriggerAction
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
