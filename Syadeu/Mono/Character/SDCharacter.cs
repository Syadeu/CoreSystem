using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace Syadeu.Mono
{
    public class SDCharacter : DataBehaviour
    {
        [SerializeField] private int m_CharacterType;

        public bool Initialized { get; private set; } = false;
        public SDType Type { get; private set; }

        protected override void Awake()
        {
            base.Awake();

            Type = SDTypeSetting.Instance.Types[m_CharacterType];

            if (Type.m_ComponentFlags.HasFlag(SDComponentFlag.BasicHealth))
            {
                AddDataComponent<SDBasicHealth>().Initialize(Type);
            }

            Initialized = true;
        }
    }
}
