namespace Syadeu.Presentation.Data
{
    public abstract class DataObjectBase : ObjectBase
    {
        internal virtual void InternalOnCreated()
        {
            OnCreated();
        }

        public override sealed object Clone() => base.Clone();

        protected virtual void OnCreated() { }
    }
}
