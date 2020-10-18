#define COMMENT_IGNORE_ON
using System;
using System.IO;
using System.Collections.Generic;

namespace umake{
    /*  
        - build_token 主包
        - bundle_token 类型包
        - lang_toekn 语言包
        root/
            [main]@[BUILD_TOKEN]/
                [sub]@[BUILD_TOKEN]/
                    [name]@[BUNDLE_TOKEN]/
                        [name]@[LANG_TOKEN].[ext]
        build => [main]_[sub]_[BUNDLE_TOKEN]@[LANG_TOKEN].[BUILD_TOKEN]
    */
    public static class Pipeline_Bundle{
        private static Func<string,bool> __filt(params string[] strs){
            return (str)=>{return new List<string>(strs).Contains(str);};
        }
        private static (string,string) __split(string str){
            var arr=str.Split('@');
            return (arr[0],arr.Length>1? arr[1].ToLower():"");
        }

        public static string BUILD_TOKEN=Settings.BUILD_TOKEN;
        public static string[] ASSET_TOKEN=Settings.ASSET_TOKEN;
        public static string[] LANG_TOKEN=Settings.LANG_TOKEN;
        public static Dictionary<string,string[]> BUNDLE_TOKEN=Settings.BUNDLE_TOKEN;

        private static Func<string,bool> FILTER(string name){
            return BUNDLE_TOKEN.ContainsKey(name)? __filt(BUNDLE_TOKEN[name]):__filt(ASSET_TOKEN);
        }
        public static (string,string[])[] Collect(string root){
            var bundles=new Dictionary<string,List<string>>();
            var que_bundle=new Queue<string>();
            var current_bundle="";
            var filt=__filt(ASSET_TOKEN);
            
            Action<string> start_mark=(string url)=>{
                var fname=Path.GetFileNameWithoutExtension(url);
                #if COMMENT_INGORE_ON
                if(fname.StartsWith("#"))return;
                #endif
                var (prefix,suffix)=__split(fname);
                filt=FILTER(suffix);
                if(suffix==BUILD_TOKEN){
                    var name=prefix;
                    if(que_bundle.Count>0){
                        var parent=que_bundle.Peek();
                        name=$"{parent}_{name}";
                    }
                    que_bundle.Enqueue(name);
                    current_bundle=name;
                    if(!bundles.ContainsKey(name))bundles.Add(name,new List<string>());
                    Console.WriteLine($"# bundle: {name}");
                }
                {//sub bundle
                    if(que_bundle.Count<=0)return;
                    if(!BUNDLE_TOKEN.ContainsKey(suffix))return;
                    var name=que_bundle.Peek();
                    current_bundle=$"{name}_{suffix}";
                    if(!bundles.ContainsKey(current_bundle))
                        bundles.Add(current_bundle,new List<string>());
                }
            };
            Action<string> end_mark=(string url)=>{
                var fname=Path.GetFileNameWithoutExtension(url);
                var (prefix,suffix)=__split(fname);
                if(suffix==BUILD_TOKEN)
                    que_bundle.Dequeue();
            };
            Action<string> add_asset=(string url)=>{
                if(que_bundle.Count<=0)return;

                var name=Path.GetFileNameWithoutExtension(url);
                var exten=Path.GetExtension(url);
                #if COMMENT_IGNORE_ON
                if(name.StartsWith("#"))return;
                #endif 
                if(!filt(exten))return;

                var (prefix,suffix)=__split(name);
                var bundle_name=current_bundle;
                if(__filt(LANG_TOKEN)(suffix)){
                    bundle_name=$"{current_bundle}@{suffix}";
                    if(!bundles.ContainsKey(bundle_name))
                        bundles.Add(bundle_name,new List<string>());
                }
                Console.WriteLine($"- {bundle_name} [{name}] {url}");
                bundles[bundle_name].Add(url);
            };
            Action<string> collect=null;
            collect=(string url)=>{
                if(!Directory.Exists(url))return;
                start_mark(url);
                var dinfo=new DirectoryInfo(url);
                var (dirs,files)=(dinfo.GetDirectories(),dinfo.GetFiles());
                foreach(var info in dirs)collect(info.FullName);
                foreach(var info in files)add_asset(info.FullName);
                end_mark(url);
            };
            Func<(string,string[])[]> output=()=>{
                var all=new List<(string,string[])>();
                foreach(var pair in bundles){
                    all.Add((pair.Key,pair.Value.ToArray()));
                }
                return all.ToArray();
            };

            collect(root);
            return output();
        }
    }
}

namespace umake.unity{
    using UnityEngine;
    using UnityEditor;
    using UnityEditor.Build.Content;
    using UnityEditor.Build.Pipeline;
    using UnityEditor.Build.Pipeline.Interfaces;

    public static partial class Pipeline{
        public static void BuildAssetBundles(string output,params (string,string[])[] bundles){
            Func<string,string[],AssetBundleBuild> pack=(string name,string[] urls)=>{
                var build=new AssetBundleBuild();{
                    build.assetBundleName=name;
                    var addrs=new List<string>();
                    var assets=new List<string>();
                    foreach(var url in urls){
                        var asset_name=Path.GetFileName(url);
                        addrs.Add(asset_name);
                        assets.Add($"Assets/{url.Remove(0,Application.dataPath.Length)}");
                    }
                    build.addressableNames=addrs.ToArray();
                    build.assetNames=assets.ToArray();
                }
                return build;
            };
            Action<AssetBundleBuild[]> exe=(AssetBundleBuild[] builds)=>{
                var options=BuildAssetBundleOptions.ChunkBasedCompression;
                var target=EditorUserBuildSettings.activeBuildTarget;
                var group=BuildPipeline.GetBuildTargetGroup(target);
                var args=new BundleBuildParameters(target,group,output);
                var content=new BundleBuildContent(builds);
                var code=ContentPipeline.BuildAssetBundles(args,content,out IBundleBuildResults results);
            };
            var all=new AssetBundleBuild[bundles.Length];
            for(int i=0;i<all.Length;++i){
                var (name,urls)=bundles[i];
                all[i]=pack(name,urls);
            }
            exe(all);
        }
    }
}