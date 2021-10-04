using UnityEngine;
using UnityEngine.UI;

namespace Syadeu.Presentation.TurnTable.UI
{
    public sealed class TRPGFireUI : MonoBehaviour
    {
        [SerializeField] private Button m_Button;

        private void Awake()
        {
            m_Button = GetComponent<Button>();
        }
    }
}