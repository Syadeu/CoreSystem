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

using Unity.Collections;

namespace Syadeu.Presentation.Map
{
    public struct GridPath16
   {
        public static GridPath16 Create()
        {
            return new GridPath16()
            {
                m_Paths = new FixedList32Bytes<GridTile>()
            };
        }

        private FixedList32Bytes<GridTile> m_Paths;

        public FixedList32Bytes<GridTile> Paths => m_Paths;
        public int Length => m_Paths.Length;
        public GridTile this[int index]
        {
            get => m_Paths[index];
            set => m_Paths[index] = value;
        }

        public GridPath16(in FixedList32Bytes<GridTile> paths)
        {
            m_Paths = paths;
        }

        public void Clear() => m_Paths.Clear();
        public void Add(in GridTile pathTile) => m_Paths.Add(in pathTile);
        public void RemoveAt(in int index) => m_Paths.RemoveAt(index);
    }
    public struct GridPath64
    {
        public static GridPath64 Create()
        {
            return new GridPath64()
            {
                m_Paths = new FixedList512Bytes<GridTile>()
            };
        }

        private FixedList512Bytes<GridTile> m_Paths;

        public FixedList512Bytes<GridTile> Paths => m_Paths;
        public int Length => m_Paths.Length;
        public GridTile this[int index]
        {
            get => m_Paths[index];
            set => m_Paths[index] = value;
        }

        public GridPath64(in FixedList512Bytes<GridTile> paths)
        {
            m_Paths = paths;
        }

        public void Clear() => m_Paths.Clear();
        public void Add(in GridTile pathTile) => m_Paths.Add(in pathTile);
        public void RemoveAt(in int index) => m_Paths.RemoveAt(index);
    }
}
