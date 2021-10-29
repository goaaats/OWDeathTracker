﻿using UnityEngine;
using UnityEditor;
using System.IO;

public class AssetBundles : MonoBehaviour {

	[MenuItem("Assets/Build AssetBundles")]
    static void BuildAllAssetBundles () {
        string assetBundleDirectory = "../AssetBundles";
        if (!Directory.Exists(assetBundleDirectory)) {
            Directory.CreateDirectory(assetBundleDirectory);
        }
        BuildPipeline.BuildAssetBundles(assetBundleDirectory,
                                        BuildAssetBundleOptions.None,
                                        BuildTarget.StandaloneWindows);
    }
}
