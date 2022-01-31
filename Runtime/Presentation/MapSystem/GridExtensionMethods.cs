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

using Syadeu.Collections;

namespace Syadeu.Presentation.Map
{
    [System.Obsolete("Use WorldGridSystem Instead", true)]
    public static class GridExtensionMethods
    {
        public static GridLayerChain Combine(this in GridLayer x, in GridLayer y)
        {
            return PresentationSystem<DefaultPresentationGroup, GridSystem>.System.Combine(in x, in y);
        }
        public static GridLayerChain Combine(this in GridLayer x, params GridLayer[] others)
        {
            return PresentationSystem<DefaultPresentationGroup, GridSystem>.System.Combine(in x, others);
        }
        public static GridLayerChain Combine(this in GridLayerChain x, in GridLayer y)
        {
            return PresentationSystem<DefaultPresentationGroup, GridSystem>.System.Combine(in x, in y);
        }
        public static GridLayerChain Combine(this in GridLayerChain x, in GridLayerChain y)
        {
            return PresentationSystem<DefaultPresentationGroup, GridSystem>.System.Combine(in x, in y);
        }

        public static Direction GetDirectionOf(this in GridTile x, in GridTile y)
        {
            GridSystem gridSystem = PresentationSystem<DefaultPresentationGroup, GridSystem>.System;
            for (int i = 0; i < 4; i++)
            {
                GridPosition temp = gridSystem.GetDirection(x.index, (Direction)(1 << i));
                if (temp.index.Equals(y.index))
                {
                    return (Direction)(1 << i);
                }
            }

            return Direction.NONE;
        }
    }
}
