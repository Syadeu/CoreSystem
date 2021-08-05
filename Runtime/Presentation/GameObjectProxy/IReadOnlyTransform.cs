using Syadeu.ThreadSafe;
using System;
using Unity.Mathematics;

namespace Syadeu.Presentation
{
    [Obsolete("", true)]
    public interface IReadOnlyTransform
    {
        Vector3 position { get; }
        //Vector3 localPosition { get; }

        Vector3 eulerAngles { get; }
        //Vector3 localEulerAngles { get; }
        quaternion rotation { get; }
        //quaternion localRotation { get; }

        Vector3 right { get; }
        Vector3 up { get; }
        Vector3 forward { get; }

        //Vector3 lossyScale { get; }
        Vector3 localScale { get; }
    }
}
