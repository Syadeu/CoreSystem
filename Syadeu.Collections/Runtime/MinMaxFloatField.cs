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

using System;
using UnityEngine;

namespace Syadeu.Collections
{
    [Serializable]
    public class MinMaxFloatField
    {
        [SerializeField]
        private float m_Min, m_Max;

        public float Min { get => m_Min; set { m_Min = value; } }
        public float Max { get => m_Max; set { m_Max = value; } }

        public MinMaxFloatField(float init)
        {
            m_Min = init;
            m_Max = init;
        }
        public MinMaxFloatField(float min, float max)
        {
            m_Min = min;
            m_Max = max;
        }

        public float GetValue()
        {
            return UnityEngine.Random.Range(m_Min, m_Max);
        }
    }
}
