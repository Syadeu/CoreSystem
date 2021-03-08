namespace Syadeu
{
    public interface IStaticMonoManager : IStaticManager
    {
        /// <summary>
        /// Hierarchy에서 표시될 이름을 설정합니다.
        /// 런타임에 아무런 영향을 주지 않습니다.
        /// </summary>
        string DisplayName { get; }
        /// <summary>
        /// true 일 경우, 씬이 전환되어도 파괴되지 않습니다.
        /// </summary>
        bool DontDestroy { get; }
        /// <summary>
        /// Hierarchy 에서 이 매니저 객체를 표시할지 결정합니다.
        /// <see cref="Mono.SyadeuSettings.m_VisualizeObjects"/> 가 true일 경우 영향받지 않습니다.
        /// </summary>
        bool HideInHierarchy { get; }

#pragma warning disable IDE1006 // Naming Styles
        UnityEngine.GameObject gameObject { get; }
        UnityEngine.Transform transform { get; }
#pragma warning restore IDE1006 // Naming Styles
    }
}
