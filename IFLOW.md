# VSenv Desktop - 项目概述

## 项目简介

VSenv Desktop 是一个基于 WPF (Windows Presentation Foundation) 的桌面应用程序，用于管理 VSenv 实例。VSenv 是一个开源的 VS Code 环境管理工具，允许用户创建、管理和隔离多个 VS Code 实例。

## 技术栈

- **框架**: .NET 8.0 + WPF
- **UI 库**: 
  - iNKORE.UI.WPF.Modern (现代 UI 控件)
  - FluentWPF (流畅设计)
  - ModernWpfUI (现代 WPF 控件)
- **语言**: C# (可空引用类型启用)
- **平台**: Windows (仅支持 Windows)

## 项目结构

```
VSenv Desktop/
├── App.xaml/.cs              # 应用程序入口和主题管理
├── MainWindow.xaml/.cs       # 主窗口和导航框架
├── ThemeHelper.cs            # 系统主题同步助手
├── Services/
│   └── NavigationService.cs  # 页面导航服务
└── Pages/                    # 应用程序页面
    ├── HomePage.xaml/.cs     # 首页（实例选择和启动）
    ├── InstancesPage.xaml/.cs # 实例管理页面
    ├── DownloadPage.xaml/.cs # 下载页面
    ├── VSenvInfo.xaml/.cs    # VSenv 信息页面
    ├── SettingsPage.xaml/.cs # 设置页面
    └── AboutPage.xaml/.cs    # 关于页面
```

## 主要功能

1. **实例管理**: 创建、启动、停止和删除 VS Code 实例
2. **环境隔离**: 支持沙箱模式、随机主机名、随机 MAC 地址等隔离功能
3. **代理支持**: 配置 HTTP(S) 代理
4. **主题同步**: 自动同步 Windows 系统主题（深色/浅色）
5. **现代 UI**: 使用 Windows 11 风格的现代界面设计

## 构建和运行

### 前提条件
- .NET 8.0 SDK
- Windows 操作系统
- Visual Studio 2022 或更高版本（推荐）

### 构建命令
```bash
# 还原 NuGet 包
dotnet restore

# 构建项目
dotnet build

# 运行项目
dotnet run

# 发布项目
dotnet publish -c Release -r win-x64 --self-contained
```

### Visual Studio 操作
- 打开 `VSenv Desktop.sln`
- 按 F5 或点击"开始调试"按钮运行
- 使用 Ctrl+F5 进行无调试运行

## 开发约定

### 代码风格
- 使用可空引用类型 (`<Nullable>enable</Nullable>`)
- 使用隐式全局 using (`<ImplicitUsings>enable</ImplicitUsings>`)
- 遵循 C# 命名约定

### UI 设计
- 使用 ModernWPF 和 iNKORE.UI.WPF.Modern 控件
- 遵循 Windows 11 设计语言
- 支持深色和浅色主题自动切换
- 使用 Mica 背景效果

### 架构模式
- 使用页面导航模式 (NavigationView + Frame)
- 服务类用于核心功能 (如 NavigationService)
- 助手类用于特定功能 (如 ThemeHelper)

## 核心依赖

- **iNKORE.UI.WPF.Modern**: 现代 WPF UI 控件库
- **FluentWPF**: 流畅设计支持
- **ModernWpfUI**: 现代 WPF UI 框架
- **System.Text.Encoding.CodePages**: 编码支持

## 注意事项

- 应用程序需要管理员权限来执行某些功能（如修改主机名、MAC 地址）
- 支持 VS Code 扩展管理和协议注册
- 完全开源，采用 AGPL v3.0 许可证