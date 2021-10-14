using Syadeu.Collections;
using System;
using Unity.Mathematics;

namespace Syadeu.Presentation.Map
{
    public interface IGridPosition : IValidation, IEquatable<IGridPosition>
    {
        int Length { get; }

        int2 this[int i] { get; }
    }
}
