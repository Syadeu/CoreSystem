using Unity.Entities;

namespace Syadeu.ECS
{
    [UpdateAfter(typeof(ECSCopyTransformFromMonoSystem))]
    public class ECSPathSystemGroup : ComponentSystemGroup { }
}