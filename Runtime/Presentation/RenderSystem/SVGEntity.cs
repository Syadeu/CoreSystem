// Copyright 2021 Seung Ha Kim
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Newtonsoft.Json;
using Syadeu.Presentation.Data;
using System;
using System.ComponentModel;
using System.IO;
using Unity.VectorGraphics;

namespace Syadeu.Presentation.Render
{
    [DisplayName("Data: SVG Data")]
    public sealed class SVGEntity : DataObjectBase
    {
        [JsonProperty(Order = 0, PropertyName = "RawData")]
        public byte[] m_RawData = Array.Empty<byte>();

        [JsonProperty(Order = 1, PropertyName = "DPI")]
        public float m_DPI = 100;
        [JsonProperty(Order = 2, PropertyName = "PixelPerUnit")]
        public float m_PixelPerUnit = 100;
        [JsonProperty(Order = 3, PropertyName = "WindowWidth")]
        public int m_WindowWidth = 0;
        [JsonProperty(Order = 4, PropertyName = "WindowHeight")]
        public int m_WindowHeight = 0;
        [JsonProperty(Order = 5, PropertyName = "ClipViewport")]
        public bool m_ClipViewport = false;

        [JsonProperty(Order = 100, PropertyName = "GradientResolution")]
        public int m_GradientResolution = 64;

    }
    internal sealed class SVGEntityProcessor : EntityProcessor<SVGEntity>
    {
        private VectorGraphicsModule m_GraphicsModule;

        protected override void OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, RenderSystem>(Bind);
        }
        protected override void OnDispose()
        {
            m_GraphicsModule = null;
        }
        private void Bind(RenderSystem other)
        {
            m_GraphicsModule = other.GetModule<VectorGraphicsModule>();
        }

        protected override void OnCreated(SVGEntity obj)
        {
            SVGParser.SceneInfo svg;
            using (var str = new MemoryStream(obj.m_RawData))
            using (var rdr = new StreamReader(str))
            {
                svg = SVGParser.ImportSVG(rdr,
                    dpi: obj.m_DPI,
                    pixelsPerUnit: obj.m_PixelPerUnit,
                    windowWidth: obj.m_WindowWidth,
                    windowHeight: obj.m_WindowHeight,
                    clipViewport: obj.m_ClipViewport
                    );
            }
        }
    }
}
