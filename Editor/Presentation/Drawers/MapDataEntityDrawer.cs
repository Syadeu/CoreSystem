using DG.DemiEditor;
using Syadeu;
using Syadeu.Collections;
using Syadeu.Presentation;
using Syadeu.Presentation.Map;
using Syadeu.Presentation.Render;
using SyadeuEditor.Utilities;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.VectorGraphics;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    public sealed class MapDataEntityDrawer : EntityDrawer
    {
        private bool m_OpenInvalidList = false;
        private readonly List<int> m_InvalidIndices = new List<int>();
        private readonly List<bool> m_OpenInvalidIndex = new List<bool>();

        public override string Name
        {
            get
            {
                if (m_InvalidIndices.Count > 0)
                {
                    return base.Name + " [Contains Invalid Objects]";
                }
                return base.Name;
            }
        }
        MapDataEntityBase Entity => (MapDataEntityBase)Target;
        ArrayDrawer ArrayDrawer;

        public MapDataEntityDrawer(ObjectBase objectBase) : base(objectBase)
        {
            FindInvalidObjectIndices();

            //ArrayDrawer = (ArrayDrawer)Drawers.Where((other) => other.Name.Equals("Objects")).First();
            ArrayDrawer = GetDrawer<ArrayDrawer>("Objects");
        }

        private static bool IsInvalidObject(MapDataEntityBase.Object obj)
        {
            if (!obj.m_Object.IsValid() || obj.m_Object.IsEmpty())
            {
                return true;
            }
            return false;
        }
        private void FindInvalidObjectIndices()
        {
            m_InvalidIndices.Clear();
            m_OpenInvalidIndex.Clear();

            for (int i = 0; i < Entity.m_Objects.Length; i++)
            {
                MapDataEntityBase.Object obj = Entity.m_Objects[i];
                if (IsInvalidObject(obj))
                {
                    obj.m_Object = Reference<Syadeu.Presentation.Entities.EntityBase>.Empty;
                    GUI.changed = true;

                    m_InvalidIndices.Add(i);
                    m_OpenInvalidIndex.Add(false);
                }
            }

            m_OpenInvalidList = m_InvalidIndices.Count > 0;
        }
        private void DrawInvalids()
        {
            using (new EditorUtilities.BoxBlock(ColorPalettes.TriadicColor.Three))
            {
                m_OpenInvalidList = EditorUtilities.Foldout(m_OpenInvalidList, "Invalid Objects Founded", 13);
                if (GUILayout.Button("Remove All"))
                {
                    List<MapDataEntityBase.Object> temp = Entity.m_Objects.ToList();
                    temp.RemoveAll(IsInvalidObject);
                    Entity.m_Objects = temp.ToArray();

                    List<ObjectDrawerBase> removeList = new List<ObjectDrawerBase>();
                    for (int i = 0; i < m_InvalidIndices.Count; i++)
                    {
                        removeList.Add(ArrayDrawer[m_InvalidIndices[i]]);
                    }
                    for (int i = 0; i < removeList.Count; i++)
                    {
                        ArrayDrawer.Remove(removeList[i]);
                    }

                    m_InvalidIndices.Clear();
                    m_OpenInvalidIndex.Clear();
                    GUI.changed = true;
                }

                if (!m_OpenInvalidList) return;

                EditorGUILayout.HelpBox(
                    $"Number({m_InvalidIndices.Count}) of invalid objects in this data were found.\n" +
                    $"Fix it manually."
                    , MessageType.Error);

                EditorGUI.indentLevel++;
                for (int i = 0; i < m_InvalidIndices.Count; i++)
                {
                    int idx = m_InvalidIndices[i];

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        m_OpenInvalidIndex[i]
                        = EditorUtilities.Foldout(m_OpenInvalidIndex[i], $"Element At {idx}");
                        if (GUILayout.Button("Remove", GUILayout.Width(100)))
                        {
                            var temp = Entity.m_Objects.ToList();
                            temp.RemoveAt(idx);
                            Entity.m_Objects = temp.ToArray();

                            ArrayDrawer.RemoveAt(idx);

                            m_InvalidIndices.RemoveAt(i);
                            m_OpenInvalidIndex.RemoveAt(i);
                            i--;

                            GUI.changed = true;
                            continue;
                        }
                    }

                    if (m_OpenInvalidIndex[i])
                    {
                        EditorGUI.indentLevel++;
                        using (var change = new EditorGUI.ChangeCheckScope())
                        {
                            ArrayDrawer[idx].OnGUI();

                            if (change.changed)
                            {
                                if (!IsInvalidObject(Entity.m_Objects[idx]))
                                {
                                    m_InvalidIndices.RemoveAt(i);
                                    m_OpenInvalidIndex.RemoveAt(i);
                                    i--;
                                }
                            }
                        }
                        
                        EditorGUI.indentLevel--;
                    }

                    if (i + 1 < m_InvalidIndices.Count) EditorUtilities.Line();
                }
                EditorGUI.indentLevel--;
            }
        }
        protected override void DrawGUI()
        {
            DrawHeader();
            EditorUtilities.Line();

            if (m_InvalidIndices.Count > 0)
            {
                DrawInvalids();
                EditorUtilities.Line();
            }

            for (int i = 0; i < Drawers.Length; i++)
            {
                if (!IsDrawable(Drawers[i]))
                {
                    continue;
                }

                DrawField(Drawers[i]);
            }
        }
    }

    public sealed class RawSVGEntityDrawer : ObjectBaseDrawer<RawSVGEntity>
    {
        //VectorUtils.TextureAtlas m_Atlas;
        Sprite m_Atlas;
        Material m_Material;

        public RawSVGEntityDrawer(ObjectBase objectBase) : base(objectBase)
        {
            m_Material = new Material(Shader.Find("Unlit/Vector"));
            RawSVGEntity obj = (RawSVGEntity)objectBase;

            if (!string.IsNullOrEmpty(obj.m_RawData))
            {
                m_Atlas = GenerateAtlas(obj);
            }
        }
        private static Sprite GenerateAtlas(RawSVGEntity obj)
        {
            SVGParser.SceneInfo svg;
            //using (var str = new MemoryStream(obj.m_RawData, false))
            using (var rdr = new StringReader(obj.m_RawData))
            {
                svg = SVGParser.ImportSVG(rdr,
                    dpi: obj.m_DPI,
                    pixelsPerUnit: obj.m_PixelPerUnit,
                    windowWidth: obj.m_WindowWidth,
                    windowHeight: obj.m_WindowHeight,
                    clipViewport: obj.m_ClipViewport
                    );
            }
            //svg.
            var geo = VectorUtils.TessellateScene(svg.Scene, new VectorUtils.TessellationOptions()
            {
                StepDistance = 10,
                MaxCordDeviation = .5f,
                MaxTanAngleDeviation = .1f,
                SamplingStepSize = 100
            });
            //$"{geo.Count}".ToLog();
            Sprite sprite = VectorUtils.BuildSprite(geo, 128, VectorUtils.Alignment.Center, Vector2.zero, 512);
            //var atlas = VectorUtils.GenerateAtlas(geo, 128);
            //VectorUtils.FillUVs(geo, atlas);

            return sprite;
        }

        protected override void DrawGUI()
        {
            DrawHeader();
            EditorUtilities.Line();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Import SVG"))
                {
                    string path = EditorUtility.OpenFilePanel("Select SVG", Application.dataPath, "svg");
                    if (!path.IsNullOrEmpty())
                    {
                        RawSVGEntity obj = (RawSVGEntity)m_TargetObject;
                        obj.m_RawData = File.ReadAllText(path);

                        m_Atlas = GenerateAtlas(obj);
                    }
                }
                if (GUILayout.Button("Generate Atlas"))
                {
                    RawSVGEntity obj = (RawSVGEntity)m_TargetObject;
                    m_Atlas = GenerateAtlas(obj);
                }
            }
            
            if (m_Atlas != null)
            {
                EditorUtilities.Line();
                //Rect last = GUILayoutUtility.GetLastRect();
                //EditorGUI.DrawPreviewTexture(
                //    GUILayoutUtility.GetRect(last.width, 200),
                //    m_Atlas.Texture,
                //    GridExtensions.DefaultMaterial
                //    );
                GUILayout.Box(new GUIContent(m_Atlas.texture));
                EditorGUILayout.LabelField("draw");
                EditorUtilities.Line();
                //EditorUtilities.ObjectPreview(null, m_Atlas.Texture);
            }
            else EditorGUILayout.LabelField("not draw");

            for (int i = 0; i < Drawers.Length; i++)
            {
                DrawField(Drawers[i]);
            }
            //base.DrawGUI();
        }
    }
}
