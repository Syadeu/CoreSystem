using Syadeu.Collections;
using Syadeu.Presentation.Entities;
using UnityEngine;

namespace Syadeu.Presentation.Actions
{
    public sealed class TestAction : TriggerAction
    {
        public Reference<EntityDataBase> Test;
        public PrefabReference<GameObject> TestGame;

        protected override void OnExecute(Entity<IObject> entity)
        {
            "executed".ToLog();
        }

        protected override void OnCreated()
        {
            "initialized".ToLog();
        }

        protected override void OnReserve()
        {
            "terminated".ToLog();
        }
    }
}
