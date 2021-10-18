using Syadeu.Collections;

namespace Syadeu.Presentation.Actor
{
    public interface IActorWeaponEquipEvent : IActorEvent
    {
        ActorWeaponEquipOptions EquipOptions { get; }
        Instance<ActorWeaponData> Weapon { get; }
    }
}
