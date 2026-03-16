using System;
using System.Collections.Generic;

namespace ProjectMagicEscape.Editor.RoslynGateway
{
    internal static class GatewayStateValues
    {
        public const string Ready = "Ready";
        public const string Busy = "Busy";
        public const string Reloading = "Reloading";
    }

    [Serializable]
    internal sealed class InternalRegisterRequest
    {
        public string agentName = "unity-editor";
    }

    [Serializable]
    internal sealed class InternalRegisterResponse
    {
        public bool accepted;
        public string sessionId;
        public float heartbeatIntervalSec = 1f;
        public float pollIntervalSec = 0.2f;
        public string message;
    }

    [Serializable]
    internal sealed class InternalHeartbeatRequest
    {
        public string sessionId;
        public string state;
        public string detail;
    }

    [Serializable]
    internal sealed class InternalHeartbeatResponse
    {
        public bool accepted;
        public string message;
    }

    [Serializable]
    internal sealed class GatewayTaskPayload
    {
        public string requestId;
        public string code;
        public int timeoutSec;
        public string createdAtUtc;
    }

    [Serializable]
    internal sealed class InternalPullTaskRequest
    {
        public string sessionId;
        public int maxWaitMs;
    }

    [Serializable]
    internal sealed class InternalPullTaskResponse
    {
        public bool accepted;
        public bool hasTask;
        public GatewayTaskPayload task;
        public string message;
    }

    [Serializable]
    internal sealed class InternalPushResultRequest
    {
        public string sessionId;
        public string requestId;
        public GatewayDoCodeResult result;
    }

    [Serializable]
    internal sealed class InternalPushResultResponse
    {
        public bool accepted;
        public string message;
    }

    [Serializable]
    internal sealed class GatewayDoCodeResult
    {
        public string requestId;
        public bool success;
        public string state;
        public GatewayResultPayload result = new GatewayResultPayload();
        public GatewayCompilePayload compile = new GatewayCompilePayload();
        public GatewayErrorPayload error = new GatewayErrorPayload();
        public GatewayTimingPayload timingMs = new GatewayTimingPayload();
    }

    [Serializable]
    internal sealed class GatewayResultPayload
    {
        public string resultJson;
        public string resultText;
        public string resultType;
    }

    [Serializable]
    internal sealed class GatewayCompilePayload
    {
        public string usedMode = "none";
        public List<GatewayDiagnosticEntry> diagnostics = new List<GatewayDiagnosticEntry>();
    }

    [Serializable]
    internal sealed class GatewayDiagnosticEntry
    {
        public string severity;
        public string code;
        public string message;
        public int line;
        public int column;
    }

    [Serializable]
    internal sealed class GatewayErrorPayload
    {
        public string type;
        public string message;
        public string stackTrace;
    }

    [Serializable]
    internal sealed class GatewayTimingPayload
    {
        public int queue;
        public int compile;
        public int execute;
        public int total;
    }
}
