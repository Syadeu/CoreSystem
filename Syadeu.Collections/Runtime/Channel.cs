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
using System;
using Unity.Collections;

namespace Syadeu
{
    [BurstCompatible]
    public struct Channel : IChannel, IEquatable<Channel>
    {
        public static Channel None => new Channel();
        public static Channel Audio => new Channel(LogChannel.Audio);
        public static Channel Collections => new Channel(LogChannel.Collections);
        public static Channel Core => new Channel(LogChannel.Core);
        public static Channel Editor => new Channel(LogChannel.Editor);

        private int m_LogChannel;
        //private FixedString512Bytes m_Name;

        public LogChannel LogChannel => (LogChannel)m_LogChannel;

#if UNITYENGINE && UNITY_COLLECTIONS
        [NotBurstCompatible]
#endif
        private Channel(LogChannel channel)
        {
            m_LogChannel = (int)channel;
            //m_Name = TypeHelper.Enum<LogChannel>.ToString(channel);
        }
#if UNITYENGINE && UNITY_COLLECTIONS
        [NotBurstCompatible]
#endif
        public override string ToString() => TypeHelper.Enum<LogChannel>.ToString(LogChannel);
        public override int GetHashCode() => m_LogChannel;

        public override bool Equals(object obj) => obj is Channel && Equals((Channel)obj);
        public bool Equals(Channel other) => m_LogChannel.Equals(other.m_LogChannel);

        public static implicit operator Channel(string t)
        {
            if (string.IsNullOrEmpty(t)) return None;

            LogChannel channel;
            try
            {
                channel = TypeHelper.Enum<LogChannel>.ToEnum(t);
            }
            catch (Exception)
            {
                channel = LogChannel.None;
            }
            return new Channel(channel);
        }
        public static implicit operator LogChannel(Channel t) => (LogChannel)t.m_LogChannel;
        public static implicit operator Channel(LogChannel t) => new Channel(t);

        public static Channel operator &(Channel a, Channel b)
        {
            LogChannel x = a, y = b;
            return new Channel(x & y);
        }
        public static bool operator ==(Channel a, Channel b) => a.Equals(b);
        public static bool operator !=(Channel a, Channel b) => !a.Equals(b);
    }
    public interface IChannel
    {
        LogChannel LogChannel { get; }
    }
}
