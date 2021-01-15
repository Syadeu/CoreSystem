﻿using System;

#if UNITY_JOBS && UNITY_MATH && UNITY_BURST && UNITY_COLLECTION


namespace Syadeu.ECS
{
    [Flags]
    public enum StraightPathFlags
    {
        Start = 0x01, // The vertex is the start position.
        End = 0x02, // The vertex is the end position.
        OffMeshConnection = 0x04 // The vertex is start of an off-mesh link.
    }
}

#endif