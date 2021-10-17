using Syadeu.Collections;

namespace Syadeu.Presentation.Actor
{
    public interface IActorWeaponEquipEvent
    {
        ActorWeaponEquipOptions EquipOptions { get; }
        Instance<ActorWeaponData> Weapon { get; }
    }
}
