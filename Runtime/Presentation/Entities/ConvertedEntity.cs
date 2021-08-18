using Newtonsoft.Json;
using Syadeu.Database;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.Entities
{
    public sealed class ConvertedEntity : EntityDataBase
    {
        [JsonIgnore] public GameObject gameObject { get; internal set; }
        [JsonIgnore] public Transform transform { get; internal set; }

        public float3 position
        {
            get => transform.position;
            set => transform.position = value;
        }
        public quaternion rotation
        {
            get => transform.rotation;
            set => transform.rotation = value;
        }
        public float3 eulerAngles
        {
            get => transform.eulerAngles;
            set => transform.eulerAngles = value;
        }
        public float3 localScale
        {
            get => transform.localScale;
            set => transform.localScale = value;
        }

        public float4x4 localToWorldMatrix => float4x4.TRS(position, rotation, localScale);
        public float4x4 worldToLocalMatrix => math.inverse(localToWorldMatrix);

        public override bool IsValid() => transform != null;
        protected override void OnDispose()
        {
            gameObject = null;
            transform = null;
        }
    }
}
