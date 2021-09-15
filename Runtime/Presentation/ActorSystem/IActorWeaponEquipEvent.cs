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

        /// <summary>
        /// 현재 들고있는 무기와 바꿉니다. 
        /// 들고있던 무기는 <see cref="ActorInventoryProvider"/> 로 들어가며, 없으면 즉시 파괴됩니다.
        /// </summary>
        SwitchWithSelected          =   0b00001,

        /// <summary>
        /// 빈 착용 공간이 있으면 그 공간에 집어넣고, 만약 없으면 <see cref="ActorInventoryProvider"/> 로 들어갑니다.
        /// <see cref="ActorInventoryProvider"/> 가 없는 경우 에러를 호출한 뒤 즉시 파괴됩니다.
        /// </summary>
        ToInventoryIfIsFull         =   0b00010
    }
}
