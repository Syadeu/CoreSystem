using Syadeu.Collections;
using System;
using System.Collections.Concurrent;

#if CORESYSTEM_FMOD
using FMOD;
using FMOD.Studio;
using FMODUnity;
#endif

namespace Syadeu.FMOD
{
#if CORESYSTEM_FMOD
    public class SoundGUID : IValidation
    {
        //public static SoundGUID Empty = new SoundGUID(false);

        public string EventPath { get; }

        public int Index { get; }
        public Guid GUID { get; private set; }
        public bool Preload { get; }

        public EventDescription EventDescription { get; private set; }
        public ConcurrentDictionary<string, PARAMETER_DESCRIPTION> Parameters { get; private set; }

        public bool Initialized { get; private set; }

        public SoundGUID(SoundList.FInput clip, bool initializeOnLoad = false)
        {
            if (clip.eventClips.Count < 1)
            {
                "SOUND ERROR :: 리스트에 아무 이벤트도 없는 이벤트 목록을 가지고있음".ToLogError();
                return;
            }

            EventPath = clip.eventClips[0].Event;

            Index = clip.index;
            Preload = clip.eventClips[0].Preload;

            if (initializeOnLoad)
            {
                if (RuntimeManager.StudioSystem.lookupID(clip.eventClips[0].Event, out var guid) != RESULT.OK)
                {
                    $"SOUND ERROR :: The event({clip.eventClips[0].Event}) is not valid.".ToLogError();
                }
                GUID = guid;

                EventDescription = RuntimeManager.GetEventDescription(guid);
                EventDescription.getParameterDescriptionCount(out int count);
                if (count > 0)
                {
                    Parameters = new ConcurrentDictionary<string, PARAMETER_DESCRIPTION>();
                    for (int i = 0; i < count; i++)
                    {
                        EventDescription.getParameterDescriptionByIndex(i, out var value);
                        Parameters.TryAdd(value.name, value);
                    }
                }
                else
                {
                    Parameters = null;
                }

                Initialized = true;
            }
            else
            {
                EventDescription = default;
                Parameters = null;
                Initialized = false;
            }
        }
        public SoundGUID(int index, string eventPath, bool preload, bool initializeOnLoad = false)
        {
            EventPath = eventPath;

            Index = index;
            Preload = preload;

            if (initializeOnLoad)
            {
                RuntimeManager.StudioSystem.lookupID(eventPath, out var guid);
                EventDescription = RuntimeManager.GetEventDescription(guid);
                EventDescription.getParameterDescriptionCount(out int count);
                if (count > 0)
                {
                    Parameters = new ConcurrentDictionary<string, PARAMETER_DESCRIPTION>();
                    for (int i = 0; i < count; i++)
                    {
                        EventDescription.getParameterDescriptionByIndex(i, out var value);
                        Parameters.TryAdd(value.name, value);
                    }
                }
                else
                {
                    Parameters = null;
                }

                Initialized = true;
            }
            else
            {
                EventDescription = default;
                Parameters = null;
                Initialized = false;
            }
        }

        public bool IsValid()
        {
            return Index > 0;
        }
        public void Initialize()
        {
            if (Initialized) return;
            if (RuntimeManager.StudioSystem.lookupID(EventPath, out var guid) != RESULT.OK)
            {
                $"SOUND ERROR :: The event({EventPath}) is not valid.".ToLogError();
            }
            GUID = guid;

            EventDescription = RuntimeManager.GetEventDescription(GUID);
            EventDescription.getParameterDescriptionCount(out int count);
            if (count > 0)
            {
                Parameters = new ConcurrentDictionary<string, PARAMETER_DESCRIPTION>();
                for (int i = 0; i < count; i++)
                {
                    EventDescription.getParameterDescriptionByIndex(i, out var value);
                    Parameters.TryAdd(value.name, value);
                }
            }
            else
            {
                Parameters = null;
            }

            Initialized = true;
            return;
        }
        public bool TryGetParameter(string name, out PARAMETER_DESCRIPTION parameter)
        {
            parameter = default;
            if (Parameters == null || !IsValid()) return false;

            return Parameters.TryGetValue(name, out parameter);
        }
    }
#endif
}