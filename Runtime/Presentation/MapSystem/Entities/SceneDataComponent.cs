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
                m_CreatedMapData[i].Destroy();
            }
            for (int i = 0; i < m_CreatedTerrains.Length; i++)
            {
                m_CreatedTerrains[i].Destroy();
            }
        }
    }
}
