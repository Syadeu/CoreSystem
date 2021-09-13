using Syadeu.Database;
using System;

namespace Syadeu.Presentation
{
    public interface IInstance : IValidation, IEquatable<IInstance>
    {
        Hash Idx { get; }
        ObjectBase Object { get; }
    }
}
