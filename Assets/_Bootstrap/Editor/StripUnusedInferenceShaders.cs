using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace CursedDeck.Bootstrap.Editor
{
    /// <summary>The installed Editor AI tools bring inference Resources into player builds.
    /// Cursed Deck has no inference runtime dependency; keep its variants out of Android.</summary>
    public sealed class StripUnusedInferenceShaders : IPreprocessShaders, IPreprocessComputeShaders
    {
        public int callbackOrder => int.MinValue;
        public static bool IsUnusedInferenceAsset(string path) =>
            path != null && path.StartsWith("Packages/com.unity.ai.inference/", StringComparison.Ordinal);

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> variants)
        {
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android &&
                IsUnusedInferenceAsset(AssetDatabase.GetAssetPath(shader))) variants.Clear();
        }

        public void OnProcessComputeShader(ComputeShader shader, string kernelName, IList<ShaderCompilerData> variants)
        {
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android &&
                IsUnusedInferenceAsset(AssetDatabase.GetAssetPath(shader))) variants.Clear();
        }
    }
}
