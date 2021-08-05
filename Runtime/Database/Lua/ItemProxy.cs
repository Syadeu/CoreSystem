//using Syadeu.Mono;
//using System;

//namespace Syadeu.Database.Lua
//{
//    [System.Obsolete("", true)]
//    internal sealed class ItemProxy : LuaProxyEntity<Item>
//    {
//        public ItemProxy(Item item) : base(item) { }

//        public string Name => Target.Name;
//        public string Guid => Target.Hash.ToString();

//        public Action<CreatureBrain>[] OnEquip
//        {
//            set
//            {
//                Target.m_OnEquip = null;
//                for (int i = 0; i < value.Length; i++)
//                {
//                    Target.m_OnEquip += value[i];
//                }
//            }
//        }
//        public Action<CreatureBrain>[] OnUnequip
//        {
//            set
//            {
//                Target.m_OnUnequip = null;
//                for (int i = 0; i < value.Length; i++)
//                {
//                    Target.m_OnUnequip += value[i];
//                }
//            }
//        }
//        public Action<CreatureBrain>[] OnUse
//        {
//            set
//            {
//                Target.m_OnUse = null;
//                for (int i = 0; i < value.Length; i++)
//                {
//                    Target.m_OnUse += value[i];
//                }
//            }
//        }
//        public Action<CreatureBrain>[] OnGet
//        {
//            set
//            {
//                Target.m_OnGet = null;
//                for (int i = 0; i < value.Length; i++)
//                {
//                    Target.m_OnGet += value[i];
//                }
//            }
//        }
//        public Action<CreatureBrain>[] OnDrop
//        {
//            set
//            {
//                Target.m_OnDrop = null;
//                for (int i = 0; i < value.Length; i++)
//                {
//                    Target.m_OnDrop += value[i];
//                }
//            }
//        }
//        public Action<ItemInstance>[] OnSpawn
//        {
//            set
//            {
//                Target.m_OnSpawn = null;
//                for (int i = 0; i < value.Length; i++)
//                {
//                    Target.m_OnSpawn += value[i];
//                }
//            }
//        }

//        #region Value
//        public int GetValueCount() => Target.m_Values.Count;
//        public bool HasValue(string name) => Target.m_Values.Contains(name);
//        public object GetValue(string name) => Target.m_Values.GetValue(name);
//        public void SetValue(string name, object value) => Target.m_Values.SetValue(name, value);
//        public void AddValue(string name, object value) => Target.m_Values.Add(name, value);
//        #endregion

//        #region Instance
//        public ItemInstance CreateInstance() => Target.CreateInstance();
//        public ItemInstance GetInstance(string guid) => Target.GetInstance(System.Guid.Parse(guid));
//        #endregion
//    }
//}
