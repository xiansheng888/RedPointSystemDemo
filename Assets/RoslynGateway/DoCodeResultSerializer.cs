using Newtonsoft.Json;

namespace ProjectMagicEscape.Editor.RoslynGateway
{
    internal static class DoCodeResultSerializer
    {
        public static GatewayResultPayload Serialize(object value)
        {
            GatewayResultPayload payload = new GatewayResultPayload();

            if (value == null)
            {
                payload.resultType = "null";
                payload.resultJson = "null";
                payload.resultText = string.Empty;
                return payload;
            }

            payload.resultType = value.GetType().FullName;

            try
            {
                payload.resultJson = JsonConvert.SerializeObject(value);
            }
            catch
            {
                payload.resultJson = null;
            }

            try
            {
                payload.resultText = value.ToString();
            }
            catch
            {
                payload.resultText = "<ToString failed>";
            }

            return payload;
        }
    }
}
