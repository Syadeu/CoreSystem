﻿using Syadeu.Mono;
using UnityEngine;
using System;
#if UNITY_EDITOR
#endif

namespace Syadeu.Database.Lua
{
    internal sealed class LuaUtils
    {
        public static float DeltaTime => Time.deltaTime;

        public static void Log(string txt) => ConsoleWindow.Log(txt);

        public static void AddConsoleCommand(Action<string> cmd, string[] args)
        {
            ConsoleWindow.CreateCommand(cmd, args);
        }

        public static double[] GetPosition()
        {
            Transform cam = Camera.main.transform;
            Ray ray = new Ray(cam.position, cam.forward);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                return LuaVectorUtils.FromVector(hit.point);
            }
            return LuaVectorUtils.FromVector(Vector3.zero);
        }
        public static (int, int) PositionToGridCell(double[] position)
        {
            Vector3 pos = LuaVectorUtils.ToVector(position);
            if (!GridManager.HasGrid(pos))
            {
                ConsoleWindow.Log($"Grid not found at: {pos}");
                return (-1, -1);
            }

            ref GridManager.Grid grid = ref GridManager.GetGrid(pos);
            if (!grid.HasCell(pos))
            {
                ConsoleWindow.Log($"GridCell not found at: {pos}");
                return (-1, -1);
            }

            ref GridManager.GridCell cell = ref grid.GetCell(pos);
            return (grid.Idx, cell.Idx);
        }
    }
}