namespace Syadeu.Presentation.Actor
{
    public interface IActorWeaponEquipEvent
    {
        public Instance<ActorWeaponData> Weapon { get; }
    }
}
