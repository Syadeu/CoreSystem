namespace Syadeu.Presentation
{
    public struct CoroutineJob
    {
        private readonly PresentationSystemID<CoroutineSystem> m_System;

        private readonly int m_Idx;
        internal int m_Generation;

        public int Index => m_Idx;
        public int Generation => m_Generation;

        public CoroutineJob(PresentationSystemID<CoroutineSystem> system, int index)
        {
            m_System = system;

            m_Idx = index;
            m_Generation = 0;
        }

        public void Stop()
        {
            m_System.System.StopCoroutineJob(this);
        }
    }
}
