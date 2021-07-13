using Syadeu.Database;
using System.Collections.Generic;
using UnityEngine;

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

        internal Dictionary<string, CustomTagFlag> m_Tags;
        private void OnEnable()
        {
            m_Tags = new Dictionary<string, CustomTagFlag>();
            m_Tags.Add(CustomTag1, CustomTagFlag.CustomTag1);
            m_Tags.Add(CustomTag2, CustomTagFlag.CustomTag2);
            m_Tags.Add(CustomTag3, CustomTagFlag.CustomTag3);
            m_Tags.Add(CustomTag4, CustomTagFlag.CustomTag4);
            m_Tags.Add(CustomTag5, CustomTagFlag.CustomTag5);
            m_Tags.Add(CustomTag6, CustomTagFlag.CustomTag6);
            m_Tags.Add(CustomTag7, CustomTagFlag.CustomTag7);
            m_Tags.Add(CustomTag8, CustomTagFlag.CustomTag8);
            m_Tags.Add(CustomTag9, CustomTagFlag.CustomTag9);
            m_Tags.Add(CustomTag10, CustomTagFlag.CustomTag10);
            m_Tags.Add(CustomTag11, CustomTagFlag.CustomTag11);
            m_Tags.Add(CustomTag12, CustomTagFlag.CustomTag12);
            m_Tags.Add(CustomTag13, CustomTagFlag.CustomTag13);
            m_Tags.Add(CustomTag14, CustomTagFlag.CustomTag14);
            m_Tags.Add(CustomTag15, CustomTagFlag.CustomTag15);
            m_Tags.Add(CustomTag16, CustomTagFlag.CustomTag16);
            m_Tags.Add(CustomTag17, CustomTagFlag.CustomTag17);
            m_Tags.Add(CustomTag18, CustomTagFlag.CustomTag18);
            m_Tags.Add(CustomTag19, CustomTagFlag.CustomTag19);
            m_Tags.Add(CustomTag20, CustomTagFlag.CustomTag20);
            m_Tags.Add(CustomTag21, CustomTagFlag.CustomTag21);
            m_Tags.Add(CustomTag22, CustomTagFlag.CustomTag22);
            m_Tags.Add(CustomTag23, CustomTagFlag.CustomTag23);
            m_Tags.Add(CustomTag24, CustomTagFlag.CustomTag24);
            m_Tags.Add(CustomTag25, CustomTagFlag.CustomTag25);
            m_Tags.Add(CustomTag26, CustomTagFlag.CustomTag26);
            m_Tags.Add(CustomTag27, CustomTagFlag.CustomTag27);
            m_Tags.Add(CustomTag28, CustomTagFlag.CustomTag28);
            m_Tags.Add(CustomTag29, CustomTagFlag.CustomTag29);
            m_Tags.Add(CustomTag30, CustomTagFlag.CustomTag30);
        }
        public CustomTagFlag GetUserTag(string name) => m_Tags[name];
        //public int GetUserTag(CustomTagFlag tag)
        //{
        //    if (tag == CustomTagFlag.CustomTag31) return 31;
        //    else if (tag == CustomTagFlag.CustomTag32) return 32;

        //    return m_Tags[tag.ToString()];
        //}
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
 