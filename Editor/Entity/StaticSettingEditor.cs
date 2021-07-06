using System.IO;

using Syadeu.Entities;

using UnityEngine;
using UnityEditor;

namespace SyadeuEditor
{
    [InitializeOnLoad]
    public abstract class StaticSettingEditor<T> : SettingEntity, IStaticSetting where T : ScriptableObject
    {
        private static T s_Instance;
        private static bool s_IsEnforceOrder;
        public static T Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    //if (!IsMainthread())
                    //{
                    //    StaticManagerEntity.AwaitForNotNull(ref s_Instance, ref s_IsEnforceOrder, EnforceOrder);
                    //    return s_Instance;
                    //}

                    if (!Directory.Exists("Assets/Resources/Syadeu/Editor"))
                    {
                        Directory.CreateDirectory("Assets/Resources/Syadeu/Editor");
                    }

                    s_Instance = Resources.Load<T>("Syadeu/Editor" + typeof(T).Name);
                    if (s_Instance == null)
                    {
                        //$"LOG :: Creating new static setting<{typeof(T).Name}> asset".ToLog();
                        s_Instance = CreateInstance<T>();
                        s_Instance.name = $"Syadeu {typeof(T).Name} Setting Asset";

                        if (!Directory.Exists("Assets/Resources/Syadeu/Editor"))
                        {
                            AssetDatabase.CreateFolder("Assets/Resources/Syadeu", "Editor");
                        }
                        AssetDatabase.CreateAsset(s_Instance, "Assets/Resources/Syadeu/Editor/" + typeof(T).Name + ".asset");
                    }
                }

                (s_Instance as IStaticSetting).OnInitialize();

                return s_Instance;
            }
        }

        public bool Initialized { get; private set; }
        public virtual void OnInitialize()
        {
            Initialized = true;
        }
        public virtual void Initialize() { }

        private static void EnforceOrder()
        {
            (Instance as IStaticSetting).Initialize();
        }
    }

    [System.Serializable]
    public sealed class Tooltip
    {
        static GUIStyle TitleStyle { get { if (_titleStyle == null) InitStyles(); return _titleStyle; } }
        //static GUIStyle ShortcutStyle { get { if (_shortcutStyle == null) InitStyles(); return _shortcutStyle; } }
        static GUIStyle _titleStyle = null;
        static GUIStyle _shortcutStyle = null;

        const float k_MinWidth = 128;
        const float k_MaxWidth = 330;
        const float k_MinHeight = 0;

        static void InitStyles()
        {
            _titleStyle = new GUIStyle();
            _titleStyle.margin = new RectOffset(4, 4, 4, 4);
            _titleStyle.padding = new RectOffset(4, 4, 4, 4);
            _titleStyle.fontSize = 14;
            _titleStyle.fontStyle = FontStyle.Bold;
            _titleStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            _titleStyle.richText = true;

            _shortcutStyle = new GUIStyle(_titleStyle);
            _shortcutStyle.fontSize = 14;
            _shortcutStyle.fontStyle = FontStyle.Normal;
            _shortcutStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(.5f, .5f, .5f, 1f) : new Color(.3f, .3f, .3f, 1f);

            EditorStyles.wordWrappedLabel.richText = true;
        }

        //static readonly Color separatorColor = new Color(.65f, .65f, .65f, .5f);

        /// <summary>
        /// The title to show in the tooltip window.
        /// </summary>
        /// <value>
        /// The header text for this tooltip.
        /// </value>
        public string title { get; set; }

        /// <summary>
        /// A brief summary of what this menu action does.
        /// </summary>
        /// <value>
        /// The body of the summary text.
        /// </value>
        public string summary { get; set; }

        ///// <summary>
        ///// The shortcut assigned to this menu item.
        ///// </summary>
        ///// <value>
        ///// A text representation of the optional shortcut.
        ///// </value>
        //public string shortcut { get; set; }

        internal static Tooltip TempContent = new Tooltip("", "");

        ///// <summary>
        ///// Create a new tooltip.
        ///// </summary>
        ///// <param name="title">The header text for this tooltip.</param>
        ///// <param name="summary">The body of the tooltip text. This should be kept brief.</param>
        ///// <param name="shortcut">A set of keys to be displayed as the shortcut for this action.</param>
        //public Tooltip(string title, string summary/*, params char[] shortcut*/) : this(title, summary/*, ""*/)
        //{
        //    if (shortcut != null && shortcut.Length > 0)
        //    {
        //        this.shortcut = string.Empty;

        //        for (int i = 0; i < shortcut.Length - 1; i++)
        //        {
        //            if (!EditorUtility.IsUnix())
        //                this.shortcut += InternalUtility.ControlKeyString(shortcut[i]) + " + ";
        //            else
        //                this.shortcut += shortcut[i] + " + ";
        //        }

        //        if (!EditorUtility.IsUnix())
        //            this.shortcut += InternalUtility.ControlKeyString(shortcut[shortcut.Length - 1]);
        //        else
        //            this.shortcut += shortcut[shortcut.Length - 1];
        //    }
        //}

        /// <summary>
        /// Create a new tooltip.
        /// </summary>
        /// <param name="title">The header text for this tooltip.</param>
        /// <param name="summary">The body of the tooltip text. This should be kept brief.</param>
        /// <param name="shortcut">A set of keys to be displayed as the shortcut for this action.</param>
        public Tooltip(string title, string summary/*, string shortcut = ""*/)
        {
            this.title = title;
            this.summary = summary;
            //this.shortcut = shortcut;
        }

        /// <summary>
        /// Get the size required in GUI space to render this tooltip.
        /// </summary>
        /// <returns></returns>
        internal Vector2 CalcSize()
        {
            const float pad = 8;
            Vector2 total = new Vector2(k_MinWidth, k_MinHeight);

            bool hastitle = !string.IsNullOrEmpty(title);
            bool hasSummary = !string.IsNullOrEmpty(summary);
            //bool hasShortcut = !string.IsNullOrEmpty(shortcut);

            if (hastitle)
            {
                Vector2 ns = TitleStyle.CalcSize(EditorUtils.TempContent(title));

                //if (hasShortcut)
                //{
                //    ns.x += EditorStyles.boldLabel.CalcSize(EditorUtils.TempContent(shortcut)).x + pad;
                //}

                total.x += Mathf.Max(ns.x, 256);
                total.y += ns.y;
            }

            if (hasSummary)
            {
                if (!hastitle)
                {
                    Vector2 sumSize = EditorStyles.wordWrappedLabel.CalcSize(EditorUtils.TempContent(summary));
                    total.x = Mathf.Min(sumSize.x, k_MaxWidth);
                }

                float summaryHeight = EditorStyles.wordWrappedLabel.CalcHeight(EditorUtils.TempContent(summary), total.x);
                total.y += summaryHeight;
            }

            if (hastitle && hasSummary)
                total.y += 16;

            total.x += pad;
            total.y += pad;

            return total;
        }

        internal void Draw()
        {
            if (!string.IsNullOrEmpty(title))
            {
                //if (!string.IsNullOrEmpty(shortcut))
                //{
                //    GUILayout.BeginHorizontal();
                //    GUILayout.Label(title, TitleStyle);
                //    GUILayout.FlexibleSpace();
                //    GUILayout.Label(shortcut, ShortcutStyle);
                //    GUILayout.EndHorizontal();
                //}
                //else
                {
                    GUILayout.Label(title, TitleStyle);
                }

                //UI.EditorGUIUtility.DrawSeparator(1, separatorColor);
                EditorUtils.SectorLine();
                GUILayout.Space(2);
            }

            if (!string.IsNullOrEmpty(summary))
            {
                GUILayout.Label(summary, EditorStyles.wordWrappedLabel);
            }
        }
    }
}
