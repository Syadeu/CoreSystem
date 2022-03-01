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

namespace Syadeu.Presentation.Actor
{
    [System.Flags]
    public enum ActorWeaponEquipOptions
    {
        FollowProviderSettings      =   0,

        /// <summary>
        /// 현재 들고있는 무기와 바꿉니다. 
        /// 들고있던 무기는 <see cref="ActorInventoryProvider"/> 로 들어가며, 없으면 즉시 파괴됩니다.
        /// </summary>
        SwitchWithSelected          =   0b00001,

        /// <summary>
        /// 빈 착용 공간이 있으면 그 공간에 집어넣고, 만약 없으면 <see cref="ActorInventoryProvider"/> 로 들어갑니다.
        /// <see cref="ActorInventoryProvider"/> 가 없는 경우 에러를 호출한 뒤 즉시 파괴됩니다.
        /// </summary>
        ToInventoryIfIsFull         =   0b00010,
        DestroyIfIsFull             =   0b00100,

        SelectWeapon                =   0b01000,
    }
}
