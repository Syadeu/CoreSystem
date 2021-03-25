
using Syadeu.Database;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
#endif

namespace Syadeu.Mono
{
    public sealed class UserTagNameModule : ScriptableObject
    {
        [SerializeField] private string UserTag1 = "UserTag1";
        [SerializeField] private string UserTag2 = "UserTag2";
        [SerializeField] private string UserTag3 = "UserTag3";
        [SerializeField] private string UserTag4 = "UserTag4";
        [SerializeField] private string UserTag5 = "UserTag5";
        [SerializeField] private string UserTag6 = "UserTag6";
        [SerializeField] private string UserTag7 = "UserTag7";
        [SerializeField] private string UserTag8 = "UserTag8";
        [SerializeField] private string UserTag9 = "UserTag9";
        [SerializeField] private string UserTag10 = "UserTag10";
        [SerializeField] private string UserTag11 = "UserTag11";
        [SerializeField] private string UserTag12 = "UserTag12";
        [SerializeField] private string UserTag13 = "UserTag13";
        [SerializeField] private string UserTag14 = "UserTag14";
        [SerializeField] private string UserTag15 = "UserTag15";
        [SerializeField] private string UserTag16 = "UserTag16";
        [SerializeField] private string UserTag17 = "UserTag17";
        [SerializeField] private string UserTag18 = "UserTag18";
        [SerializeField] private string UserTag19 = "UserTag19";
        [SerializeField] private string UserTag20 = "UserTag20";
        [SerializeField] private string UserTag21 = "UserTag21";
        [SerializeField] private string UserTag22 = "UserTag22";
        [SerializeField] private string UserTag23 = "UserTag23";
        [SerializeField] private string UserTag24 = "UserTag24";
        [SerializeField] private string UserTag25 = "UserTag25";
        [SerializeField] private string UserTag26 = "UserTag26";
        [SerializeField] private string UserTag27 = "UserTag27";
        [SerializeField] private string UserTag28 = "UserTag28";
        [SerializeField] private string UserTag29 = "UserTag29";
        [SerializeField] private string UserTag30 = "UserTag30";

        private Dictionary<string, int> m_Tags;
        private void OnEnable()
        {
            m_Tags = new Dictionary<string, int>();
            m_Tags.Add(UserTag1, 1);
            m_Tags.Add(UserTag2, 2);
            m_Tags.Add(UserTag3, 3);
            m_Tags.Add(UserTag4, 4);
            m_Tags.Add(UserTag5, 5);
            m_Tags.Add(UserTag6, 6);
            m_Tags.Add(UserTag7, 7);
            m_Tags.Add(UserTag8, 8);
            m_Tags.Add(UserTag9, 9);
            m_Tags.Add(UserTag10, 10);
            m_Tags.Add(UserTag11, 11);
            m_Tags.Add(UserTag12, 12);
            m_Tags.Add(UserTag13, 13);
            m_Tags.Add(UserTag14, 14);
            m_Tags.Add(UserTag15, 15);
            m_Tags.Add(UserTag16, 16);
            m_Tags.Add(UserTag17, 17);
            m_Tags.Add(UserTag18, 18);
            m_Tags.Add(UserTag19, 19);
            m_Tags.Add(UserTag20, 20);
            m_Tags.Add(UserTag21, 21);
            m_Tags.Add(UserTag22, 22);
            m_Tags.Add(UserTag23, 23);
            m_Tags.Add(UserTag24, 24);
            m_Tags.Add(UserTag25, 25);
            m_Tags.Add(UserTag26, 26);
            m_Tags.Add(UserTag27, 27);
            m_Tags.Add(UserTag28, 28);
            m_Tags.Add(UserTag29, 29);
            m_Tags.Add(UserTag30, 30);
        }
        public int GetUserTag(string name) => m_Tags[name];
        public int GetUserTag(UserTagFlag tag)
        {
            if (tag == UserTagFlag.UserTag31) return 31;
            else if (tag == UserTagFlag.UserTag32) return 32;

            return m_Tags[tag.ToString()];
        }
        public string GetUserTag(int idx)
        {
            if (idx.Equals(31)) return UserTagFlag.UserTag31.ToString();
            else if (idx.Equals(32)) return UserTagFlag.UserTag32.ToString();

            foreach (var item in m_Tags)
            {
                if (item.Value.Equals(idx)) return item.Key;
            }
            return string.Empty;
        }
    }
}
 