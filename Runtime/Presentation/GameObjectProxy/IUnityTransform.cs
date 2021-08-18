using Syadeu.Presentation.Entities;
using System;
using UnityEngine;

namespace Syadeu.Presentation
{
    public interface IUnityTransform : ITransform, IDisposable
    {
#pragma warning disable IDE1006 // Naming Styles
        ConvertedEntity entity { get; }
        Transform provider { get; }
#pragma warning restore IDE1006 // Naming Styles
    }
}
