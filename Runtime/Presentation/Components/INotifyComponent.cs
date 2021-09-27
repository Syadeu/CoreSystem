using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Components
{
    public interface INotifyComponent<TComponent> where TComponent : unmanaged, IEntityComponent
    {
        EntityData<IEntityData> Parent { get; }
    }
}
