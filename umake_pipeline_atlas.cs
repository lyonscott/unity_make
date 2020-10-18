#define COMMENT_IGNORE_ON
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEditor;
using UnityEditor.U2D;

namespace umake{
    public static class Pipeline_Atlas{
        private static (string,string) __split(string str){
            var arr=str.Split('@');
            return (arr[0],arr.Length>1? arr[1].ToLower():"");
        }
        private static Func<string,bool> __filt(params string[] strs){
            return (str)=>{return new List<string>(strs).Contains(str);};
        }
        private static string[] __find(string path,string filter){
            var list=new List<string>();
            var guids=AssetDatabase.FindAssets(filter,new string[]{path});
            for(int i=0;i<guids.Length;++i){
                var url=AssetDatabase.GUIDToAssetPath(guids[i]);
                var name=Path.GetFileNameWithoutExtension(url);
                #if COMMENT_IGNORE_ON
                if(name.StartsWith("#"))continue;
                #endif
                list.Add(url);
            }
            return list.ToArray();
        }
        public static string[] LANG_TOKEN=Settings.LANG_TOKEN;

        public static (string,string,string[])[] Collect(string root){
            List<(string,string,string[])> all=new List<(string,string,string[])>();
            Dictionary<string,List<string>> atlases=null;
            Dictionary<string,string> outputs=null;
            string current_atlas="";

            Action<string> start_mark=(string url)=>{
                atlases=new Dictionary<string, List<string>>();
                outputs=new Dictionary<string, string>();
                var fname=Path.GetFileNameWithoutExtension(url);
                var (prefix,suffix)=__split(fname);
                current_atlas=prefix;
                if(!atlases.ContainsKey(prefix)){
                    Console.WriteLine($"# atlas: {prefix}");
                    atlases.Add(prefix,new List<string>());
                    outputs.Add(prefix,url);
                }
            };
            Action end_mark=()=>{
                foreach(var pair in atlases){
                    var name=pair.Key;
                    var output=outputs[name];
                    var urls=pair.Value.ToArray();
                    all.Add((name,output,urls));
                }
            };
            Action<string> add_asset=(string url)=>{
                var fname=Path.GetFileNameWithoutExtension(url);
                var (prefix,suffix)=__split(fname);
                var name=current_atlas;
                if(__filt(LANG_TOKEN)(suffix))
                    name=$"{name}@{suffix}";
                if(!atlases.ContainsKey(name)){
                    Console.WriteLine($"# atlas: {name}");
                    atlases.Add(name,new List<string>());
                    outputs.Add(name,outputs[current_atlas]);
                }
                atlases[name].Add(url);
            };
            Action<string> collect=(string url)=>{
                var folders=__find(url,"@atlas t:Folder");
                foreach(var folder in folders){
                    start_mark(folder);
                    var sprites=__find(folder,"t:Sprite");
                    foreach(var path in sprites)add_asset(path);
                    end_mark();
                }
            };
            collect(root);
            return all.ToArray();
        }
    }
}

namespace umake.unity{
    public static partial class Pipeline{
        public static void BuildAtlas((string name,string output,string[] urls)[] atlases){
            Func<string[],Sprite[]> load=(urls)=>{
                var list=new List<Sprite>();
                foreach(var url in urls){
                    var obj=AssetDatabase.LoadAssetAtPath<Sprite>(url);
                    list.Add(obj);
                }
                return list.ToArray();
            };
            Action<string,string,string[]> exe=(string name,string output,string[] urls)=>{
                var asset=new SpriteAtlas();
                var objs=load(urls);
                asset.Add(objs);
                Directory.CreateDirectory(output);
                AssetDatabase.CreateAsset(asset,$"{output}/{name}.spriteatlas");
            };
            foreach(var atlas in atlases){
                exe(atlas.name,atlas.output,atlas.urls);
            }
        }
    }
}