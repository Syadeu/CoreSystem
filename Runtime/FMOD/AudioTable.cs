namespace Syadeu.FMOD
{
    [System.Serializable]
    public struct AudioTable : IAudioTable
    {
        public string m_Name;
        public string m_Guid;
        public int m_Index;

        System.Guid IAudioTable.Guid => System.Guid.Parse(m_Guid);
        int IAudioTable.Index => m_Index;
        string IAudioTable.Name => m_Name;
    }
}