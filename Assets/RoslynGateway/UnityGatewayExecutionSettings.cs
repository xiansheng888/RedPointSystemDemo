using UnityEditor;

namespace ProjectMagicEscape.Editor.RoslynGateway
{
    internal static class UnityGatewayExecutionSettings
    {
        private const string TrustedModePrefKey = "ProjectMagicEscape.UnityRoslynGateway.TrustedModeEnabled";

        public static bool TrustedModeEnabled
        {
            get => EditorPrefs.GetBool(TrustedModePrefKey, false);
            set => EditorPrefs.SetBool(TrustedModePrefKey, value);
        }

        public static string SecurityModeLabel
        {
            get
            {
                return TrustedModeEnabled
                    ? "Trusted (EnsureLoad)"
                    : "Restricted (UseSettings)";
            }
        }
    }
}
