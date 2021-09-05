using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Events
{
    public sealed class OnGridDetectEntityEvent : SynchronizedEvent<OnGridDetectEntityEvent>
    {
        public Entity<IEntity> Detector { get; private set; }
        public Entity<IEntity> Target { get; private set; }

        public bool Detected { get; private set; }

        public static OnGridDetectEntityEvent GetEvent(Entity<IEntity> detector, Entity<IEntity> target, bool isDetect)
        {
            var temp = Dequeue();
            temp.Detector = detector;
            temp.Target = target;
            temp.Detected = isDetect;
            return temp;
        }
        public override bool IsValid() => Detector.IsValid() && Target.IsValid();
        protected override void OnTerminate()
        {
            Detector = Entity<IEntity>.Empty;
            Target = Entity<IEntity>.Empty;
        }
    }
}
