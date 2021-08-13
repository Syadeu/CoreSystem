using Newtonsoft.Json;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Event;

namespace Syadeu.Presentation.Attributes
{
    [AttributeAcceptOnly(typeof(EntityBase))]
    public sealed class TriggerBoundAttribute : AttributeBase
    {
        [JsonIgnore] internal ClusterID m_ClusterID = ClusterID.Empty;

        [JsonProperty(Order = 0, PropertyName = "MatchWithAABB")] public bool m_MatchWithAABB;
    }
    internal sealed class TriggerBoundProcessor : AttributeProcessor<TriggerBoundAttribute>
    {
        protected override void OnInitialize()
        {
            EventSystem.AddEvent<OnTransformChanged>(OnTransformChangedEvent);

            base.OnInitialize();
        }
        private void OnTransformChangedEvent(OnTransformChanged ev)
        {
            TriggerBoundAttribute att = ev.entity.GetAttribute<TriggerBoundAttribute>();
            //if (att == )
        }
    }
}
