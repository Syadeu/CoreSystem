using Newtonsoft.Json;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Data;
using Syadeu.Presentation.Entities;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Syadeu.Presentation.Actions
{
    public sealed class PlayAnimationClipEventAction : ActionBase<PlayAnimationClipEventAction>
    {
        [JsonProperty(Order = 0, PropertyName = "Data")]
        public Reference<EntityAnimationClipEventData> m_Data;

        [JsonIgnore] private EntityData<IEntityData> Executer { get; set; } = EntityData<IEntityData>.Empty;

        protected override void OnExecute(EntityData<IEntityData> entity)
        {
            Executer = entity;

            var oper
                = Addressables.LoadAssetAsync<AnimationClip>(m_Data.GetObject().m_AnimationClip.GetObjectSetting().m_RefPrefab);
            oper.Completed += Oper_Completed;
        }
        private void Oper_Completed(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<AnimationClip> obj)
        {
            CoreSystem.StartUnityUpdate(this, Update(Executer, m_Data.GetObject(), obj.Result));
        }
        protected override void OnTerminate()
        {
            Executer = EntityData<IEntityData>.Empty;
        }

        private static IEnumerator Update(EntityData<IEntityData> executer, EntityAnimationClipEventData data, AnimationClip clip)
        {
            Entity<IEntity> entity = PresentationSystem<EntitySystem>.System.CreateEntity(data.m_Entity, 0);
            ProxyTransform tr = (ProxyTransform)entity.transform;
            tr.enableCull = false;

            yield return new WaitUntil(() => tr.proxy != null);

            Mono.RecycleableMonobehaviour proxy = tr.proxy;
            AnimatorComponent component = proxy.GetComponent<AnimatorComponent>();
            if (component == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"This entity({entity.Name}) does not have animator but requested play animation.");
                entity.Destroy();
                yield break;
            }

            Animation animation = proxy.GetComponent<Animation>();
            if (animation == null)
            {
                animation = proxy.AddComponent<Animation>();
                animation.playAutomatically = false;
            }

            animation.clip = clip;
            animation.Play();

            for (int i = 0; i < data.m_OnClipStart.Length; i++)
            {
                data.m_OnClipStart[i].Execute(executer);
            }

            float passed = 0;
            while (passed < clip.length)
            {
                passed += Time.deltaTime;
                yield return null;
            }

            for (int i = 0; i < data.m_OnClipEnd.Length; i++)
            {
                data.m_OnClipEnd[i].Execute(executer);
            }
        }
    }
}
