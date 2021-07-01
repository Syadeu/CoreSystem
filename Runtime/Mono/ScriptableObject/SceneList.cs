#if UNITY_EDITOR
#endif

using Syadeu.Database;
using System.Collections.Generic;

namespace Syadeu.Mono
{
    public sealed class SceneList : StaticSettingEntity<SceneList>
    {
        public SceneReference CustomLoadingScene;
        public SceneReference StartScene;

        public List<SceneReference> Scenes = new List<SceneReference>();
    }
}
 