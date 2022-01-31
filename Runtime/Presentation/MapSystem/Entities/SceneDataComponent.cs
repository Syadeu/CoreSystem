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

using Syadeu.Collections;
using System;

namespace Syadeu.Presentation.Map
{
    public struct SceneDataComponent : IEntityComponent, IDisposable
    {
        internal bool m_Created;
        internal FixedInstanceList64<MapDataEntity> m_CreatedMapData;
        internal FixedInstanceList64<TerrainData> m_CreatedTerrains;

        void IDisposable.Dispose()
        {
            for (int i = 0; i < m_CreatedMapData.Length; i++)
            {
                //MapDataEntity mapData = m_CreatedMapData[i].Object;
                //mapData.DestroyChildOnDestroy = DestroyChildOnDestroy;
                m_CreatedMapData[i].GetEntity().Destroy();
            }
            for (int i = 0; i < m_CreatedTerrains.Length; i++)
            {
                m_CreatedTerrains[i].GetEntity().Destroy();
            }
        }
    }
}
