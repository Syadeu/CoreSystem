using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Proxy;

namespace Syadeu.Presentation.Entities
{
    public interface IEntityOnProxyCreated
    {
        void OnProxyCreated(EntityBase entityBase, Entity<IEntity> entity, RecycleableMonobehaviour monoObj);
    }
    public interface IEntityOnProxyRemoved
    {
        void OnProxyRemoved(EntityBase entityBase, Entity<IEntity> entity, RecycleableMonobehaviour monoObj);
    }
}
