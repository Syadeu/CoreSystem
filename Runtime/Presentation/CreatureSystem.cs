using Syadeu.Database;
using Syadeu.Database.CreatureData;
using Syadeu.Mono;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation
{
    public sealed class CreatureSystem : PresentationSystemEntity<CreatureSystem>
    {
        public override bool EnableBeforePresentation => true;
        public override bool EnableOnPresentation => true;
        public override bool EnableAfterPresentation => false;

        public void Spawn(Hash hash)
        {
            Creature entity = CreatureDataList.Instance.GetEntity(hash);
            var prefabInfo = PrefabList.Instance.ObjectSettings[entity.m_PrefabIdx];

            for (int i = 0; i < entity.m_OnSpawn.m_Scripts.Count; i++)
            {
                //entity.m_OnSpawn.m_Scripts[i]
            }
        }
    }
}
