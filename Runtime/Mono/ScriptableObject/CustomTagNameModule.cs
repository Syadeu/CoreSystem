
using Syadeu.Database;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
#endif

namespace Syadeu.Mono
{
    public sealed class CustomTagNameModule : ScriptableObject
    {
        [SerializeField] private string CustomTag1 = "CustomTag1";
        [SerializeField] private string CustomTag2 = "CustomTag2";
        [SerializeField] private string CustomTag3 = "CustomTag3";
        [SerializeField] private string CustomTag4 = "CustomTag4";
        [SerializeField] private string CustomTag5 = "CustomTag5";
        [SerializeField] private string CustomTag6 = "CustomTag6";
        [SerializeField] private string CustomTag7 = "CustomTag7";
        [SerializeField] private string CustomTag8 = "CustomTag8";
        [SerializeField] private string CustomTag9 = "CustomTag9";
        [SerializeField] private string CustomTag10 = "CustomTag10";
        [SerializeField] private string CustomTag11 = "CustomTag11";
        [SerializeField] private string CustomTag12 = "CustomTag12";
        [SerializeField] private string CustomTag13 = "CustomTag13";
        [SerializeField] private string CustomTag14 = "CustomTag14";
        [SerializeField] private string CustomTag15 = "CustomTag15";
        [SerializeField] private string CustomTag16 = "CustomTag16";
        [SerializeField] private string CustomTag17 = "CustomTag17";
        [SerializeField] private string CustomTag18 = "CustomTag18";
        [SerializeField] private string CustomTag19 = "CustomTag19";
        [SerializeField] private string CustomTag20 = "CustomTag20";
        [SerializeField] private string CustomTag21 = "CustomTag21";
        [SerializeField] private string CustomTag22 = "CustomTag22";
        [SerializeField] private string CustomTag23 = "CustomTag23";
        [SerializeField] private string CustomTag24 = "CustomTag24";
        [SerializeField] private string CustomTag25 = "CustomTag25";
        [SerializeField] private string CustomTag26 = "CustomTag26";
        [SerializeField] private string CustomTag27 = "CustomTag27";
        [SerializeField] private string CustomTag28 = "CustomTag28";
        [SerializeField] private string CustomTag29 = "CustomTag29";
        [SerializeField] private string CustomTag30 = "CustomTag30";

        private Dictionary<string, int> m_Tags;
        private void OnEnable()
        {
            m_Tags = new Dictionary<string, int>();
            m_Tags.Add(CustomTag1, 1);
            m_Tags.Add(CustomTag2, 2);
            m_Tags.Add(CustomTag3, 3);
            m_Tags.Add(CustomTag4, 4);
            m_Tags.Add(CustomTag5, 5);
            m_Tags.Add(CustomTag6, 6);
            m_Tags.Add(CustomTag7, 7);
            m_Tags.Add(CustomTag8, 8);
            m_Tags.Add(CustomTag9, 9);
            m_Tags.Add(CustomTag10, 10);
            m_Tags.Add(CustomTag11, 11);
            m_Tags.Add(CustomTag12, 12);
            m_Tags.Add(CustomTag13, 13);
            m_Tags.Add(CustomTag14, 14);
            m_Tags.Add(CustomTag15, 15);
            m_Tags.Add(CustomTag16, 16);
            m_Tags.Add(CustomTag17, 17);
            m_Tags.Add(CustomTag18, 18);
            m_Tags.Add(CustomTag19, 19);
            m_Tags.Add(CustomTag20, 20);
            m_Tags.Add(CustomTag21, 21);
            m_Tags.Add(CustomTag22, 22);
            m_Tags.Add(CustomTag23, 23);
            m_Tags.Add(CustomTag24, 24);
            m_Tags.Add(CustomTag25, 25);
            m_Tags.Add(CustomTag26, 26);
            m_Tags.Add(CustomTag27, 27);
            m_Tags.Add(CustomTag28, 28);
            m_Tags.Add(CustomTag29, 29);
            m_Tags.Add(CustomTag30, 30);
        }
        public int GetUserTag(string name) => m_Tags[name];
        public int GetUserTag(CustomTagFlag tag)
        {
            if (tag == CustomTagFlag.CustomTag31) return 31;
            else if (tag == CustomTagFlag.CustomTag32) return 32;

            return m_Tags[tag.ToString()];
        }
        public string GetUserTag(int idx)
        {
            if (idx.Equals(31)) return CustomTagFlag.CustomTag31.ToString();
            else if (idx.Equals(32)) return CustomTagFlag.CustomTag32.ToString();

            foreach (var item in m_Tags)
            {
                if (item.Value.Equals(idx)) return item.Key;
            }
            return string.Empty;
        }
    }
}
 