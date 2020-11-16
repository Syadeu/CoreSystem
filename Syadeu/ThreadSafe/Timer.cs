using Syadeu.Extentions.EditorUtils;
using System;

namespace Syadeu
{
    public delegate void TimerAction();
    public delegate bool TimerCondition();
    public sealed class Timer : IDisposable
    {
        public static int InstanceCount { get; private set; } = 0;

        internal readonly int m_Hashcode;

        public TimeSpan StartTime { get; set; }
        internal long TargetedTime { get; set; }
        internal TimeSpan TargetTime { get; set; }

        internal TimerAction TimerStartAction { get; set; }
        internal TimerAction TimerKillAction { get; set; }
        internal TimerAction TimerEndAction { get; set; }
        internal TimerAction TimerUpdateAction { get; set; }

        internal bool Disposed { get; set; } = false;

        internal bool Activated { get; set; } = false;
        internal bool Completed { get; set; } = false;
        internal bool Killed { get; set; } = false;

        internal bool Started { get; set; } = false;

        public Timer()
        {
            m_Hashcode = InstanceCount;
            InstanceCount += 1;
        }

        public override int GetHashCode()
        {
            return m_Hashcode;
        }

        public bool IsTimerActive() => Activated || Started;
        public bool IsTimerComplete() => Completed;

        public Timer SetTargetTime(long second)
        {
            //TargetTime = second;
            TargetedTime = second * 10000000;
            return this;
        }
        public Timer OnTimerStart(TimerAction action)
        {
            TimerStartAction = action;
            return this;
        }
        public Timer OnTimerKill(TimerAction action)
        {
            TimerKillAction = action;
            return this;
        }
        public Timer OnTimerEnd(TimerAction action)
        {
            TimerEndAction = action;
            return this;
        }
        public Timer OnTimerUpdate(TimerAction action)
        {
            TimerUpdateAction = action;
            return this;
        }

        public Timer Start()
        {
            if (Disposed)
            {
                "ERROR :: Cannot start timer because it is disposed".ToLog();
                return this;
            }
            if (Started)
            {
                return this;
            }
            if (Activated)
            {
                "ERROR :: This timer is already started".ToLog();
                return this;
            }

            Started = true;
            //StartTime = default;
            Completed = false;
            Killed = false;

            CoreSystem.Instance.m_Timers.Enqueue(this);
            return this;
        }
        public Timer Kill()
        {
            Killed = true;
            return this;
        }

        public void Dispose()
        {
            Disposed = true;
        }
    }
}
