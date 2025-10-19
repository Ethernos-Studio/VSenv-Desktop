# VSenv Desktop

一个现代化的 WPF 桌面应用程序，为 [vsenv](https://github.com/dhjs0000/vsenv) 提供图形用户界面。

![License](https://img.shields.io/badge/license-GPL3-blue.svg)
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey.svg)
![Framework](https://img.shields.io/badge/framework-.NET%208.0-purple.svg)
![UI](https://img.shields.io/badge/UI-WPF%20Modern-blue.svg)
[![GitHub release](https://img.shields.io/github/release/Ethernos-Studio/VSenv-Desktop.svg)](https://github.com/Ethernos-Studio/VSenv-Desktop/releases/)
[![GitHub stars](https://img.shields.io/github/stars/Ethernos-Studio/VSenv-Desktop.svg)](https://github.com/Ethernos-Studio/VSenv-Desktop/stargazers)
[![GitHub issues](https://img.shields.io/github/issues/Ethernos-Studio/VSenv-Desktop.svg)](https://github.com/Ethernos-Studio/VSenv-Desktop/issues)
[![GitHub forks](https://img.shields.io/github/forks/Ethernos-Studio/VSenv-Desktop.svg)](https://github.com/Ethernos-Studio/VSenv-Desktop/network)

![Stars](https://starchart.cc/Ethernos-Studio/VSenv-Desktop.svg)

## 📊 项目统计

![GitHub commit activity](https://img.shields.io/github/commit-activity/m/Ethernos-Studio/VSenv-Desktop)
![GitHub last commit](https://img.shields.io/github/last-commit/Ethernos-Studio/VSenv-Desktop)
![GitHub repo size](https://img.shields.io/github/repo-size/Ethernos-Studio/VSenv-Desktop)
![Lines of code](https://img.shields.io/tokei/lines/github/Ethernos-Studio/VSenv-Desktop)

## 🚦 开发状态

| 状态 | 徽章 |
|------|------|
| 版本 | ![GitHub release (latest by date)](https://img.shields.io/github/v/release/Ethernos-Studio/VSenv-Desktop) |
| 下载量 | ![GitHub all releases](https://img.shields.io/github/downloads/Ethernos-Studio/VSenv-Desktop/total) |
| 语言 | ![GitHub top language](https://img.shields.io/github/languages/top/Ethernos-Studio/VSenv-Desktop) |
| 问题 | ![GitHub open issues](https://img.shields.io/github/issues-raw/Ethernos-Studio/VSenv-Desktop) |
| 拉取请求 | ![GitHub pull requests](https://img.shields.io/github/issues-pr-raw/Ethernos-Studio/VSenv-Desktop) |

## 📑 目录

- [概述](#概述)
- [特性](#特性)
- [技术栈](#技术栈)
- [快速开始](#快速开始)
- [使用说明](#使用说明)
- [项目结构](#项目结构)
- [依赖关系](#依赖关系)
- [许可证](#许可证)
- [相关项目](#相关项目)
- [贡献](#贡献)
- [免责声明](#免责声明)
- [Star 历史](#star-历史)

## 概述

VSenv Desktop 是一个基于 WPF 的 GUI 框架，用于管理 VSenv 实例。请注意，这个程序仅提供用户界面，核心的 VSenv 功能实现位于独立的 vsenv 项目中。

## 特性

- 🎨 **现代 UI**: 采用 Windows 11 设计语言的现代化界面
- 🌓 **主题支持**: 自动同步系统深色/浅色主题
- 🧭 **直观导航**: 使用 NavigationView 的页面导航结构
- ⚙️ **实例管理**: 图形化界面管理 VS Code 实例
- 🔒 **隔离功能**: 支持沙箱模式、随机主机名、MAC 地址等隐私保护功能
- 🌐 **代理支持**: 内置 HTTP(S) 代理配置界面

## 技术栈

- **框架**: .NET 8.0 + WPF
- **UI 库**: iNKORE.UI.WPF.Modern, FluentWPF, ModernWpfUI
- **平台**: Windows 10/11

## 快速开始

### 前提条件
- Windows 10 或更高版本
- .NET 8.0 Runtime
- [vsenv](https://github.com/dhjs0000/vsenv) 核心程序已安装

### 安装
1. 从 Releases 页面下载最新版本
2. 解压到任意目录
3. 运行 `VSenvDesktop.exe`

### 从源码构建
```bash
# 克隆仓库
git clone https://github.com/Ethernos-Studio/VSenv-Desktop.git

# 进入项目目录
cd VSenv-Desktop

# 构建项目
dotnet build

# 运行应用
dotnet run
```

## 使用说明

1. **启动应用**: 运行 VSenvDesktop.exe
2. **创建实例**: 在"实例管理"页面创建新的 VS Code 实例
3. **配置选项**: 在首页设置沙箱、代理等高级选项
4. **启动 VS Code**: 选择实例并点击启动按钮

## 项目结构

```
VSenv Desktop/
├── App.xaml/.cs              # 应用程序入口
├── MainWindow.xaml/.cs       # 主窗口
├── Services/
│   └── NavigationService.cs  # 导航服务
└── Pages/                    # 各个功能页面
    ├── HomePage.xaml/.cs     # 首页
    ├── InstancesPage.xaml/.cs # 实例管理
    ├── SettingsPage.xaml/.cs # 设置
    └── AboutPage.xaml/.cs    # 关于
```

## 依赖关系

这个 GUI 应用程序依赖于:
- **vsenv**: 核心功能实现（命令行工具）
- **.NET 8.0**: 运行时环境
- **ModernWPF**: UI 框架

## 许可证

本项目采用 [GNU General Public License v3.0](LICENSE) 许可证。

## 相关项目

- [vsenv](https://github.com/dhjs0000/vsenv) - VSenv 核心实现（命令行版本）
- [VSenv 文档](https://dhjs0000.github.io/VSenv/helps.html) - 详细使用文档

## 贡献

欢迎提交 Issue 和 Pull Request！

## 免责声明

本软件完全免费开源，仅供学习研究使用。使用本软件即表示您同意承担所有相关风险。

## Star 历史

![Star History Chart](https://api.star-history.com/svg?repos=Ethernos-Studio/VSenv-Desktop&type=Date)

---

<div align="center">

### 🔗 相关链接

[![VSenv Core](https://img.shields.io/badge/vsenv-core-green.svg)](https://github.com/dhjs0000/vsenv)
[![Documentation](https://img.shields.io/badge/docs-online-blue.svg)](https://dhjs0000.github.io/VSenv/helps.html)
[![Issues](https://img.shields.io/badge/issues-welcome-green.svg)](https://github.com/Ethernos-Studio/VSenv-Desktop/issues)

### 📱 社交

[![GitHub followers](https://img.shields.io/github/followers/Ethernos-Studio?style=social)](https://github.com/Ethernos-Studio)
[![GitHub stars](https://img.shields.io/github/stars/Ethernos-Studio/VSenv-Desktop?style=social)](https://github.com/Ethernos-Studio/VSenv-Desktop/stargazers)

</div>