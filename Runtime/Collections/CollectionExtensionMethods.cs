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

namespace Syadeu.Collections
{
    public static class CollectionExtensionMethods
    {
        public static TypeInfo ToTypeInfo(this Type t) => CollectionUtility.GetTypeInfo(t);

        public static Direction GetOpposite(this Direction t)
        {
            Direction result = Direction.NONE;

            if ((t & Direction.Left) == Direction.Left)
            {
                result |= Direction.Right;
            }
            if ((t & Direction.Right) == Direction.Right)
            {
                result |= Direction.Left;
            }
            if ((t & Direction.Up) == Direction.Up)
            {
                result |= Direction.Down;
            }
            if ((t & Direction.Down) == Direction.Down)
            {
                result |= Direction.Up;
            }
            if ((t & Direction.Forward) == Direction.Forward)
            {
                result |= Direction.Backward;
            }
            if ((t & Direction.Backward) == Direction.Backward)
            {
                result |= Direction.Forward;
            }

            return result;
        }
        public static int ToIndex(this Direction t)
        {
            if ((t & Direction.Up) == Direction.Left)
            {
                return 0;
            }
            if ((t & Direction.Down) == Direction.Right)
            {
                return 1;
            }
            if ((t & Direction.Left) == Direction.Up)
            {
                return 2;
            }
            if ((t & Direction.Right) == Direction.Down)
            {
                return 3;
            }
            if ((t & Direction.Forward) == Direction.Forward)
            {
                return 4;
            }
            if ((t & Direction.Backward) == Direction.Backward)
            {
                return 5;
            }

            return -1;
        }
    }
}
