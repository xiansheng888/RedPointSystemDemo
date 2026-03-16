using UnityEditor;

namespace ProjectMagicEscape.Editor.RoslynGateway
{
    [InitializeOnLoad]
    internal static class UnityGatewayBootstrap
    {
        static UnityGatewayBootstrap()
        {
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            EditorApplication.quitting += OnEditorQuitting;
        }

        private static void OnBeforeAssemblyReload()
        {
            UnityGatewayAgent.OnBeforeAssemblyReload();
        }

        private static void OnEditorQuitting()
        {
            UnityGatewayAgent.Shutdown();
        }
    }
}
