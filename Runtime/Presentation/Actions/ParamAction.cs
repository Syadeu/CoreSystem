namespace Syadeu.Presentation.Actions
{
    public abstract class ParamAction<T> : ParamActionBase<ParamAction<T>, T> { }
    public abstract class ParamAction<T, TA> : ParamActionBase<ParamAction<T, TA>, T, TA> { }
}
