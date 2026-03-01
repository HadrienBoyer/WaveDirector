// File: Assets/TappyTale/WaveDirector/Editor/Onboarding/WaveDirectorWelcomeBootstrap.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace TappyTale.WaveDirector.Editor
{
    /// <summary>
    /// Shows the Wave Director welcome window once per installed version (unless the user disables it).
    /// </summary>
    [InitializeOnLoad]
    internal static class WaveDirectorWelcomeBootstrap
    {
        private const string Version = "1.0.2";
        private const string PrefKey = "TT_WaveDirector_WelcomeShown_";

        static WaveDirectorWelcomeBootstrap()
        {
            // Avoid popping UI in batch mode (CI/build server).
            if (Application.isBatchMode) return;

            // Delay so Unity has time to finish compiling and importing.
            EditorApplication.delayCall += TryShow;
        }

        private static void TryShow()
        {
            // SessionState prevents repeated pop-ups during the same editor session.
            const string SessionKey = "TT_WaveDirector_WelcomeShownThisSession";
            if (SessionState.GetBool(SessionKey, false)) return;

            SessionState.SetBool(SessionKey, true);

            if (EditorPrefs.GetBool(PrefKey + Version, false)) return;

            WaveDirectorWelcomeWindow.ShowWindow(focus: false);
        }
    }
}
#endif
