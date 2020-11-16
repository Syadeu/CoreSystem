using FMOD;
using FMOD.Studio;
using FMODUnity;
using Syadeu.Extentions.EditorUtils;
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
        }

        private Language m_CurrentLanguage;

        private static Bus m_MasterBus;

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
            //SyadeuSystem.OnBackgroundAsyncUpdate += OnBackgroundAsyncUpdate;
        }

        private void OnUnityStart()
        {
            FMODStudioSystem.getBus("bus:/", out m_MasterBus);

            SubListener = Instance.gameObject.AddComponent<FMODListener>();
            MainListener = SubListener;
            FMODStudioSystem.setListenerWeight(MainListener.Index, 1);

            //CurrentLanguage = Language._ko_kr;
            //SetLocale(CurrentLanguage);
        }
        private void OnUnityUpdate()
        {
            #region Out Focus Mute
#if !UNITY_EDITOR
            m_MasterBus.getVolume(out float vol);
            m_MasterBus.setVolume(Mathf.Lerp(vol, Instance.IsFocused ? 1 : 0, Time.deltaTime * 5));
#endif
            #endregion
        }
        private static IEnumerator OnBackgroundAsyncUpdate(/*SyadeuSystem.Awaiter awaiter*/)
        {
            while (true)
            {
                for (int i = 0; i < FMODSound.InstanceCount; i++)
                {
                    FMODSound sound = FMODSound.GetInstance(i);
                    if (!sound.Activated) continue;
                    //awaiter(1);

                    //if (!Playlist[i].IsPlaying() || Playlist[i].Disposed)
                    //{
                    //    if (!Playlist[i].Disposed) Playlist[i].Dispose();
                    //    Playlist.RemoveAt(i);
                    //    i--;
                    //    continue;
                    //}

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

        #endregion

        #region Exposed

        public global::FMOD.Studio.System FMODStudioSystem => RuntimeManager.StudioSystem;
        public global::FMOD.System FMODCoreSystem => RuntimeManager.CoreSystem;

        //public static List<CTFmodEntity> Playlist { get; private set; } = new List<CTFmodEntity>();
        public static List<EventDescription> PreloadedSamples { get; set; } = new List<EventDescription>();

        public static ConcurrentDictionary<int, SoundListGUID> SoundLists { get; } = new ConcurrentDictionary<int, SoundListGUID>();
        public static ConcurrentDictionary<int, SoundRoom> SoundRooms { get; } = new ConcurrentDictionary<int, SoundRoom>();

        public static SoundRoom CurrentListenerRoom { get; set; } = SoundRoom.Null;
        public static FMODListener MainListener { get; set; }
        public static FMODListener SubListener { get; set; }

        public static Language CurrentLanguage { get { return Instance.m_CurrentLanguage; } }

        #endregion

        #region Methods

        public static bool GetSoundList(int listIndex, out SoundListGUID list)
        {
            if (!SoundLists.TryGetValue(listIndex, out list))
            {
                $"SOUND ERROR :: 지정된 사운드 리스트를 찾을 수 없음\nListIndex :: {listIndex}".ToLogError();
                return false;
            }
            return true;
        }

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

        public static bool SetListener(FMODListener listener, GameObject attenuationObj = null)
        {
            if (MainListener == null) MainListener = listener;
            else
            {
                SubListener = MainListener;
                MainListener = listener;
                RESULT result = Instance.FMODStudioSystem.setListenerWeight(MainListener.Index, 1);
                if (result != RESULT.OK)
                {
                    $"SOUND EXCEPTION :: Listener가 제대로 설정되지 않음\nTrace: {result}".ToLog();
                    return false;
                }
            }

            MainListener.attenuationObject = attenuationObj;

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

        // Current listener position.
        private static VECTOR listenerPositionFmod = new VECTOR();
        /// Returns whether the listener is currently inside the given |room| boundaries.
        public static bool IsListenerInsideRoom(Transform room)
        {
            // Compute the room position relative to the listener.
            VECTOR unused;
            RuntimeManager.CoreSystem.get3DListenerAttributes(MainListener.Index, out listenerPositionFmod, out unused, out unused, out unused);
            Vector3 listenerPosition = ToNormalVector(listenerPositionFmod);
            Vector3 relativePosition = listenerPosition - room.position;
            Quaternion rotationInverse = Quaternion.Inverse(room.rotation);

            // Boundaries instance to be used in room detection logic.
            Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);

            // Set the size of the room as the boundary and return whether the listener is inside.
            bounds.size = Vector3.Scale(room.transform.lossyScale, room.transform.localScale);
            return bounds.Contains(rotationInverse * relativePosition);
        }

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

        public const int MaxSoundCount = 999;
        public static bool GetSound(out FMODSound sound)
        {
            sound = FMODSound.GetDatabase();
            if (sound == null)
            {
                if (FMODSound.InstanceCount < MaxSoundCount)
                {
                    sound = new FMODSound();
                }
                else
                {
                    "Sound is reached maximum instance count".ToLog();
                    return false;
                }
            }
            return true;
        }
        public static bool Play(FMODSound sound, Transform targetPos = null)
        {
            if (targetPos != null)
            {
                sound.SetPosition(targetPos);
            }
            sound.Play();
            return true;
        }
        public static bool Play(int listIndex, int soundIndex, Transform targetPos = null)
        {
            if (!GetSound(out var sound)) return false;

            if (!sound.SetEventPath(listIndex, soundIndex))
            {
                return false;
            }

            return Play(sound, targetPos);
        }

        #endregion
    }
}
