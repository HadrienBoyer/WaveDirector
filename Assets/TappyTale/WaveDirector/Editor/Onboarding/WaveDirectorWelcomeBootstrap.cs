#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace TappyTale.WaveDirector.Editor
{
    [InitializeOnLoad]
    internal static class WaveDirectorWelcomeBootstrap
    {
        private const string Version = "1.1.0";
        private const string PrefKey = "TT_WaveDirector_WelcomeShown_";

        static WaveDirectorWelcomeBootstrap()
        {
            if (Application.isBatchMode) return;
            EditorApplication.delayCall += TryShow;
        }

        private static void TryShow()
        {
            const string SessionKey = "TT_WaveDirector_WelcomeShownThisSession";
            if (SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, true);

            if (EditorPrefs.GetBool(PrefKey + Version, false)) return;

            WaveDirectorWelcomeWindow.ShowWindow(focus: false);
        }
    }
}
#endif
