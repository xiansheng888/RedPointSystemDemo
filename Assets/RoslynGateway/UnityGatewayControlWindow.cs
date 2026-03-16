using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace ProjectMagicEscape.Editor.RoslynGateway
{
    internal sealed class UnityGatewayControlWindow : EditorWindow
    {
        private const double StatusRefreshIntervalSec = 1.5;

        private GatewayRuntimeStatus _runtimeStatus;
        private Task<GatewayRuntimeStatus> _statusTask;
        private Task<GatewayProcessOperationResult> _operationTask;

        private string _pythonExecutable;
        private double _nextRefreshTime;
        private string _operationMessage = string.Empty;

        [MenuItem("Tools/Unity Roslyn Gateway/Control Window")]
        public static void OpenWindow()
        {
            UnityGatewayControlWindow window = GetWindow<UnityGatewayControlWindow>("Unity Roslyn Gateway");
            window.minSize = new Vector2(560f, 360f);
            window.Show();
        }

        private void OnEnable()
        {
            UnityGatewayAgent.Startup();
            _pythonExecutable = UnityGatewayProcessController.PythonExecutable;
            _nextRefreshTime = 0;
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            UnityGatewayAgent.Shutdown();
        }

        private void OnEditorUpdate()
        {
            UnityGatewayProcessController.UpdateProcessState();

            if (_statusTask != null && _statusTask.IsCompleted)
            {
                if (_statusTask.Status == TaskStatus.RanToCompletion)
                {
                    _runtimeStatus = _statusTask.Result;
                }
                else
                {
                    _runtimeStatus = new GatewayRuntimeStatus
                    {
                        gatewayReachable = false,
                        error = "状态刷新任务失败",
                    };
                }

                _statusTask = null;
                Repaint();
            }

            if (_operationTask != null && _operationTask.IsCompleted)
            {
                if (_operationTask.Status == TaskStatus.RanToCompletion && _operationTask.Result != null)
                {
                    _operationMessage = _operationTask.Result.message ?? string.Empty;
                }
                else
                {
                    _operationMessage = "操作失败";
                }

                _operationTask = null;
                _nextRefreshTime = 0;
                Repaint();
            }

            if (_statusTask == null && _operationTask == null && EditorApplication.timeSinceStartup >= _nextRefreshTime)
            {
                _statusTask = UnityGatewayProcessController.QueryRuntimeStatusAsync();
                _nextRefreshTime = EditorApplication.timeSinceStartup + StatusRefreshIntervalSec;
            }
        }

        private void OnGUI()
        {
            DrawHeader();
            EditorGUILayout.Space(8f);
            DrawRuntimeStatus();
            EditorGUILayout.Space(8f);
            DrawControlButtons();
            EditorGUILayout.Space(8f);
            DrawPathAndConfig();
            EditorGUILayout.Space(8f);
            DrawHintBlock();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Unity Roslyn Gateway 控制面板", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "用于在 Unity Editor 内启动/停止外部网关进程，并查看网关与 Unity Agent 的连接状态。",
                MessageType.Info);
        }

        private void DrawRuntimeStatus()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                GatewayProcessState processState = UnityGatewayProcessController.ProcessState;
                EditorGUILayout.LabelField("网关进程状态", processState.ToString());
                EditorGUILayout.LabelField("进程信息", UnityGatewayProcessController.LastMessage);
                EditorGUILayout.LabelField("执行安全模式", UnityGatewayExecutionSettings.SecurityModeLabel);

                int pid = UnityGatewayProcessController.OwnedProcessId;
                EditorGUILayout.LabelField("进程 PID", pid > 0 ? pid.ToString() : "-");

                bool gatewayReachable = _runtimeStatus != null && _runtimeStatus.gatewayReachable;
                EditorGUILayout.LabelField("网关 HTTP", gatewayReachable ? "Online" : "Offline");

                string gatewayState = _runtimeStatus != null ? _runtimeStatus.gatewayState : string.Empty;
                string gatewayDetail = _runtimeStatus != null ? _runtimeStatus.gatewayDetail : string.Empty;
                EditorGUILayout.LabelField("网关状态", string.IsNullOrWhiteSpace(gatewayState) ? "-" : gatewayState);
                EditorGUILayout.LabelField("网关详情", string.IsNullOrWhiteSpace(gatewayDetail) ? "-" : gatewayDetail);

                bool unityConnected = _runtimeStatus != null && _runtimeStatus.unityConnected;
                EditorGUILayout.LabelField("Unity Agent连接", unityConnected ? "Connected" : "Disconnected");
                EditorGUILayout.LabelField("Unity Session", _runtimeStatus != null && !string.IsNullOrWhiteSpace(_runtimeStatus.unitySessionId) ? _runtimeStatus.unitySessionId : "-");
                EditorGUILayout.LabelField("最近心跳(UTC)", _runtimeStatus != null && !string.IsNullOrWhiteSpace(_runtimeStatus.lastHeartbeatUtc) ? _runtimeStatus.lastHeartbeatUtc : "-");

                if (_runtimeStatus != null && !string.IsNullOrWhiteSpace(_runtimeStatus.error))
                {
                    EditorGUILayout.HelpBox(_runtimeStatus.error, MessageType.Warning);
                }

                if (!string.IsNullOrWhiteSpace(_operationMessage))
                {
                    EditorGUILayout.HelpBox(_operationMessage, MessageType.None);
                }
            }
        }

        private void DrawControlButtons()
        {
            bool busy = _operationTask != null;
            GatewayProcessState processState = UnityGatewayProcessController.ProcessState;

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = !busy && processState != GatewayProcessState.Running && processState != GatewayProcessState.Starting;
                if (GUILayout.Button("启动网关", GUILayout.Height(28f)))
                {
                    StartGateway();
                }

                GUI.enabled = !busy && (processState == GatewayProcessState.Running || processState == GatewayProcessState.Error || processState == GatewayProcessState.NotRunning);
                if (GUILayout.Button("停止网关", GUILayout.Height(28f)))
                {
                    StopGateway();
                }

                GUI.enabled = !busy;
                if (GUILayout.Button("立即刷新状态", GUILayout.Height(28f)))
                {
                    ForceRefreshStatus();
                }

                GUI.enabled = true;
            }
        }

        private void DrawPathAndConfig()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("配置", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("网关 URL", UnityGatewayProcessController.GatewayBaseUrl);
                EditorGUILayout.LabelField("网关目录", UnityGatewayProcessController.GatewayToolDirectory);

                bool trustedModeEnabled = UnityGatewayExecutionSettings.TrustedModeEnabled;
                bool newTrustedModeEnabled = EditorGUILayout.ToggleLeft(
                    "Trusted Mode（允许执行 UnityEditor 命名空间，跳过 Roslyn 安全校验）",
                    trustedModeEnabled);

                if (newTrustedModeEnabled != trustedModeEnabled)
                {
                    UnityGatewayExecutionSettings.TrustedModeEnabled = newTrustedModeEnabled;
                    _operationMessage = $"已切换执行安全模式: {UnityGatewayExecutionSettings.SecurityModeLabel}";
                }

                string newPython = EditorGUILayout.TextField("Python 可执行路径", _pythonExecutable ?? string.Empty);
                if (newPython != _pythonExecutable)
                {
                    _pythonExecutable = newPython;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("保存 Python 路径", GUILayout.Height(24f)))
                    {
                        UnityGatewayProcessController.PythonExecutable = _pythonExecutable;
                        _pythonExecutable = UnityGatewayProcessController.PythonExecutable;
                        _operationMessage = $"已保存 Python 路径: {_pythonExecutable}";
                    }

                    string defaultPython = UnityGatewayProcessController.DefaultPythonExecutable;
                    if (GUILayout.Button($"恢复默认({defaultPython})", GUILayout.Height(24f)))
                    {
                        UnityGatewayProcessController.PythonExecutable = defaultPython;
                        _pythonExecutable = UnityGatewayProcessController.PythonExecutable;
                        _operationMessage = $"已恢复默认 Python 路径: {_pythonExecutable}";
                    }
                }
            }
        }

        private static void DrawHintBlock()
        {
            EditorGUILayout.HelpBox(
                "启动网关时会先检测 fastapi/uvicorn/pydantic，缺失则自动执行 pip 安装。\n" +
                "停止网关时会先请求优雅关闭，再尝试结束本地记录的进程。",
                MessageType.None);
        }

        private void StartGateway()
        {
            if (_operationTask != null)
            {
                return;
            }

            _operationMessage = "正在启动网关...";
            _operationTask = UnityGatewayProcessController.StartGatewayAsync();
            Repaint();
        }

        private void StopGateway()
        {
            if (_operationTask != null)
            {
                return;
            }

            _operationMessage = "正在停止网关...";
            _operationTask = UnityGatewayProcessController.StopGatewayAsync();
            Repaint();
        }

        private void ForceRefreshStatus()
        {
            if (_statusTask != null)
            {
                return;
            }

            _statusTask = UnityGatewayProcessController.QueryRuntimeStatusAsync();
            _nextRefreshTime = EditorApplication.timeSinceStartup + StatusRefreshIntervalSec;
            Repaint();
        }
    }
}
