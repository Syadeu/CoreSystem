namespace Syadeu.ECS
{
    public enum PathStatus
    {
        Idle = 0,
        
        PathQueued = 1 << 0,

        PathFound = 1 << 1,
        Failed = 1 << 2,
        //Paused = 1 << 2,

        ExceedDistance = 1 << 3
    }
}