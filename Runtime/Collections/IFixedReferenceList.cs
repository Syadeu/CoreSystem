namespace Syadeu.Collections
{
    public interface IFixedReferenceList<T>
        where T : class, IObject
    {
        int Length { get; }

        public IFixedReference<T> this[int index] { get; set; }

        void Clear();
    }
}
