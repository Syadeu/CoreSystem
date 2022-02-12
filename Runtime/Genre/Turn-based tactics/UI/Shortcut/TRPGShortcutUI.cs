using Syadeu.Collections;
using System.Collections;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

using EventSystem = Syadeu.Presentation.Events.EventSystem;

namespace Syadeu.Presentation.TurnTable.UI
{
    //[RequireComponent(typeof(Button))]
    [AddComponentMenu("")]
    public sealed class TRPGShortcutUI : PresentationBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        //[SerializeField] private ShortcutType m_ShortcutType;
        private FixedReference<TRPGShortcutData> m_Data;

        [SerializeField] private TextMeshProUGUI m_ShortcutIndexText;

        //private bool m_IsHide = false;
        //private bool m_Enabled = true;

        private Button m_Button;
        private TRPGCanvasUISystem m_CanvasUISystem;
        private Events.EventSystem m_EventSystem;
        private int m_Index;

        //public ShortcutType ShortcutType => m_ShortcutType;
        //public bool Hide 
        //{ 
        //    get => m_IsHide;
        //    set
        //    {
        //        m_IsHide = value;
        //        gameObject.SetActive(!m_IsHide);
        //    }
        //}
        //public bool Enable
        //{
        //    get => m_Enabled;
        //    set
        //    {
        //        m_Enabled = value;
        //        m_Button.interactable = value;
        //        gameObject.SetActive(value);
        //    }
        //}
        public int Index => m_Index + 1;
        public FixedReference<TRPGShortcutData> Data => m_Data;

        private void Awake()
        {
            RequestSystem<DefaultPresentationGroup, EventSystem>(Bind);
            RequestSystem<TRPGIngameSystemGroup, TRPGCanvasUISystem>(Bind);
        }
        private void Bind(EventSystem other)
        {
            m_EventSystem = other;
        }
        private void Bind(TRPGCanvasUISystem other)
        {
            m_CanvasUISystem = other;
        }
        public void Initialize(TRPGShortcutData data)
        {
            m_Data = data.AsOriginal();

            GetOrAddComponent<CanvasRenderer>();
            RectTransform tr = (RectTransform)transform;
            tr.sizeDelta = data.m_Generals.m_SizeDelta;
            
            GameObject backgroundObj = new GameObject("Background");
            Image backgroundImg;
            {
                backgroundObj.AddComponent<CanvasRenderer>();
                backgroundObj.transform.SetParent(tr);

                backgroundImg = backgroundObj.AddComponent<Image>();
                data.m_Generals.m_BackgroundImage.LoadAssetAsync(t => backgroundImg.sprite = t);
                backgroundImg.color = data.m_Generals.m_BackgroundColor;

                RectTransform backgroundTr = (RectTransform)backgroundObj.transform;
                SetupAnchors(backgroundTr);
                backgroundTr.anchoredPosition = Vector2.zero;
                backgroundTr.sizeDelta = data.m_Generals.m_SizeDelta;
            }

            GameObject imageObj = new GameObject("Image");
            {
                imageObj.AddComponent<CanvasRenderer>();
                imageObj.transform.SetParent(tr);

                var imageImg = imageObj.AddComponent<Image>();
                //texImg.sprite = data.m_Generals.m_Image.LoadAsset();
                data.m_Generals.m_Image.LoadAssetAsync(t => imageImg.sprite = t);
                imageImg.color = data.m_Generals.m_ImageColor;

                RectTransform imageTr = (RectTransform)imageObj.transform;
                SetupAnchors(imageTr);

                float half = data.m_Generals.m_TextureOffset * .5f;

                imageTr.anchoredPosition = new Vector2(0, -half);
                imageTr.sizeDelta = data.m_Generals.m_SizeDelta - (float2)data.m_Generals.m_TextureOffset;
            }

            m_Button = GetOrAddComponent<Button>();
            {
                m_Button.onClick.AddListener(Click);

                m_Button.targetGraphic = backgroundImg;
            }

            Transform parent = transform.parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                if (parent.GetChild(i).Equals(transform))
                {
                    m_Index = i;
                    break;
                }
            }

            //m_ShortcutIndexText.text = $"{m_Index + 1}";
        }
        private static void SetupAnchors(RectTransform tr)
        {
            tr.anchorMin = new Vector2(.5f, 1);
            tr.anchorMax = new Vector2(.5f, 1);
            tr.pivot = new Vector2(.5f, 1);
        }
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        //private IEnumerator Start()
        //{
            

        //    yield return new WaitUntil(() => m_CanvasUISystem != null);

        //    //m_CanvasUISystem.AuthoringShortcut(this, m_ShortcutType);
        //}
        private void OnDestroy()
        {
            //m_CanvasUISystem.RemoveShortcut(this, m_ShortcutType);

            m_CanvasUISystem = null;
            m_EventSystem = null;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            //"pointer down".ToLog();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            //"pointer in".ToLog();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            //"pointer exit".ToLog();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            //"pointer up".ToLog();
        }
        private void Click()
        {
            m_EventSystem.PostEvent(TRPGShortcutUIPressedEvent.GetEvent(this, m_Data));
        }

        internal void OnKeyboardPressed(InputAction.CallbackContext obj)
        {
            Click();
        }
    }
}