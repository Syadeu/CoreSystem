namespace Syadeu.Presentation
{
    public enum UpdateLoop
    {
        Default,

        /// <summary>
        /// <see cref="UnityEngine.PlayerLoop.PreLateUpdate"/>
        /// </summary>
        Transform,
        AfterTransform,
    }
}
