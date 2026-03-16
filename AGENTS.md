# AGENTS.md - RedPointSystemDemo 项目指南

本文档为 AI 代理提供在此 Unity 项目中工作的指南，包含构建命令、测试命令、代码风格和项目约定。

## 项目概述

- **项目名称**: RedPointSystemDemo (红点系统演示)
- **项目类型**: Unity 游戏项目
- **Unity 版本**: 2020.3.26f1c1 (LTS)
- **.NET 版本**: 4.7.1
- **目标平台**: Android (主要), 其他平台支持

## 构建和测试命令

### Unity 构建

此项目没有预配置的命令行构建脚本。构建方式：

1. **Unity Editor 构建**:
   - 打开 Unity Editor (2020.3.26f1c1)
   - File > Build Settings
   - 选择目标平台 (Android)
   - 点击 Build

2. **命令行构建** (可选):
   ```bash
   # 需要 Unity 命令行工具
   Unity -projectPath "D:\1_TempDir\RedPoint\RedPointSystemDemo" -buildTarget Android -executeMethod BuildScript.Build
   ```

### 测试

项目使用 **Unity Test Framework** (com.unity.test-framework 1.1.29):

1. **运行测试**:
   - 在 Unity Editor 中: Window > General > Test Runner
   - 选择 PlayMode 或 EditMode 测试
   - 点击 Run All 或选择单个测试运行

2. **测试位置**:
   - 第三方库测试: `Assets/3rd/AsyncAwaitUtil/Tests/`
   - 项目自定义测试: 暂无 (建议创建 `Assets/Tests/` 目录)

3. **测试命名约定**:
   - 测试类: `[功能名]Tests` (如 `RedPointSystemTests`)
   - 测试方法: `[场景]_[预期行为]` (如 `MailRead_ShouldUpdateRedPoint`)

### 代码质量检查

项目没有配置静态代码分析工具。建议:

1. **手动检查**:
   - 运行 Unity Editor 并检查 Console 窗口
   - 确保没有编译错误或警告

2. **建议添加**:
   - `.editorconfig` 文件统一代码风格
   - Unity 代码分析包

## 代码风格指南

### 命名约定

| 元素类型 | 约定 | 示例 |
|----------|------|------|
| **类名** | PascalCase | `RedPointSystem`, `MailModel`, `MainView` |
| **方法名** | PascalCase | `InitRedPointTreeNode`, `SetInvoke`, `GetNodeByName` |
| **属性** | PascalCase | `Instance`, `Mails`, `pointNum` |
| **成员变量** | m 前缀 + PascalCase | `mRootNode`, `mUpdateQueue`, `mInstance` |
| **局部变量** | camelCase | `node`, `tmpNode`, `task`, `strNode` |
| **常量** | PascalCase | `main`, `mail`, `mailSystem` (在 `RedPointConst` 类中) |
| **枚举值** | PascalCase | `System`, `Team`, `Alliance` |
| **事件** | on 前缀 + PascalCase | `onClick`, `onSelect`, `onPointerDown` |
| **委托** | On 前缀 + PascalCase | `OnPointNumChange` |

### 代码格式化

1. **缩进**: 使用 **4 个空格** (不是制表符)
2. **大括号风格**: Allman 风格 (左大括号独立一行)
   ```csharp
   private RedPointSystem()
   { 
   }
   
   public void InitRedPointTreeNode()
   {
       mRootNode = new RedPointNode();
       mRootNode.nodeName = RedPointConst.main;
   }
   ```
3. **行长度**: 建议不超过 120 个字符
4. **空行使用**:
   - 属性和方法之间使用空行分隔
   - 逻辑块之间使用空行分隔
   - 不同功能区域使用 `#region` 分隔

### 导入语句顺序

using 语句应按以下顺序分组:

1. System 命名空间
2. UnityEngine 命名空间
3. 第三方命名空间 (UnityEngine.UI, QFramework 等)
4. 项目内部命名空间

示例:
```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
```

### 注释规范

1. **XML 文档注释** (推荐):
   ```csharp
   /// <summary>
   /// 红点更新任务
   /// </summary>
   public class RedPointUpdateTask
   {
       // ...
   }
   ```

2. **中文注释**: 项目代码使用中文注释 (注意文件编码应为 UTF-8)
3. **TODO 注释**: 使用 `// TODO: [描述]` 标记待完成工作
4. **FIXME 注释**: 使用 `// FIXME: [问题描述]` 标记需要修复的问题

### 错误处理

1. **日志输出**:
   - 信息: `Debug.Log("描述信息")`
   - 警告: `Debug.LogWarning("警告信息")`
   - 错误: `Debug.LogError("错误信息")`

2. **错误返回值模式**:
   ```csharp
   public bool GetNodeRedPoint(string strNode, out RedPointNode redPointNode)
   {
       if (string.IsNullOrEmpty(strNode))
       {
           redPointNode = null;
           return false;
       }
       // ... 正常逻辑
   }
   ```

3. **异常处理**: 仅在必要时使用 try-catch，避免过度使用

### 异步编程

项目使用 **AsyncAwaitUtil** 和 **UniRx** 库:

1. **async/await 模式**:
   ```csharp
   private async Task startCheckUpdate()
   {
       while (true)
       {
           await new WaitForSeconds(0.1f);
           // ... 处理逻辑
       }
   }
   ```

2. **启动协程**:
   ```csharp
   // 使用 .Coroutine() 扩展方法
   startCheckUpdate().Coroutine();
   ```

3. **可用等待对象**:
   - `await new WaitForSeconds(seconds)`
   - `await new WaitForUpdate()`
   - `await new WaitForBackgroundThread()`
   - `await new WaitForSignal()`

### 设计模式

1. **单例模式** (核心模式):
   ```csharp
   private static RedPointSystem _instance;
   public static RedPointSystem Instance
   {
       get
       {
           if (_instance == null)
           {
               _instance = new RedPointSystem();
           }
           return _instance;
       }
   }
   private RedPointSystem() { }
   ```

2. **观察者模式** (事件/回调):
   ```csharp
   // 事件定义
   public delegate void OnPointNumChange(RedPointNode node);
   
   // 事件注册
   public event RedPointSystem.OnPointNumChange numChangeFunc;
   
   // 事件触发
   numChangeFunc?.Invoke(this);
   ```

## 项目结构

```
Assets/
├── scripts/                    # 游戏脚本
│   ├── BootStart.cs           # 启动入口
│   ├── common/                # 公共组件
│   │   └── UIEventListener.cs # QFramework UI 事件组件
│   ├── redPointSystem/        # 红点系统核心
│   │   ├── RedPointSystem.cs  # 红点系统主类
│   │   ├── RedPointNode.cs    # 红点节点类
│   │   └── RedPointConst.cs   # 红点常量定义
│   └── UI/                    # UI 相关脚本
│       ├── Mail/              # 邮件系统
│       │   ├── MailView.cs
│       │   ├── MailModel.cs
│       │   └── MailListView.cs
│       └── Main/              # 主界面
│           └── MainView.cs
├── 3rd/                       # 第三方库
│   ├── AsyncAwaitUtil/        # 异步工具库
│   │   ├── Source/            # 源代码
│   │   ├── UniRx/             # 响应式编程库
│   │   └── Tests/             # 测试
│   └── (其他第三方资源)
├── UI/                        # UI 资源
│   ├── 效果/                  # UI 效果图
│   ├── UI/                    # UI 素材
│   └── 资源包/                # 资源包
└── Scenes/                    # 场景文件
    └── SampleScene.unity      # 示例场景
```

## 第三方库

### 主要库

1. **AsyncAwaitUtil**: Unity 异步编程支持
   - 位置: `Assets/3rd/AsyncAwaitUtil/`
   - 用途: 提供 `async/await` 在 Unity 中的支持

2. **UniRx**: 响应式编程扩展
   - 位置: `Assets/3rd/AsyncAwaitUtil/UniRx/`
   - 用途: 响应式事件处理

3. **QFramework**: UI 事件框架
   - 位置: `Assets/scripts/common/UIEventListener.cs`
   - 用途: UI 事件监听和处理

### Unity 包依赖

查看 `Packages/manifest.json` 获取完整依赖列表，主要包含:
- `com.unity.test-framework`: 测试框架
- `com.unity.textmeshpro`: 文本渲染
- `com.unity.timeline`: 时间线工具
- `com.unity.ide.rider`: Rider IDE 支持
- `com.unity.ide.visualstudio`: VS IDE 支持

## AI 工具集成

### Unity Roslyn Gateway

项目包含 **Unity Roslyn Gateway** 工具，允许 AI 代理直接执行 Unity C# 代码:

1. **位置**: `Tools/UnityRoslynGateway/`
2. **启动服务器**:
   ```bash
   cd Tools/UnityRoslynGateway/
   python3 gateway_server.py
   ```
   - 默认端口: 19090
   - 需要 Unity Editor 正在运行

3. **执行代码**:
   ```bash
   python3 ai_gateway_client.py do-code --code "Debug.Log('Hello from AI');"
   ```

4. **使用场景**:
   - 创建/修改 Prefab
   - 管理 GameObject
   - 读取编辑器状态
   - 触发编译

### OpenSpec 工作流

项目配置了 **OpenSpec** 实验性工作流:

1. **技能位置**: `.opencode/skills/` (11个技能)
2. **主要技能**:
   - `openspec-new-change`: 创建新变更
   - `openspec-apply-change`: 实施变更任务
   - `openspec-verify-change`: 验证实施
   - `openspec-archive-change`: 归档完成变更

3. **使用方式**: 通过 `/opsx-*` 命令或直接调用技能

## 开发工作流

### 新功能开发

1. **分析需求**: 理解红点系统的节点关系
2. **设计架构**: 遵循现有单例和观察者模式
3. **实现代码**: 遵循命名约定和代码风格
4. **测试验证**: 使用 Unity Test Runner
5. **集成测试**: 确保红点更新正确触发

### 代码修改

1. **阅读现有代码**: 理解红点节点层级
2. **保持向后兼容**: 不破坏现有红点回调
3. **更新文档**: 修改相关注释和文档
4. **运行测试**: 确保现有功能正常

### 调试技巧

1. **红点调试**:
   ```csharp
   // 查看红点状态
   RedPointSystem.Instance.GetNodeRedPoint("mail.system", out var node);
   Debug.Log($"红点数量: {node?.pointNum}");
   ```

2. **事件跟踪**: 使用 `Debug.Log` 跟踪事件触发
3. **性能监控**: 注意 `Update()` 中的频繁操作

## 常见任务示例

### 添加新红点类型

1. 在 `RedPointConst.cs` 中添加常量:
   ```csharp
   public const string shop = "shop";
   public const string shopNewItem = "shop.newItem";
   ```

2. 在 `RedPointSystem.cs` 的 `lstRedPointTreeList` 中添加节点:
   ```csharp
   RedPointConst.shop,
   RedPointConst.shopNewItem,
   ```

3. 实现红点逻辑:
   ```csharp
   // 更新红点数量
   RedPointSystem.Instance.SetInvoke(RedPointConst.shopNewItem, count);
   ```

### 创建新 UI 组件

1. 遵循现有 UI 组件结构
2. 使用 `UIEventListener` 处理点击事件
3. 集成红点系统回调
4. 添加中文注释说明功能

## 注意事项

1. **编码问题**: 部分中文注释显示乱码，建议使用 UTF-8 编码
2. **性能考虑**: 红点系统使用队列更新，避免每帧检查
3. **内存管理**: 注意事件回调的注册和注销
4. **平台兼容**: 主要目标为 Android，但保持跨平台兼容性
5. **测试覆盖**: 目前测试覆盖不足，建议添加更多测试

---

*最后更新: 2026-03-16*  
*适用于: AI 代理、新开发者、代码审查*