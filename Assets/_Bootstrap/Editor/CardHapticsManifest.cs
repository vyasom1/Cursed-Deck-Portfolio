using System.IO;
using System.Xml;
using UnityEditor.Android;
namespace CursedDeck.Bootstrap.Editor
{
    /// <summary>Add only the normal VIBRATE permission; retain Unity's generated activity configuration.</summary>
    public sealed class CardHapticsManifest:IPostGenerateGradleAndroidProject
    {
        public int callbackOrder=>0;
        public void OnPostGenerateGradleAndroidProject(string path)=>EnsurePermission(Path.Combine(path,"src/main/AndroidManifest.xml"));
        public static void EnsurePermission(string manifest)
        {
            const string ns="http://schemas.android.com/apk/res/android";
            var document=new XmlDocument();document.Load(manifest);var root=document.DocumentElement;
            foreach(XmlNode node in root.ChildNodes)if(node.Name=="uses-permission"&&node.Attributes?["name",ns]?.Value=="android.permission.VIBRATE")return;
            var permission=document.CreateElement("uses-permission");var name=document.CreateAttribute("android","name",ns);name.Value="android.permission.VIBRATE";permission.Attributes.Append(name);root.AppendChild(permission);document.Save(manifest);
        }
    }
}
