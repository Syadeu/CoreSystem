using Syadeu.Mono;

namespace Syadeu.Database.Lua
{
    [System.Obsolete("", true)]
    internal sealed class CreatureBrainProxy : LuaProxyEntity<CreatureBrain>
    {
        public CreatureBrainProxy(CreatureBrain brain) : base(brain) { }

        public string Name => Target.DisplayName;

        public bool IsOnGrid => Target.IsOnGrid;
        public bool IsOnNavMesh => Target.IsOnNavMesh;
        public bool IsMoving => Target.IsMoving;

        public bool HasInventory => Target.GetComponent<CreatureInventory>() != null;


    }
}
