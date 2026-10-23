using NUnit.Framework;
using CursedDeck.Bootstrap.Editor;
namespace CursedDeck.ViewTests
{
    public sealed class AndroidBuildScopeTests
    {
        [Test] public void InferenceStrippingPreservesAllProjectAndUiShaderPaths()
        {
            Assert.That(StripUnusedInferenceShaders.IsUnusedInferenceAsset("Packages/com.unity.ai.inference/Runtime/Core/Resources/Sentis/PixelShaders/ConvTranspose.shader"));
            Assert.That(StripUnusedInferenceShaders.IsUnusedInferenceAsset("Assets/Shaders/HiddenSentis.shader"), Is.False);
            Assert.That(StripUnusedInferenceShaders.IsUnusedInferenceAsset("Assets/TextMesh Pro/Shaders/TMP_SDF.shader"), Is.False);
            Assert.That(StripUnusedInferenceShaders.IsUnusedInferenceAsset("Packages/com.unity.render-pipelines.universal/Shaders/2D/Sprite-Unlit-Default.shader"), Is.False);
            Assert.That(StripUnusedInferenceShaders.IsUnusedInferenceAsset(null), Is.False);
        }
    }
}
