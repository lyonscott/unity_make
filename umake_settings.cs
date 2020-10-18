using System.Collections.Generic;

namespace umake{
    public static class Settings{
        private static string[] __arr(params string[] strs){return strs;}
        
        public static string BUILD_TOKEN="bundle";
        public static string[] ASSET_TOKEN=new string[]{
            ".png",
            ".prefab",
            ".asset",
        };
        public static string[] LANG_TOKEN=new string[]{
            "zhcn",
            "en",
        };
        public static Dictionary<string,string[]> BUNDLE_TOKEN=new Dictionary<string,string[]>(){
            {"atlas",__arr(".spriteatlas")},
            {"data",ASSET_TOKEN},
        };
    }
}