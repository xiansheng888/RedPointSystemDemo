using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace ProjectMagicEscape.Editor.RoslynGateway
{
    internal static class UnityGatewayAgent
    {
        private const string RegisterPath = "/internal/agent/register";
        private const string HeartbeatPath = "/internal/agent/heartbeat";
        private const string PullTaskPath = "/internal/agent/pull-task";
        private const string PushResultPath = "/internal/agent/push-result";

        private const double RegisterRetrySeconds = 1.0;
        private const int DefaultHttpTimeoutMs = 3000;
        private const int LongPollMaxWaitMs = 5000;

        private static readonly Queue<InternalPushResultRequest> PendingPushResults = new Queue<InternalPushResultRequest>();

        private static HttpClient _httpClient;
        private static string _gatewayBaseUrl;
        private static bool _initialized;

        private static string _sessionId;
        private static string _agentState = GatewayStateValues.Ready;
        private static string _stateDetail = "Idle";

        private static float _heartbeatIntervalSec = 1f;
        private static float _pollIntervalSec = 0.2f;

        private static double _nextRegisterTime;
        private static double _nextHeartbeatTime;
        private static double _nextPollTime;
        private static double _nextPushRetryTime;

        private static Task<InternalRegisterResponse> _registerTask;
        private static Task<InternalHeartbeatResponse> _heartbeatTask;
        private static Task<InternalPullTaskResponse> _pullTask;
        private static Task<InternalPushResultResponse> _pushTask;

        private static GatewayTaskPayload _pendingTask;
        private static bool _isExecuting;

        public static void Startup()
        {
            if (_initialized)
            {
                return;
            }

            _gatewayBaseUrl = GetGatewayBaseUrl();
            _httpClient = new HttpClient();
            _httpClient.Timeout = Timeout.InfiniteTimeSpan;

            EditorApplication.update += OnEditorUpdate;
            _initialized = true;

            SetState(GatewayStateValues.Ready, "Waiting for registration");
            _nextRegisterTime = EditorApplication.timeSinceStartup;
            Debug.Log($"[UnityGateway] Started. Gateway={_gatewayBaseUrl}");
        }

        public static void Shutdown()
        {
            if (!_initialized)
            {
                return;
            }

            EditorApplication.update -= OnEditorUpdate;

            _httpClient?.Dispose();
            _httpClient = null;
            _initialized = false;

            _sessionId = null;
            _pendingTask = null;
            PendingPushResults.Clear();
        }

        public static void OnBeforeAssemblyReload()
        {
            if (!_initialized || string.IsNullOrEmpty(_sessionId))
            {
                return;
            }

            _ = HeartbeatOnceAsync(_sessionId, GatewayStateValues.Reloading, "Assembly reload");
        }

        private static void OnEditorUpdate()
        {
            if (!_initialized)
            {
                return;
            }

            TickRegister();
            if (string.IsNullOrEmpty(_sessionId))
            {
                return;
            }

            TickHeartbeat();
            TickPushResultQueue();

            if (_isExecuting)
            {
                return;
            }

            if (_pendingTask != null)
            {
                ExecutePendingTask();
                return;
            }

            TickPullTask();
        }

        private static void TickRegister()
        {
            if (!string.IsNullOrEmpty(_sessionId))
            {
                return;
            }

            double now = EditorApplication.timeSinceStartup;
            if (_registerTask == null)
            {
                if (now < _nextRegisterTime)
                {
                    return;
                }

                _registerTask = RegisterAsync();
                return;
            }

            if (!_registerTask.IsCompleted)
            {
                return;
            }

            try
            {
                InternalRegisterResponse response = _registerTask.Result;
                if (response != null && response.accepted && !string.IsNullOrEmpty(response.sessionId))
                {
                    _sessionId = response.sessionId;
                    _heartbeatIntervalSec = Mathf.Max(0.2f, response.heartbeatIntervalSec);
                    _pollIntervalSec = Mathf.Max(0.05f, response.pollIntervalSec);

                    _nextHeartbeatTime = now;
                    _nextPollTime = now;
                    SetState(GatewayStateValues.Ready, "Registered");
                    Debug.Log($"[UnityGateway] Registered session={_sessionId}");
                }
                else
                {
                    _nextRegisterTime = now + RegisterRetrySeconds;
                    Debug.LogWarning($"[UnityGateway] Register rejected: {response?.message}");
                }
            }
            catch (Exception ex)
            {
                _nextRegisterTime = now + RegisterRetrySeconds;
                Debug.LogWarning($"[UnityGateway] Register failed: {ex.Message}");
            }
            finally
            {
                _registerTask = null;
            }
        }

        private static void TickHeartbeat()
        {
            double now = EditorApplication.timeSinceStartup;

            if (_heartbeatTask != null)
            {
                if (!_heartbeatTask.IsCompleted)
                {
                    return;
                }

                try
                {
                    InternalHeartbeatResponse response = _heartbeatTask.Result;
                    if (response == null || !response.accepted)
                    {
                        ResetSession($"Heartbeat rejected: {response?.message}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[UnityGateway] Heartbeat failed: {ex.Message}");
                }
                finally
                {
                    _heartbeatTask = null;
                }
            }

            if (_heartbeatTask == null && now >= _nextHeartbeatTime)
            {
                string state = ResolveHeartbeatState();
                string detail = ResolveHeartbeatDetail();
                _heartbeatTask = HeartbeatOnceAsync(_sessionId, state, detail);
                _nextHeartbeatTime = now + _heartbeatIntervalSec;
            }
        }

        private static void TickPullTask()
        {
            double now = EditorApplication.timeSinceStartup;

            if (_pullTask != null)
            {
                if (!_pullTask.IsCompleted)
                {
                    return;
                }

                try
                {
                    InternalPullTaskResponse response = _pullTask.Result;
                    if (response == null)
                    {
                        _nextPollTime = now + _pollIntervalSec;
                    }
                    else if (!response.accepted)
                    {
                        ResetSession($"PullTask rejected: {response.message}");
                    }
                    else if (response.hasTask && response.task != null)
                    {
                        _pendingTask = response.task;
                    }
                    else
                    {
                        _nextPollTime = now + _pollIntervalSec;
                    }
                }
                catch (Exception ex)
                {
                    _nextPollTime = now + _pollIntervalSec;
                    Debug.LogWarning($"[UnityGateway] PullTask failed: {ex.Message}");
                }
                finally
                {
                    _pullTask = null;
                }

                return;
            }

            if (now < _nextPollTime)
            {
                return;
            }

            _pullTask = PullTaskAsync(_sessionId, LongPollMaxWaitMs);
        }

        private static void ExecutePendingTask()
        {
            GatewayTaskPayload task = _pendingTask;
            _pendingTask = null;

            if (task == null)
            {
                return;
            }

            _isExecuting = true;
            SetState(GatewayStateValues.Busy, $"Executing {task.requestId}");

            GatewayDoCodeResult executionResult;
            try
            {
                executionResult = RoslynDoCodeExecutor.Execute(task.code, task.timeoutSec);
                executionResult.requestId = task.requestId;
            }
            catch (Exception ex)
            {
                executionResult = new GatewayDoCodeResult();
                executionResult.requestId = task.requestId;
                executionResult.success = false;
                executionResult.state = "RuntimeError";
                executionResult.error.type = ex.GetType().FullName;
                executionResult.error.message = ex.Message;
                executionResult.error.stackTrace = ex.ToString();
            }
            finally
            {
                _isExecuting = false;
                SetState(GatewayStateValues.Ready, "Idle");
            }

            InternalPushResultRequest request = new InternalPushResultRequest();
            request.sessionId = _sessionId;
            request.requestId = task.requestId;
            request.result = executionResult;

            PendingPushResults.Enqueue(request);
            _nextPushRetryTime = EditorApplication.timeSinceStartup;
        }

        private static void TickPushResultQueue()
        {
            double now = EditorApplication.timeSinceStartup;

            if (_pushTask != null)
            {
                if (!_pushTask.IsCompleted)
                {
                    return;
                }

                try
                {
                    InternalPushResultResponse response = _pushTask.Result;
                    if (response != null && response.accepted)
                    {
                        if (PendingPushResults.Count > 0)
                        {
                            PendingPushResults.Dequeue();
                        }
                    }
                    else
                    {
                        _nextPushRetryTime = now + 1.0;
                        if (response != null)
                        {
                            Debug.LogWarning($"[UnityGateway] PushResult rejected: {response.message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _nextPushRetryTime = now + 1.0;
                    Debug.LogWarning($"[UnityGateway] PushResult failed: {ex.Message}");
                }
                finally
                {
                    _pushTask = null;
                }

                return;
            }

            if (PendingPushResults.Count == 0 || now < _nextPushRetryTime || string.IsNullOrEmpty(_sessionId))
            {
                return;
            }

            InternalPushResultRequest request = PendingPushResults.Peek();
            request.sessionId = _sessionId;
            _pushTask = PushResultAsync(request);
        }

        private static async Task<InternalRegisterResponse> RegisterAsync()
        {
            InternalRegisterRequest request = new InternalRegisterRequest();
            return await PostJsonAsync<InternalRegisterRequest, InternalRegisterResponse>(RegisterPath, request, DefaultHttpTimeoutMs);
        }

        private static async Task<InternalHeartbeatResponse> HeartbeatOnceAsync(string sessionId, string state, string detail)
        {
            InternalHeartbeatRequest request = new InternalHeartbeatRequest();
            request.sessionId = sessionId;
            request.state = state;
            request.detail = detail;
            return await PostJsonAsync<InternalHeartbeatRequest, InternalHeartbeatResponse>(HeartbeatPath, request, DefaultHttpTimeoutMs);
        }

        private static async Task<InternalPullTaskResponse> PullTaskAsync(string sessionId, int maxWaitMs)
        {
            InternalPullTaskRequest request = new InternalPullTaskRequest();
            request.sessionId = sessionId;
            request.maxWaitMs = Mathf.Clamp(maxWaitMs, 50, 5000);
            return await PostJsonAsync<InternalPullTaskRequest, InternalPullTaskResponse>(PullTaskPath, request, maxWaitMs + DefaultHttpTimeoutMs);
        }

        private static async Task<InternalPushResultResponse> PushResultAsync(InternalPushResultRequest request)
        {
            return await PostJsonAsync<InternalPushResultRequest, InternalPushResultResponse>(PushResultPath, request, DefaultHttpTimeoutMs);
        }

        private static async Task<TResponse> PostJsonAsync<TRequest, TResponse>(string path, TRequest request, int timeoutMs)
        {
            if (_httpClient == null)
            {
                throw new InvalidOperationException("HttpClient is not initialized");
            }

            string url = _gatewayBaseUrl + path;
            string requestJson = JsonConvert.SerializeObject(request);

            using (StringContent content = new StringContent(requestJson, Encoding.UTF8, "application/json"))
            using (CancellationTokenSource cts = new CancellationTokenSource(Mathf.Max(500, timeoutMs)))
            using (HttpResponseMessage response = await _httpClient.PostAsync(url, content, cts.Token))
            {
                string responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"HTTP {(int)response.StatusCode}: {responseJson}");
                }

                TResponse parsed = JsonConvert.DeserializeObject<TResponse>(responseJson);
                if (parsed == null)
                {
                    throw new Exception($"Empty response at {path}");
                }

                return parsed;
            }
        }

        private static string ResolveHeartbeatState()
        {
            if (EditorApplication.isCompiling || _isExecuting)
            {
                return GatewayStateValues.Busy;
            }

            return _agentState;
        }

        private static string ResolveHeartbeatDetail()
        {
            if (EditorApplication.isCompiling)
            {
                return "Editor is compiling";
            }

            if (_isExecuting)
            {
                return _stateDetail;
            }

            return _stateDetail;
        }

        private static string GetGatewayBaseUrl()
        {
            string fromEnv = Environment.GetEnvironmentVariable("UNITY_ROSLYN_GATEWAY_URL");
            string url = string.IsNullOrWhiteSpace(fromEnv) ? "http://127.0.0.1:19090" : fromEnv;
            return url.TrimEnd('/');
        }

        private static void SetState(string state, string detail)
        {
            _agentState = state;
            _stateDetail = detail ?? string.Empty;
        }

        private static void ResetSession(string reason)
        {
            Debug.LogWarning($"[UnityGateway] Reset session: {reason}");
            _sessionId = null;
            _pendingTask = null;
            _pullTask = null;
            _heartbeatTask = null;
            PendingPushResults.Clear();
            SetState(GatewayStateValues.Ready, "Waiting for registration");
            _nextRegisterTime = EditorApplication.timeSinceStartup + RegisterRetrySeconds;
        }
    }
}
