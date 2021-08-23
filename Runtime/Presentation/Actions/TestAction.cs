namespace Syadeu.Presentation.Entities
{
    public sealed class TestAction : ActionBase<TestAction>
    {
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
