using UnityEditor;
using UnityEngine;
using umake.unity;

namespace umake{
    public static class UMake{
        [MenuItem("Tools/build/atlases")]
        public static void BuildAtlases(){
            var atlases=Pipeline_Atlas.Collect("Assets");
            Pipeline.BuildAtlas(atlases);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/build/bundles")]
        public static void BuildBundles(){
            var root=Application.dataPath;
            var bundles=Pipeline_Bundle.Collect(root);
            var output=$"./build/{EditorUserBuildSettings.activeBuildTarget}";
            Pipeline.BuildAssetBundles(output,bundles);
        }
    }
}