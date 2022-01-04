namespace Syadeu.Presentation.Data
{
    [InternalLowLevelEntity]
    public abstract class DataObjectBase : ObjectBase
    {
        public override sealed object Clone() => base.Clone();
    }
}
