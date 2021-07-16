namespace Syadeu.Presentation
{
    public static class EntityExtensionMethods
    {
        public static IEntity GetEntity(this DataGameObject obj)
        {
            if (!PresentationSystem<EntitySystem>.IsValid()) return null;
            return PresentationSystem<EntitySystem>.System.GetEntity(obj.m_Idx);
        }
    }
}
