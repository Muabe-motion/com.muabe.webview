using UnityEditor;

[InitializeOnLoad]
public class WebViewDefines
{
    static WebViewDefines()
    {
        var target = EditorUserBuildSettings.selectedBuildTargetGroup;
        if (target == BuildTargetGroup.Android)
        {
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);

            if (!defines.Contains("UNITYWEBVIEW_ANDROID_USES_CLEARTEXT_TRAFFIC"))
            {
                defines += ";UNITYWEBVIEW_ANDROID_USES_CLEARTEXT_TRAFFIC";
                PlayerSettings.SetScriptingDefineSymbolsForGroup(target, defines);
                UnityEngine.Debug.Log("Added UNITYWEBVIEW_ANDROID_USES_CLEARTEXT_TRAFFIC define");
            }
        }
    }
}