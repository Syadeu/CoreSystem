namespace Syadeu
{
    public interface IInitialize
    {
        void Initialize();
    }
    public interface IInitialize<T>
    {
        void Initialize(T t);
    }
    public interface IInitialize<T, TA>
    {
        void Initialize(T t, TA ta);
    }
    public interface IInitialize<T, TA, TAA>
    {
        void Initialize(T t, TA ta, TAA taa);
    }
    public interface IInitialize<T, TA, TAA, TAAA>
    {
        void Initialize(T t, TA ta, TAA taa, TAAA taaa);
    }
}
