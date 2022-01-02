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

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Syadeu.Presentation.Map
{
    internal sealed class ObstacleLayerModule : PresentationSystemModule<GridSystem>
    {
        private NativeHashMap<GridLayer, UnsafeHashSet<int>> m_Layers;
        private NativeMultiHashMap<GridLayerChain, GridLayer> m_ParsedLayers;
        private GridMapAttribute m_Grid;

        #region Presentation Methods

        protected override void OnInitialize()
        {
            m_ParsedLayers = new NativeMultiHashMap<GridLayerChain, GridLayer>(512, AllocatorManager.Persistent);
        }
        public void Initialize(GridMapAttribute grid)
        {
            Clear();

            m_Grid = grid;
            m_Layers = new NativeHashMap<GridLayer, UnsafeHashSet<int>>(grid.m_Layers.Length, AllocatorManager.Persistent);

            for (int i = 0; i < grid.m_Layers.Length; i++)
            {
                GridLayer layer = GetLayer(i);
                UnsafeHashSet<int> set = new UnsafeHashSet<int>(grid.m_Layers[i].m_Indices.Length, Allocator.Persistent);
                for (int j = 0; j < grid.m_Layers[i].m_Indices.Length; j++)
                {
                    set.Add(grid.m_Layers[i].m_Indices[j]);
                }

                m_Layers.Add(layer, set);
            }
        }
        protected override void OnDispose()
        {
            Clear();

            m_ParsedLayers.Dispose();
        }

        #endregion

        #region Get

        public GridLayer GetLayer(in int index)
        {
            if (m_Grid == null || m_Grid.m_Layers.Length <= index)
            {
                CoreSystem.Logger.LogError(Channel.Data,
                    $"Grid layer({index}) not found.");
                return GridLayer.Empty;
            }

            int hash = unchecked(index * 397 ^ m_Grid.GetHashCode());
            bool inverse = m_Grid.m_Layers[index].m_Inverse;

            return new GridLayer(hash, inverse);
        }
        public GridLayerChain GetLayerChain(params int[] indices)
        {
            if (m_Grid == null || m_Grid.m_Layers.Length == 0)
            {
                CoreSystem.Logger.LogError(Channel.Data,
                    $"Grid layer not found.");
                return GridLayerChain.Empty;
            }

            if (indices.Length == 0) return GridLayerChain.Empty;
            GridLayerChain chain;

            unsafe
            {
                GridLayer* layers = stackalloc GridLayer[indices.Length];
                layers[0] = GetLayer(indices[0]);
                chain = new GridLayerChain(layers[0]);

                for (int i = 1; i < indices.Length; i++)
                {
                    layers[i] = GetLayer(indices[i]);

                    chain = new GridLayerChain(chain, layers[i]);
                }

                if (m_ParsedLayers.ContainsKey(chain))
                {
                    return chain;
                }

                for (int i = 0; i < indices.Length; i++)
                {
                    m_ParsedLayers.Add(chain, layers[i]);
                }
            }

            return chain;
        }

        public bool TryGetChainEnumerator(in GridLayerChain chain, out NativeMultiHashMap<GridLayerChain, GridLayer>.Enumerator iter)
        {
            if (m_ParsedLayers.ContainsKey(chain))
            {
                iter = m_ParsedLayers.GetValuesForKey(chain);
                return true;
            }

            iter = default(NativeMultiHashMap<GridLayerChain, GridLayer>.Enumerator);
            return false;
        }

        public GridLayerChain Combine(in GridLayer x, in GridLayer y)
        {
            GridLayerChain chain = new GridLayerChain(x, y);
            if (m_ParsedLayers.ContainsKey(chain)) return chain;

            m_ParsedLayers.Add(chain, x);
            m_ParsedLayers.Add(chain, y);

            return chain;
        }
        public GridLayerChain Combine(in GridLayer x, params GridLayer[] others)
        {
            GridLayerChain chain = new GridLayerChain(x, others);
            if (m_ParsedLayers.ContainsKey(chain)) return chain;

            m_ParsedLayers.Add(chain, x);
            for (int i = 0; i < others.Length; i++)
            {
                m_ParsedLayers.Add(chain, others[i]);
            }

            return chain;
        }
        public GridLayerChain Combine(in GridLayerChain x, in GridLayer y)
        {
            GridLayerChain chain = new GridLayerChain(x, y);
            if (m_ParsedLayers.ContainsKey(chain)) return chain;

            if (!m_ParsedLayers.TryGetFirstValue(x, out var layer, out var iter))
            {
                throw new Exception();
            }

            do
            {
                m_ParsedLayers.Add(chain, layer);
            } while (m_ParsedLayers.TryGetNextValue(out layer, ref iter));

            m_ParsedLayers.Add(chain, y);

            return chain;
        }
        public GridLayerChain Combine(in GridLayerChain x, in GridLayerChain y)
        {
            GridLayerChain chain = new GridLayerChain(x, y);
            if (m_ParsedLayers.ContainsKey(chain)) return chain;

            if (!m_ParsedLayers.ContainsKey(x) || !m_ParsedLayers.ContainsKey(y))
            {
                throw new Exception();
            }

            GridLayer layer;
            if (m_ParsedLayers.TryGetFirstValue(x, out layer, out var iter))
            {
                do
                {
                    m_ParsedLayers.Add(chain, layer);
                } while (m_ParsedLayers.TryGetNextValue(out layer, ref iter));
            }
            if (m_ParsedLayers.TryGetFirstValue(y, out layer, out iter))
            {
                do
                {
                    m_ParsedLayers.Add(chain, layer);
                } while (m_ParsedLayers.TryGetNextValue(out layer, ref iter));
            }

            return chain;
        }

        #endregion

        public bool Has(in GridLayer layer, in int index)
        {
            if (!m_Layers.TryGetValue(layer, out var set)) return false;

            return set.Contains(index);
        }
        public bool Has(in GridLayerChain chain, in int index)
        {
            if (!m_ParsedLayers.TryGetFirstValue(chain, out var layer, out var iter)) return false;

            do
            {
                if (Has(in layer, in index)) return true;
            } while (m_ParsedLayers.TryGetNextValue(out layer, ref iter));

            return false;
        }
        public bool Has(in GridLayerChain chain, in int index, out GridLayer targetLayer)
        {
            if (!m_ParsedLayers.TryGetFirstValue(chain, out var layer, out var iter))
            {
                targetLayer = GridLayer.Empty;
                return false;
            }

            do
            {
                if (Has(in layer, in index))
                {
                    targetLayer = layer;
                    return true;
                }
            } while (m_ParsedLayers.TryGetNextValue(out layer, ref iter));

            targetLayer = GridLayer.Empty;
            return false;
        }

        public void Clear()
        {
            if (m_Layers.IsCreated)
            {
                foreach (var item in m_Layers)
                {
                    item.Value.Dispose();
                }

                m_Layers.Dispose();
            }

            m_ParsedLayers.Clear();

            m_Grid = null;
        }

        #region Filter

        public void FilterByLayer1024(in GridLayerChain chain, ref FixedList4096Bytes<int> indices)
        {
            for (int i = 0; i < indices.Length; i++)
            {
                if (!Has(in chain, indices[i], out GridLayer targetLayer)) continue;

                if (targetLayer.Inverse)
                {
                    indices.RemoveAt(i);
                    continue;
                }
                else
                {
                    indices.RemoveAt(i);
                    continue;
                }
            }
        }
        public void FilterByLayer1024(in GridLayer layer, ref FixedList4096Bytes<int> indices)
        {
            for (int i = 0; i < indices.Length; i++)
            {
                if (layer.Inverse)
                {
                    if (!m_Layers[layer].Contains(indices[i]))
                    {
                        //filtered.Add(indices[i]);
                        indices.RemoveAt(i);
                        continue;
                    }
                }
                else
                {
                    if (m_Layers[layer].Contains(indices[i]))
                    {
                        //filtered.Add(indices[i]);
                        indices.RemoveAt(i);
                        continue;
                    }
                }
            }
        }

        public void FilterByLayer(in GridLayerChain chain, ref NativeList<int> indices)
        {
            for (int i = indices.Length - 1; i >= 0; i--)
            {
                if (!Has(in chain, indices[i], out GridLayer targetLayer)) continue;

                if (targetLayer.Inverse)
                {
                    indices.RemoveAt(i);
                    continue;
                }
                else
                {
                    indices.RemoveAt(i);
                    continue;
                }
            }
        }
        public void FilterByLayer(in GridLayer layer, ref NativeList<int> indices)
        {
            for (int i = indices.Length - 1; i >= 0; i--)
            {
                if (layer.Inverse)
                {
                    if (!m_Layers[layer].Contains(indices[i]))
                    {
                        //filtered.Add(indices[i]);
                        indices.RemoveAt(i);
                        continue;
                    }
                }
                else
                {
                    if (m_Layers[layer].Contains(indices[i]))
                    {
                        //filtered.Add(indices[i]);
                        indices.RemoveAt(i);
                        continue;
                    }
                }
            }
        }

        [Obsolete]
        public int[] FilterByLayer(GridLayer layer, int[] indices, out int[] filteredIndices)
        {
            List<int> temp = new List<int>();
            List<int> filtered = new List<int>();
            for (int i = 0; i < indices.Length; i++)
            {
                if (layer.Inverse)
                {
                    if (!m_Layers[layer].Contains(indices[i]))
                    {
                        filtered.Add(indices[i]);
                        continue;
                    }
                }
                else
                {
                    if (m_Layers[layer].Contains(indices[i]))
                    {
                        filtered.Add(indices[i]);
                        continue;
                    }
                }

                temp.Add(indices[i]);
            }
            filteredIndices = filtered.Count == 0 ? Array.Empty<int>() : filtered.ToArray();
            return temp.ToArray();
        }

        #endregion
    }
}
