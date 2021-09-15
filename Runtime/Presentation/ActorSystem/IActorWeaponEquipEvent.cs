namespace Syadeu.Presentation.Actor
{
    public interface IActorWeaponEquipEvent
    {
        ActorWeaponEquipOptions EquipOptions { get; }
        Instance<ActorWeaponData> Weapon { get; }
    }

    public enum ActorWeaponEquipOptions
    {
        FollowProviderSettings      =   0,

        SwitchWithSelected          =   0b00001,

        ToInventoryIfIsFull         =   0b00010
    }
}
