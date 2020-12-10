using FMOD.Studio;
using FMODUnity;
using Syadeu.Extentions.EditorUtils;
using Syadeu.ThreadSafe;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace Syadeu.FMOD
{
    public class FMODSound : RecycleableDatabase<FMODSound>
    {
        #region Initializer
        private static ThreadSafe.Vector3 INIT_POSITION { get; } = new ThreadSafe.Vector3(999, 999, 999);
        public static List<FMODSound> Playlist { get; } = new List<FMODSound>();

        private static readonly ConcurrentQueue<FMODSound> WaitForPlay = new ConcurrentQueue<FMODSound>();

        static FMODSound()
        {
            CreateMemory(m_MemoryBlock);

            CoreSystem.Instance.StartUnityUpdate(UnityUpdater());
        }

        private static IEnumerator UnityUpdater()
        {
            while (true)
            {
                if (WaitForPlay.Count > 0)
                {
                    int waitforplayCount = WaitForPlay.Count;
                    for (int i = 0; i < waitforplayCount; i++)
                    {
                        if (WaitForPlay.TryDequeue(out FMODSound sound))
                        {
                            if (!sound.ValidCheck() || sound.IsPlaying) continue;

                            if (sound.Is3D)
                            {
                                if (sound.Position == INIT_POSITION)
                                {
                                    sound.SetPosition(FMODSystem.Instance.transform.position);
                                }
                            }

                            sound.FMODInstance.start();
                            Playlist.Add(sound);
                            sound.IsListed = true;

                            sound.OnPlay?.Invoke();
                        }
                    }
                }

                for (int i = 0; i < Playlist.Count; i++)
                {
                    if (!Playlist[i].Activated)
                    {
                        Playlist.RemoveAt(i);
                        i--;
                        continue;
                    }

                    PLAYBACK_STATE playbackState;
                    if (!Playlist[i].FMODInstance.isValid())
                    {
                        Playlist[i].Terminate();
                        Playlist.RemoveAt(i);
                        i--;
                        continue;
                    }
                    Playlist[i].FMODInstance.getPlaybackState(out playbackState);
                    if (playbackState == PLAYBACK_STATE.STOPPED)
                    {
                        Playlist[i].OnStop?.Invoke();
                        Playlist[i].Terminate();
                        Playlist.RemoveAt(i);
                        i--;
                        continue;
                    }

                    if (Playlist[i].IsObject)
                    {
                        if (Playlist[i].Transform == null)
                        {
                            Playlist[i].OnStop?.Invoke();

                            if (Playlist[i].FMODInstance.isValid()) Playlist[i].FMODInstance.stop(global::FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                            Playlist[i].Terminate();
                            Playlist.RemoveAt(i);
                            i--;
                            continue;
                        }

                        Playlist[i].FMODInstance.set3DAttributes(RuntimeUtils.To3DAttributes(Playlist[i].Transform, Playlist[i].Rigidbody));
                    }
                }
                yield return null;
            }
        }

        private static int m_MemoryBlock = 512;
        private static void CreateMemory(int count)
        {
            for (int i = 0; i < count; i++)
            {
                FMODSound sound = new FMODSound();
                sound.Terminate();
            }
        }
        /// <summary>
        /// 놀고있는 사운드 객체를 뽑아옵니다. 없으면 생성하여 반환합니다.
        /// </summary>
        /// <returns></returns>
        public static FMODSound GetSound()
        {
            FMODSound sound = GetDatabase();
            if (sound == null)
            {
                CreateMemory(m_MemoryBlock);
                m_MemoryBlock *= 2;
                //if (InstanceCount < m_MemoryBlock)
                //{
                //    sound = new FMODSound();
                //}
                //else
                //{
                $"Sound is reached maximum instance count => {m_MemoryBlock}".ToLog();
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
                    //if (Transform == null) return m_Position;
                    //else
                    {
                        return Transform.position.ToThreadSafe();
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

        protected EVENT_CALLBACK EventCallback { get; set; }
        /// <summary>
        /// 3D 사운드인지 반환합니다.
        /// </summary>
        protected bool Is3D { get; private set; }
        /// <summary>
        /// 재생이 가능한 사운드인지 반환합니다.
        /// </summary>
        protected bool IsVailded { get; private set; } = false;
        /// <summary>
        /// Vector3로 포지션값이 지정된게 아니고 Transfrom으로 지정된 사운드인지 반환합니다
        /// </summary>
        protected bool IsObject { get; private set; } = false;
        /// <summary>
        /// Playlist에 등록되어있는지 반환합니다.
        /// </summary>
        protected bool IsListed { get; private set; } = false;

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

            WaitForPlay.Enqueue(this);
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

        protected bool ValidCheck()
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
}
