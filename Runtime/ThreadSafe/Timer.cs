using Syadeu.Extensions.Logs;
using System;

namespace Syadeu
{
    //public delegate void TimerAction();
    public delegate bool TimerCondition();
    public sealed class Timer : IDisposable
    {
        internal static int InstanceCount { get; private set; } = 0;

        public TimeSpan StartTime { get; set; }
        internal long TargetedTime { get; set; }
        internal TimeSpan TargetTime { get; set; }

        internal Action TimerStartAction { get; set; }
        internal Action TimerStartBackgroundAction { get; set; }
        internal Action TimerKillAction { get; set; }
        internal Action TimerKillBackgroundAction { get; set; }
        internal Action TimerEndAction { get; set; }
        internal Action TimerEndBackgroundAction { get; set; }
        internal Action TimerUpdateAction { get; set; }

        internal bool Disposed { get; set; } = false;

        internal bool Activated { get; set; } = false;
        internal bool Completed { get; set; } = false;
        internal bool Killed { get; set; } = false;

        internal bool Started { get; set; } = false;

        internal string CalledFrom { get; set; }

        public Timer()
        {
            InstanceCount += 1;
        }

        public bool IsTimerActive() => Activated || Started;
        public bool IsTimerComplete() => Completed;

        public Timer SetTargetTime(float second)
        {
            //TargetTime = second;
            TargetedTime = (long)(second * 10000000);
            return this;
        }
        public Timer OnTimerStart(Action action)
        {
            TimerStartAction = action;
            return this;
        }
        public Timer OnTimerStartBackground(Action action)
        {
            TimerStartBackgroundAction = action;
            return this;
        }
        public Timer OnTimerKill(Action action)
        {
            TimerKillAction = action;
            return this;
        }
        public Timer OnTimerKillBackground(Action action)
        {
            TimerKillBackgroundAction = action;
            return this;
        }
        public Timer OnTimerEnd(Action action)
        {
            TimerEndAction = action;
            return this;
        }
        public Timer OnTimerEndBackground(Action action)
        {
            TimerEndBackgroundAction = action;
            return this;
        }
        public Timer OnTimerUpdate(Action action)
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

            CalledFrom = Environment.StackTrace;
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
            InstanceCount -= 1;
            Disposed = true;
        }
    }
}
