using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace TarotUnity.Editor
{
    public static class TarotUnityTestRunner
    {
        private const string EditModeResultPath = "Temp/TarotUnityEditModeTestResults.txt";
        private const string PlayModeResultPath = "Temp/TarotUnityPlayModeTestResults.txt";

        [MenuItem("Tools/Tarot Unity/Run EditMode Tests")]
        public static void RunEditModeTests()
        {
            RunTests(TestMode.EditMode, EditModeResultPath);
        }

        [MenuItem("Tools/Tarot Unity/Run PlayMode Tests")]
        public static void RunPlayModeTests()
        {
            RunTests(TestMode.PlayMode, PlayModeResultPath);
        }

        private static void RunTests(TestMode testMode, string resultPath)
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new ResultWriter(resultPath));
            api.Execute(new ExecutionSettings(new Filter
            {
                testMode = testMode,
            }));
        }

        private sealed class ResultWriter : ICallbacks
        {
            private readonly string path;
            private readonly StringBuilder details = new();

            public ResultWriter(string path)
            {
                this.path = path;
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
                details.AppendLine($"RunStarted: {testsToRun.Name}");
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                details.AppendLine($"RunFinished: {result.ResultState}");
                details.AppendLine($"Passed: {result.PassCount}");
                details.AppendLine($"Failed: {result.FailCount}");
                details.AppendLine($"Skipped: {result.SkipCount}");

                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "Temp");
                File.WriteAllText(path, details.ToString());
            }

            public void TestStarted(ITestAdaptor test)
            {
                details.AppendLine($"TestStarted: {test.FullName}");
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                details.AppendLine($"TestFinished: {result.FullName} => {result.ResultState}");
                if (!string.IsNullOrWhiteSpace(result.Message))
                {
                    details.AppendLine(result.Message);
                }

                if (!string.IsNullOrWhiteSpace(result.StackTrace))
                {
                    details.AppendLine(result.StackTrace);
                }
            }
        }
    }
}
