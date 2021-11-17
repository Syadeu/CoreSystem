// Copyright 2021 Seung Ha Kim
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.


using Syadeu.Collections;
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

        internal Dictionary<string, UserTagFlag> m_Tags;
        private void OnEnable()
        {
            m_Tags = new Dictionary<string, UserTagFlag>();
            m_Tags.Add(UserTag1, UserTagFlag.UserTag1);
            m_Tags.Add(UserTag2, UserTagFlag.UserTag2);
            m_Tags.Add(UserTag3, UserTagFlag.UserTag3);
            m_Tags.Add(UserTag4, UserTagFlag.UserTag4);
            m_Tags.Add(UserTag5, UserTagFlag.UserTag5);
            m_Tags.Add(UserTag6, UserTagFlag.UserTag6);
            m_Tags.Add(UserTag7, UserTagFlag.UserTag7);
            m_Tags.Add(UserTag8, UserTagFlag.UserTag8);
            m_Tags.Add(UserTag9, UserTagFlag.UserTag9);
            m_Tags.Add(UserTag10, UserTagFlag.UserTag10);
            m_Tags.Add(UserTag11, UserTagFlag.UserTag11);
            m_Tags.Add(UserTag12, UserTagFlag.UserTag12);
            m_Tags.Add(UserTag13, UserTagFlag.UserTag13);
            m_Tags.Add(UserTag14, UserTagFlag.UserTag14);
            m_Tags.Add(UserTag15, UserTagFlag.UserTag15);
            m_Tags.Add(UserTag16, UserTagFlag.UserTag16);
            m_Tags.Add(UserTag17, UserTagFlag.UserTag17);
            m_Tags.Add(UserTag18, UserTagFlag.UserTag18);
            m_Tags.Add(UserTag19, UserTagFlag.UserTag19);
            m_Tags.Add(UserTag20, UserTagFlag.UserTag20);
            m_Tags.Add(UserTag21, UserTagFlag.UserTag21);
            m_Tags.Add(UserTag22, UserTagFlag.UserTag22);
            m_Tags.Add(UserTag23, UserTagFlag.UserTag23);
            m_Tags.Add(UserTag24, UserTagFlag.UserTag24);
            m_Tags.Add(UserTag25, UserTagFlag.UserTag25);
            m_Tags.Add(UserTag26, UserTagFlag.UserTag26);
            m_Tags.Add(UserTag27, UserTagFlag.UserTag27);
            m_Tags.Add(UserTag28, UserTagFlag.UserTag28);
            m_Tags.Add(UserTag29, UserTagFlag.UserTag29);
            m_Tags.Add(UserTag30, UserTagFlag.UserTag30);
        }
    }
    public static class UserTag
    {
        public static UserTagFlag GetUserTag(string name) => CoreSystemSettings.Instance.m_UserTagNameModule.m_Tags[name];
        public static CustomTagFlag GetCustomTag(string name) => CoreSystemSettings.Instance.m_CustomTagNameModule.m_Tags[name];
        //public static UserTagFlag GetUserTag(int tag)
        //{
        //    if (tag == 31) return UserTagFlag.UserTag31;
        //    else if (tag == 32) return UserTagFlag.UserTag32;

        //    return SyadeuSettings.Instance.m_UserTagNameModule.m_Tags[tag.ToString()];
        //}
        //public static UserTagFlag GetUserTag(int idx)
        //{
        //    if (idx.Equals(31)) return UserTagFlag.UserTag31;
        //    else if (idx.Equals(32)) return UserTagFlag.UserTag32;

        //    foreach (var item in SyadeuSettings.Instance.m_UserTagNameModule.m_Tags)
        //    {
        //        if (item.Value.Equals(idx)) return item.Value;
        //    }
        //    return UserTagFlag.NONE;
        //}
    }
}
 