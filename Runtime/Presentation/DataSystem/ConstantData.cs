namespace Syadeu.Presentation.Data
{
    /// <summary>
    /// 시스템에서 자동으로 관리하는 Entity <see langword="abstract"/> 입니다.
    /// </summary>
    /// <remarks>
    /// 상속받는 엔티티는 사용자에 의해 객체가 생성되서는 안됩니다. 생성과 파괴는 <seealso cref="DataContainerSystem"/> 에서 관리합니다. 게임이 시작될때 생성되고, 게임이 종료될때 파괴됩니다. 
    /// 생성된 엔티티를 받아오려면 <seealso cref="DataContainerSystem.TryGetConstantEntities(Collections.TypeInfo, out Unity.Collections.FixedList4096Bytes{Collections.InstanceID})"/> 를 참조하세요.
    /// </remarks>
    [InternalLowLevelEntity]
    public abstract class ConstantData : DataObjectBase
    {

    }
}
