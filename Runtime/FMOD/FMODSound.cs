using Syadeu.ThreadSafe;
using Syadeu.Collections;

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

using UnityEngine;
using Syadeu.Mono;

#if CORESYSTEM_FMOD
using FMOD;
using FMOD.Studio;
using FMODUnity;
#endif

namespace Syadeu.FMOD
{
#if CORESYSTEM_FMOD
    public class FMODSound : RecycleableDatabase<FMODSound>
    {
    #region Initializer
        internal static ThreadSafe.Vector3 INIT_POSITION { get; } = new ThreadSafe.Vector3(999, 999, 999);
        
        /// <summary>
        /// 놀고있는 사운드 객체를 뽑아옵니다. 없으면 생성하여 반환합니다.
        /// </summary>
        /// <returns></returns>
        public static FMODSound GetSound()
        {
            FMODSound sound = GetDatabase();
            if (sound == null)
            {
                FMODSystem.Instance.CreateMemory(CoreSystemSettings.Instance.m_FMODMemoryBlock);
                CoreSystemSettings.Instance.m_FMODMemoryBlock *= 2;
                //if (InstanceCount < m_MemoryBlock)
                //{
                //    sound = new FMODSound();
                //}
                //else
                //{
                $"CoreSystem.FMOD :: Sound is reached maximum instance count => {CoreSystemSettings.Instance.m_FMODMemoryBlock}".ToLog();
                //    sound = GetDatabase(Playlist[10].DataIndex);
                //}
                sound = GetDatabase();
            }
            if (sound == null)
            {
                "FATAL ERROR :: SOUND IS NULL".ToLogError();
                return null;
            }

            return sound;
        }
        protected override void OnInitialize()
        {
            if (IsPlaying)
            {
                Stop();
            }

            SoundGUID = null;

            Transform = null;
            Rigidbody = null;
            Position = INIT_POSITION;
            FMODInstance = default;

            EventCallback = null;
            Is3D = false;
            IsVailded = false;
            IsObject = false;
            IsListed = false;

            Paused = false;
        }
        protected override void OnTerminate()
        {
            if (FMODInstance.hasHandle())
            {
                FMODInstance.release();
                FMODInstance.clearHandle();
            }

            base.OnTerminate();
        }

    #endregion

        public SoundGUID SoundGUID { get; private set; }

        public Transform Transform { get; private set; }
        public Rigidbody Rigidbody { get; private set; }
        private ThreadSafe.Vector3 m_Position;
        public ThreadSafe.Vector3 Position
        {
            get
            {
                if (IsObject)
                {
                    if (Transform == null) return INIT_POSITION;
                    else
                    {
                        return CoreSystem.GetPosition(Transform).ToThreadSafe();
                    }
                }
                else
                {
                    return m_Position;
                }
            }
            private set { m_Position = value; }
        }
        public EventInstance FMODInstance { get; private set; }

        protected internal EVENT_CALLBACK EventCallback { get; set; }
        /// <summary>
        /// 3D 사운드인지 반환합니다.
        /// </summary>
        protected internal bool Is3D { get; private set; }
        /// <summary>
        /// 재생이 가능한 사운드인지 반환합니다.
        /// </summary>
        protected internal bool IsVailded { get; internal set; } = false;
        /// <summary>
        /// Vector3로 포지션값이 지정된게 아니고 Transfrom으로 지정된 사운드인지 반환합니다
        /// </summary>
        protected internal bool IsObject { get; internal set; } = false;
        /// <summary>
        /// Playlist에 등록되어있는지 반환합니다.
        /// </summary>
        protected internal bool IsListed { get; internal set; } = false;

        public bool Paused { get; private set; } = false;

        public event Action OnPlay;
        public event Action OnStop;

    #region Utils

        /// <summary>
        /// 현재 재생 중인지 반환합니다.
        /// </summary>
        public bool IsPlaying
        {
            get
            {
                if (FMODInstance.isValid())
                {
                    FMODInstance.getPlaybackState(out PLAYBACK_STATE state);
                    return state != PLAYBACK_STATE.STOPPED;
                }
                return false;
            }
        }

    #endregion

        public void Play()
        {
            //if (!ValidCheck() || IsPlaying) return;

            //if (Is3D)
            //{
            //    if (Position == INIT_POSITION)
            //    {
            //        SetPosition(FMODSystem.Instance.transform.position);
            //    }
            //}

            FMODSystem.Instance.WaitForPlay.Enqueue(this);
        }
        public virtual void Pause()
        {
            if (FMODInstance.isValid())
            {
                FMODInstance.setPaused(true);
                Paused = true;
            }
        }
        public virtual void Restart()
        {
            if (FMODInstance.isValid())
            {
                FMODInstance.setPaused(false);
                Paused = false;
            }
        }
        public virtual void Stop()
        {
            if (FMODInstance.isValid())
            {
                OnStop?.Invoke();
                FMODInstance.stop(global::FMOD.Studio.STOP_MODE.ALLOWFADEOUT);

                //FMODInstance.release();
                //FMODInstance.clearHandle();

                Terminate();
            }
        }

        internal void InternalPlay()
        {
            FMODInstance.start();
            OnPlay?.Invoke();
        }
        internal void InternalStop()
        {
            OnStop?.Invoke();
            Terminate();
        }

        public static SoundGUID GetEventPath(int listIndex, int soundIndex)
        {
            if (!FMODSystem.GetSoundList(listIndex, out var list)) return null;

            SoundGUID input = null;
            for (int i = 0; i < list.Sounds.Length; i++)
            {
                if (list.Sounds[i].Index == soundIndex)
                {
                    input = list.Sounds[i];
                    break;
                }
            }

            return input;
        }
        public bool SetEventPath(int listIndex, int soundIndex)
        {
            SoundGUID input = GetEventPath(listIndex, soundIndex);

            if (input == null)
            {
                $"SOUND ERROR :: 요청한 사운드를 찾을 수 없음\nIndex :: {soundIndex} ListType :: {listIndex}".ToLog();
                return false;
            }

            SoundGUID = input;
            return true;
        }
        public FMODSound SetEventPath(SoundGUID target)
        {
            SoundGUID = target;
            return this;
        }
        public bool SetEventPath(string path)
        {
            SoundGUID = new SoundGUID(99, path, false);
            return true;
        }

    #region Sound Settings

        public FMODSound SetPosition(Transform transform)
        {
            if (transform == null)
            {
                $"SOUND EXCEPTION :: Target transform is null, this request will be ignored".ToLog();
                return this;
            }

            if (ValidCheck())
            {
                if (Is3D)
                {
                    Rigidbody = transform.GetComponent<Rigidbody>();
                    if (FMODInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform, Rigidbody)) != global::FMOD.RESULT.OK)
                    {
                        $"SOUND EXCEPTION :: Positioning error, Event: {SoundGUID}".ToLog();
                    }

                    //FMODSystem.AttachInstanceToGameObject(this, transform, rid);

                    Transform = transform;
                    IsObject = true;
                }

                //isPositionSet = true;
            }

            return this;
        }
        public FMODSound SetPosition(UnityEngine.Vector3 position)
        {
            if (ValidCheck())
            {
                if (Is3D)
                {
                    if (FMODInstance.set3DAttributes(position.To3DAttributes()) != global::FMOD.RESULT.OK)
                    {
                        $"SOUND EXCEPTION :: Positioning error, Event: {SoundGUID}".ToLog();
                    }
                }

                Position = position.ToThreadSafe();
                //isPositionSet = true;
            }

            return this;
        }
        public PARAMETER_DESCRIPTION GetParameter(string name)
        {
            SoundGUID.TryGetParameter(name, out var parameter);
            return parameter;
        }
        public FMODSound SetParameter(string name, float value)
        {
            if (ValidCheck() && SoundGUID.TryGetParameter(name, out var parameter))
            {
                var result = FMODInstance.setParameterByID(parameter.id, value);
                if (result != global::FMOD.RESULT.OK)
                {
                    $"SOUND EXCEPTION :: The parameter {name} has raised an error at ({SoundGUID}).\nTrace: {result}, Name: {name}, Value: {value}".ToLog();
                }
            }

            return this;
        }
        public FMODSound SetParameter(PARAMETER_ID id, float value, bool ignoreSpeed = false)
        {
            if (ValidCheck())
            {
                var result = FMODInstance.setParameterByID(id, value, ignoreSpeed);
                if (result != global::FMOD.RESULT.OK)
                {
                    $"SOUND EXCEPTION :: The parameter {id} has raised an error at ({SoundGUID}).\nTrace: {result}, ID: {id}, Value: {value}".ToLog();
                }
            }

            return this;
        }
        public FMODSound SetOcclusion(float direct)
        {
            if (ValidCheck())
            {
                var result = FMODInstance.setParameterByName("Occlusion", direct);
                if (result != global::FMOD.RESULT.OK)
                {
                    direct = Mathf.Abs(direct - 1);
                    FMODInstance.setVolume(direct);
                }
            }
            //SetParameter("Occlusion", direct);
            return this;
        }
        public FMODSound SetTrigger()
        {
            if (FMODInstance.isValid())
            {
                FMODInstance.triggerCue();
            }

            return this;
        }

    #endregion

    #region Checks

        protected internal bool ValidCheck()
        {
            if (IsVailded /*|| instance.isValid()*/) return true;
            if (SoundGUID == null || !SoundGUID.IsValid())
            {
                "FMOD Event path is not set.".ToLogError();
                return false;
            }
            if (!SoundGUID.Initialized) SoundGUID.Initialize();

            SoundGUID.EventDescription.is3D(out var is3D);
            Is3D = is3D;
            if (!FMODInstance.isValid())
            {
                FMODInstance.clearHandle();
                SoundGUID.EventDescription.createInstance(out var instance);
                FMODInstance = instance;
            }

            if (!FMODInstance.hasHandle())
            {
                $"SOUND ERROR :: Fmod handle is null".ToLogError();
                return false;
            }

            IsVailded = true;
            return true;
        }

    #endregion
    }
#endif
}
