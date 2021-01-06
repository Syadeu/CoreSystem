using FMOD;
using FMOD.Studio;
using FMODUnity;
using Syadeu.Extentions.EditorUtils;
using Syadeu.Mono;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace Syadeu.FMOD
{
    /// <summary>
    /// FMOD 메인 객체 시스템 사운드 컨트롤러입니다.
    /// </summary>
    public sealed class FMODSystem : StaticManager<FMODSystem>
    {
        #region INIT

        public enum Language
        {
            _ko_kr,
            _en_us,
        }

        private Bus m_MasterBus;
        private Language m_CurrentLanguage;

        internal ConcurrentQueue<FMODSound> WaitForPlay { get; } = new ConcurrentQueue<FMODSound>();
        internal List<FMODSound> Playlist { get; } = new List<FMODSound>();

        public bool IsFocused { get; private set; } = true;

        public override void OnInitialize()
        {
            if (!IsBankLoaded("Master"))
            {
                LoadBank("Master");
            }
            if (!IsBankLoaded("Master.strings"))
            {
                LoadBank("Master.strings");
            }

            for (int i = 0; i < FMODSettings.SoundLists.Count; i++)
            {
                if (!SoundLists.TryAdd(FMODSettings.SoundLists[i].listIndex, new SoundListGUID(FMODSettings.SoundLists[i])))
                {
                    $"SOUND EXCEPTION :: Failed to add list {FMODSettings.SoundLists[i].listName}".ToLog();
                }
            }

            SoundRooms.Clear();
            for (int i = 0; i < FMODSettings.SoundRooms.Count; i++)
            {
                FMODSettings.SoundRooms[i] = FMODSettings.SoundRooms[i].Initialize(i);
                SoundRooms.TryAdd(i, FMODSettings.SoundRooms[i]);
            }

            CoreSystem.OnUnityStart += OnUnityStart;
            CoreSystem.OnUnityUpdate += OnUnityUpdate;

            StartBackgroundUpdate(OnBackgroundAsyncUpdate());
            StartUnityUpdate(OnUnityCustomUpdater());
        }

        private void OnUnityStart()
        {
            CreateMemory(SyadeuSettings.Instance.m_MemoryBlock);

            FMODStudioSystem.getBus("bus:/", out m_MasterBus);

            MainListener = Instance.gameObject.AddComponent<FMODListener>();
            FMODStudioSystem.setListenerWeight(MainListener.Index, 1);
        }
        private void OnUnityUpdate()
        {
            #region Out Focus Mute
#if !UNITY_EDITOR
            m_MasterBus.getVolume(out float vol);
            m_MasterBus.setVolume(Mathf.Lerp(vol, Instance.IsFocused ? 1 : 0, Time.deltaTime * 5));
#endif
            #endregion

            if (MainListenerTarget != null)
            {
                MainListener.transform.position = Vector3.Lerp(MainListener.transform.position, MainListenerTarget.position, Time.deltaTime * 3f);
            }
        }
        private IEnumerator OnUnityCustomUpdater()
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
                                if (sound.Position == FMODSound.INIT_POSITION)
                                {
                                    if (!sound.IsObject)
                                    {
                                        sound.SetPosition(FMODSystem.Instance.transform.position);
                                    }
                                    else
                                    {
                                        // 실행요청을 받았는데 오브젝트가 사라졌다?
                                        continue;
                                    }
                                }
                            }

                            //sound.FMODInstance.start();
                            Playlist.Add(sound);
                            sound.IsListed = true;
                            sound.InternalPlay();
                            //sound.OnPlay?.Invoke();
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
                        //Playlist[i].OnStop?.Invoke();
                        //Playlist[i].Terminate();
                        Playlist[i].InternalStop();
                        Playlist.RemoveAt(i);
                        i--;
                        continue;
                    }

                    if (Playlist[i].IsObject)
                    {
                        if (Playlist[i].Transform == null)
                        {
                            //Playlist[i].OnStop?.Invoke();
                            Playlist[i].InternalStop();

                            if (Playlist[i].FMODInstance.isValid()) Playlist[i].FMODInstance.stop(global::FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                            //Playlist[i].Terminate();
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
        private IEnumerator OnBackgroundAsyncUpdate(/*SyadeuSystem.Awaiter awaiter*/)
        {
#if UNITY_EDITOR
            UnityEngine.Profiling.CustomSampler fmodUpdateSampler = UnityEngine.Profiling.CustomSampler.Create("FMODSystem Background");
#endif

            while (true)
            {
#if UNITY_EDITOR
                fmodUpdateSampler.Begin();
#endif
                for (int i = 0; i < FMODSound.InstanceCount; i++)
                {
                    FMODSound sound = FMODSound.GetInstance(i);
                    if (!sound.Activated) continue;

                    if (SoundRooms.Count > 0 && MainListener != null)
                    {
                        bool temp = false;
                        foreach (var room in SoundRooms.Values)
                        {
                            if (room.IsValid() && room.Contains(MainListener.Position))
                            {
                                if (CurrentListenerRoom != room) CurrentListenerRoom = room;

                                temp = true;
                                break;
                            }
                        }
                        if (!temp)
                        {
                            if (CurrentListenerRoom != SoundRoom.Null)
                            {
                                CurrentListenerRoom = SoundRoom.Null;
                            }
                        }
                        else
                        {
                            if (CurrentListenerRoom.IsValid() && !CurrentListenerRoom.Contains(sound.Position))
                            {
                                sound.SetOcclusion(CurrentListenerRoom.Direct);
                            }
                        }
                    }
                }

#if UNITY_EDITOR
                fmodUpdateSampler.End();
#endif
                yield return null;
            }

        }

        private void OnApplicationFocus(bool focus)
        {
            IsFocused = focus;
        }
        private void OnApplicationQuit()
        {
            for (int i = 0; i < PreloadedSamples.Count; i++)
            {
                PreloadedSamples[i].unloadSampleData();
            }
        }

        internal void CreateMemory(int count)
        {
            for (int i = 0; i < count; i++)
            {
                FMODSound sound = new FMODSound();
                sound.Terminate();
            }
        }

        #endregion

        #region Exposed

        public global::FMOD.Studio.System FMODStudioSystem => RuntimeManager.StudioSystem;
        public global::FMOD.System FMODCoreSystem => RuntimeManager.CoreSystem;

        private List<EventDescription> PreloadedSamples { get; set; } = new List<EventDescription>();

        public ConcurrentDictionary<int, SoundListGUID> SoundLists { get; } = new ConcurrentDictionary<int, SoundListGUID>();
        public ConcurrentDictionary<int, SoundRoom> SoundRooms { get; } = new ConcurrentDictionary<int, SoundRoom>();

        public SoundRoom CurrentListenerRoom { get; private set; } = SoundRoom.Null;
        public FMODListener MainListener { get; private set; }
        public Transform MainListenerTarget { get; private set; }

        public static Language CurrentLanguage { get { return Instance.m_CurrentLanguage; } }

        #endregion

        #region Methods

        public static IReadOnlyList<FMODSound> GetPlayList() => Instance.Playlist;

        /// <summary>
        /// 해당 인덱스의 사운드 리스트를 가져옵니다.
        /// </summary>
        /// <param name="listIndex"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public static bool GetSoundList(int listIndex, out SoundListGUID list)
        {
            if (!Instance.SoundLists.TryGetValue(listIndex, out list))
            {
                $"SOUND ERROR :: 지정된 사운드 리스트를 찾을 수 없음\nListIndex :: {listIndex}".ToLogError();
                return false;
            }
            return true;
        }
        /// <summary>
        /// 로컬라이징을 위해 분리한 뱅크들을 해당 언어로 교체합니다
        /// </summary>
        /// <param name="language"></param>
        /// <returns></returns>
        public static bool SetLocale(Language language)
        {
            if (FMODSettings.LocalizedBankNames == null) return false;

            if (CurrentLanguage != language)
            {
                foreach (var name in FMODSettings.LocalizedBankNames)
                {
                    UnloadBank(name + CurrentLanguage.ToString());
                }
            }

            foreach (var name in FMODSettings.LocalizedBankNames)
            {
                if (!LoadBank(name + language.ToString()))
                {
                    return false;
                }
            }

            Instance.m_CurrentLanguage = language;
            return true;
        }

        #region FMOD Functions

        public static RESULT GetParameterDescriptionByName(string name, out PARAMETER_DESCRIPTION value) => Instance.FMODStudioSystem.getParameterDescriptionByName(name, out value);
        public static RESULT GetParameterByID(PARAMETER_ID id, out float value) => Instance.FMODStudioSystem.getParameterByID(id, out value);

        public static bool SetListenerTarget(Transform target, GameObject attenuationObj = null)
        {
            Instance.MainListenerTarget = target;
            Instance.MainListener.attenuationObject = attenuationObj;

            return true;
        }

        /// <summary>
        /// 이 시스템의 현재 사용가능한 오디오 출력장치를 반환합니다.
        /// </summary>
        /// <returns></returns>
        public static List<SoundDriver> GetAudioDevices()
        {
            //FMODCoreSystem.update();
            List<SoundDriver> soundDrivers = new List<SoundDriver>();
            Instance.FMODCoreSystem.getNumDrivers(out int count);

            for (int i = 0; i < count; i++)
            {
                Instance.FMODCoreSystem.getDriverInfo(i, out string name, 999, out Guid guid, out int systemrate, out SPEAKERMODE speakermode, out int speakermodechannels);
                soundDrivers.Add(new SoundDriver(i, name, guid, systemrate, speakermode, speakermodechannels));
            }

            return soundDrivers;
        }
        /// <summary>
        /// 현재 사용하는 오디오 출력장치를 반환합니다.<br/>
        /// </summary>
        /// <returns></returns>
        public static SoundDriver GetAudioDevice()
        {
            //FMODCoreSystem.update();
            Instance.FMODCoreSystem.getDriver(out int driver);
            Instance.FMODCoreSystem.getDriverInfo(driver, out string name, 999, out Guid guid, out int systemrate, out SPEAKERMODE mod, out int channels);

            return new SoundDriver(driver, name, guid, systemrate, mod, channels);
        }
        /// <summary>
        /// 해당 출력장치로 설정합니다.
        /// </summary>
        /// <returns></returns>
        public static bool SetAudioDevice(int driverIndex)
        {
            Instance.FMODCoreSystem.getDriver(out int driver);
            if (driver == driverIndex) return true;

            return Instance.FMODCoreSystem.setDriver(driverIndex) == RESULT.OK;
        }

        //// Current listener position.
        //private static VECTOR listenerPositionFmod = new VECTOR();
        ///// Returns whether the listener is currently inside the given |room| boundaries.
        //public static bool IsListenerInsideRoom(Transform room)
        //{
        //    // Compute the room position relative to the listener.
        //    VECTOR unused;
        //    RuntimeManager.CoreSystem.get3DListenerAttributes(MainListener.Index, out listenerPositionFmod, out unused, out unused, out unused);
        //    Vector3 listenerPosition = ToNormalVector(listenerPositionFmod);
        //    Vector3 relativePosition = listenerPosition - room.position;
        //    Quaternion rotationInverse = Quaternion.Inverse(room.rotation);

        //    // Boundaries instance to be used in room detection logic.
        //    Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);

        //    // Set the size of the room as the boundary and return whether the listener is inside.
        //    bounds.size = Vector3.Scale(room.transform.lossyScale, room.transform.localScale);
        //    return bounds.Contains(rotationInverse * relativePosition);
        //}

        public static VECTOR ToFMODVector(Vector3 vector3) => RuntimeUtils.ToFMODVector(vector3);
        public static Vector3 ToNormalVector(VECTOR vec) => new Vector3(vec.x, vec.y, vec.z);

        public static bool SetGlobalParam(PARAMETER_DESCRIPTION param, float value, bool ignoreSpeed = false)
        {
            var result = Instance.FMODStudioSystem.setParameterByID(param.id, value, ignoreSpeed);
            if (result != RESULT.OK)
            {
                $"SOUND EXCEPTION :: The parameter {param.name} has raised an error at (Global parameter).\nTrace: {result}, Name: {param.name}, Value: {value}".ToLog();
            }
            return result == RESULT.OK;
        }
        public static bool GetGlobalParam(PARAMETER_DESCRIPTION param, out float value)
        {
            var result = Instance.FMODStudioSystem.getParameterByID(param.id, out value);
            if (result != RESULT.OK)
            {
                $"SOUND EXCEPTION :: The parameter {param.name} has raised an error at (Global parameter).\nTrace: {result}, Name: {param.name}, Value: {value}".ToLog();
            }
            return result == RESULT.OK;
        }
        public static bool LoadBank(string name)
        {
            try
            {
                if (!RuntimeManager.HasBankLoaded(name))
                {
                    //$"SOUND LOG :: Loading Bank {name}".ToLog();
                    RuntimeManager.LoadBank(name, true);
                }
            }
            catch (BankLoadException e)
            {
                UnityEngine.Debug.LogException(e);
                return false;
            }

            RuntimeManager.WaitForAllLoads();
            return true;
        }
        public static void UnloadBank(string name)
        {
            //$"SOUND LOG :: Unloading Bank {name}".ToLog();
            RuntimeManager.UnloadBank(name);
        }
        public static bool IsBankLoaded(string name) => RuntimeManager.HasBankLoaded(name);

        #endregion

        /// <summary>
        /// 해당 사운드를 재생합니다
        /// </summary>
        /// <param name="listIndex"></param>
        /// <param name="soundIndex"></param>
        /// <param name="targetPos"></param>
        /// <returns></returns>
        public static bool Play(int listIndex, int soundIndex, Transform targetPos = null)
        {
            FMODSound sound = FMODSound.GetSound();

            if (!sound.SetEventPath(listIndex, soundIndex))
            {
                return false;
            }

            if (targetPos != null)
            {
                sound.SetPosition(targetPos);
            }
            sound.Play();
            return true;
        }

        #endregion
    }
}
