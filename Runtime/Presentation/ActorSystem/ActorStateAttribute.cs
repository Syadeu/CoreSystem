using Newtonsoft.Json;

namespace Syadeu.Presentation.Actor
{
    public sealed class ActorStateAttribute : ActorAttributeBase
    {
        public enum State
        {
            Idle    =   0,

            Alert   =   0b0001,
            Chasing =   0b0010,
            Battle  =   0b0100,

            Dead    =   0b1000
        }

        [JsonIgnore] private State m_State = State.Idle;
    }
}
