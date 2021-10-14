namespace Syadeu.Presentation
{
    public interface IExecutable<T>
    {
        bool Predicate(in T t);
    }
}
