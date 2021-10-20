namespace Syadeu.Presentation.Data
{
    public abstract class DataObjectBase : ObjectBase
    {
        /// <summary>
        /// 객체를 사용하려고 할때 실행
        /// </summary>
        internal virtual void InternalOnCreated()
        {
            OnCreated();
        }

        public override sealed object Clone() => base.Clone();

        /// <summary>
        /// 객체를 사용하려고 할때 실행
        /// </summary>
        protected virtual void OnCreated() { }
    }
}
