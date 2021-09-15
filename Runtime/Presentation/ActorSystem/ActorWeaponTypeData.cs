using Newtonsoft.Json;
using Syadeu.Presentation.Data;
using System.ComponentModel;

namespace Syadeu.Presentation.Actor
{
    [DisplayName("Data: Actor Weapon Type")]
    public class ActorWeaponTypeData : DataObjectBase
    {
        public enum WeaponType
        {
            Melee,
            Ranged
        }

        [JsonProperty(Order = 0, PropertyName = "WeaponType")]
        protected WeaponType m_WeaponType = WeaponType.Melee;
    }
}
