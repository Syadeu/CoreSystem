using Syadeu.Collections;

namespace Syadeu.FMOD
{
#if CORESYSTEM_FMOD
    public readonly struct SoundListGUID : IValidation
    {
        public int Index { get; }
        public SoundGUID[] Sounds { get; }

        public SoundListGUID(SoundList list)
        {
            Index = list.listIndex;
            Sounds = new SoundGUID[list.fSounds.Count];

            for (int i = 0; i < list.fSounds.Count; i++)
            {
                Sounds[i] = new SoundGUID(list.fSounds[i]);
            }
        }

        public SoundListGUID InitializeAt(int index)
        {
            Sounds[index].Initialize();

            return this;
        }
        public SoundListGUID InitializeAll()
        {
            for (int i = 0; i < Sounds.Length; i++)
            {
                Sounds[i].Initialize();
            }

            return this;
        }

        public bool IsValid()
        {
            return Sounds != null;
        }
    }
#endif
}