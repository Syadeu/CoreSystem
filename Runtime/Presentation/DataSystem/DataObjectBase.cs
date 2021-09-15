namespace Syadeu.Presentation.Data
{
    public abstract class DataObjectBase : ObjectBase
    {
        internal virtual void InternalOnCreated()
        {
            OnCreated();
        }
        internal virtual void InternalOnDestroy()
        {
            OnDestroy();
        }

        public override sealed object Clone() => base.Clone();

        protected virtual void OnCreated() { }
        protected virtual void OnDestroy() { }
    }
}
