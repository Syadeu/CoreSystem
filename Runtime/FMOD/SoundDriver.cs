using Syadeu.Collections;
using System;

#if CORESYSTEM_FMOD
using FMOD;
using FMOD.Studio;
using FMODUnity;
#endif

namespace Syadeu.FMOD
{
#if CORESYSTEM_FMOD
    public readonly struct SoundDriver : IEquatable<SoundDriver>, IValidation
    {
        /// <summary>
        /// 시스템상의 출력장치 인덱스 번호
        /// </summary>
        public int Index { get; }
        /// <summary>
        /// 시스템상의 이름
        /// </summary>
        public string Name { get; }
        public Guid Guid { get; }
        /// <summary>
        /// Samplerate that current using in this driver
        /// </summary>
        public int SystemRate { get; }
        public SPEAKERMODE SpeakerMode { get; }
        /// <summary>
        /// This speaker's channel count
        /// </summary>
        public int SpeakerModeChannels { get; }

        public SoundDriver(int index, string name, Guid guid, int sysRate, SPEAKERMODE mode, int modeChannels)
        {
            Index = index;
            Name = name;
            Guid = guid;
            SystemRate = sysRate;
            SpeakerMode = mode;
            SpeakerModeChannels = modeChannels;
        }

        public bool IsValid()
        {
            var driverList = FMODSystem.GetAudioDevices();
            for (int i = 0; i < driverList.Count; i++)
            {
                if (driverList[i].Equals(this))
                {
                    return true;
                }
            }

            return false;
        }
        public static implicit operator SoundDriver(int driverIndex)
        {
            var driverList = FMODSystem.GetAudioDevices();
            for (int i = 0; i < driverList.Count; i++)
            {
                if (driverList[i].Index == driverIndex)
                {
                    return driverList[i];
                }
            }

            throw new Exception("해당 인덱스의 사운드 출력장치는 존재하지 않음");
        }
        public static implicit operator string(SoundDriver val) => val.Name;
        public static implicit operator int(SoundDriver val) => val.Index;
        public override string ToString() => Name;

        public bool Equals(SoundDriver other)
        {
            return Index == other.Index && Guid == other.Guid;
        }
    }
#endif
}
