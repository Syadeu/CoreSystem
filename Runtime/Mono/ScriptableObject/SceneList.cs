#if UNITY_EDITOR
#endif

using Syadeu.Database;
using Syadeu.Presentation;
using System.Collections.Generic;
using UnityEngine;

namespace Syadeu.Mono
{
    public sealed class SceneList : StaticSettingEntity<SceneList>
    {
        public SceneReference CustomLoadingScene;

        [Space]
        public SceneReference MasterScene;
        public SceneReference StartScene;

        public List<SceneReference> Scenes = new List<SceneReference>();

        public SceneReference GetScene(string path)
        {
            if (CustomLoadingScene != null && CustomLoadingScene.ScenePath.Equals(path)) return CustomLoadingScene;
            if (MasterScene != null && MasterScene.ScenePath.Equals(path)) return MasterScene;
            if (StartScene != null && StartScene.ScenePath.Equals(path)) return StartScene;

            for (int i = 0; i < Scenes.Count; i++)
            {
                if (Scenes[i].ScenePath.Equals(path)) return Scenes[i];
            }
            return null;
        }
    }
}
 