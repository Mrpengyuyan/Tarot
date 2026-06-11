using NUnit.Framework;
using TarotUnity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    public sealed class Phase10ReleaseUxTests
    {
        [Test]
        public void ReleasePackageTextExplainsExecutableConfigAndFallback()
        {
            var packageTextType = FindRuntimeType("TarotUnity.UI.ReleasePackageText");
            Assert.That(packageTextType, Is.Not.Null);

            var readme = InvokeStaticString(packageTextType, "BuildReadme");

            Assert.That(readme, Does.Contain("TarotUnity.exe"));
            Assert.That(readme, Does.Contain("tarot_desktop_config.json"));
            Assert.That(readme, Does.Contain("http://localhost:8000/api/v1"));
            Assert.That(readme, Does.Contain("本地模式"));
            Assert.That(readme, Does.Contain("后端"));

            var configExample = InvokeStaticString(packageTextType, "BuildConfigExample");
            Assert.That(configExample, Does.Contain("\"backendBaseUrl\""));
            Assert.That(configExample, Does.Contain("\"requestTimeoutSeconds\""));
        }

        [Test]
        public void ReleaseStatusCopyIsPlayerReadable()
        {
            var copyType = FindRuntimeType("TarotUnity.UI.ReleaseUxCopy");
            Assert.That(copyType, Is.Not.Null);

            Assert.That(GetStaticStringProperty(copyType, "LocalModeReady"), Does.Contain("本地模式"));
            Assert.That(InvokeStaticString(copyType, "BackendFallback", "timeout"), Does.Contain("后端暂时不可用"));
            Assert.That(InvokeStaticString(copyType, "BackendFallback", "timeout"), Does.Contain("本地模式"));
            Assert.That(InvokeStaticString(copyType, "BackendOnlyFailure", "401"), Does.Contain("tarot_desktop_config.json"));
            Assert.That(InvokeStaticString(copyType, "BackendOnlyFailure", "401"), Does.Contain("后端连接失败"));
        }

        [Test]
        public void ReadingRoomHasReleaseStatusTextWiredToController()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/ReadingRoom.unity");

            var status = GameObject.Find("Phase10_ReleaseStatusText");
            Assert.That(status, Is.Not.Null);

            var text = status.GetComponent<Text>();
            Assert.That(text, Is.Not.Null);
            Assert.That(text.text, Does.Contain("本地模式"));

            var controller = Object.FindFirstObjectByType<ReadingRoomController>();
            Assert.That(controller, Is.Not.Null);

            var serializedController = new SerializedObject(controller);
            var property = serializedController.FindProperty("releaseStatusText");
            Assert.That(property, Is.Not.Null);
            Assert.That(property.objectReferenceValue, Is.SameAs(text));
        }

        [Test]
        public void Phase10PackageBuilderTypeExists()
        {
            var builderType = System.Type.GetType("TarotUnity.Editor.Phase10ReleasePackageBuilder, Assembly-CSharp-Editor");
            Assert.That(builderType, Is.Not.Null);

            var windowsFolder = builderType.GetField("WindowsReleaseFolder")?.GetValue(null) as string;
            Assert.That(windowsFolder, Is.EqualTo("Builds/Desktop/Release/TarotUnity-Windows-x64"));
        }

        private static System.Type FindRuntimeType(string fullName)
        {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static string GetStaticStringProperty(System.Type type, string propertyName)
        {
            var property = type.GetProperty(propertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            Assert.That(property, Is.Not.Null);
            return property.GetValue(null) as string;
        }

        private static string InvokeStaticString(System.Type type, string methodName, params object[] args)
        {
            var method = type.GetMethod(methodName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            Assert.That(method, Is.Not.Null);
            return method.Invoke(null, args) as string;
        }
    }
}
