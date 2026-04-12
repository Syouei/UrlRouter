# UrlPrompt

A lightweight Windows utility that intercepts `http/https` link launches, asks for confirmation, and then forwards the URL to a real browser.

## Language
- English (default): this document
- 中文版: see [中文版](#中文版)

## Why
Some apps open links immediately. UrlPrompt adds a confirmation step so you can:
- review the URL before opening
- edit/copy the URL
- choose which browser should actually open it

## Features
- Intercepts `http` and `https` links (after protocol association)
- Shows a confirmation dialog before opening
- Lets you edit the URL (HTTP/HTTPS validation included)
- Supports browser selection:
  - Edge
  - Chrome
  - Firefox
  - Custom browser executable path
- Saves your browser preference to local config
- Writes simple open/error logs

## Requirements
- Windows 10/11
- .NET 8 runtime (for framework-dependent build)
- For development: .NET 8 SDK

## Quick Start
1. Build or publish `UrlPrompt.exe`.
2. Put `install.bat` in the same folder as `UrlPrompt.exe`.
3. Run `install.bat`.
4. In Windows **Default apps**, set `UrlPrompt` as handler for `HTTP` and `HTTPS` protocols.

After setup, any app opening a web link through system protocol handling will first show UrlPrompt.

## Installation / Uninstallation

### Preferred (portable)
- Install: run `install.bat`
- Uninstall: run `uninstall.bat`

`install.bat` dynamically detects the current folder and writes registry keys under `HKCU` (current user only), so no fixed drive path is required.

### `.reg` files
- `install.reg` is a template (contains placeholders), mainly for reference.
- `uninstall.reg` can remove relevant keys.

## Build

```bash
dotnet build -c Release
```

Optional publish (self-contained example):

```bash
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

## Configuration and Logs
UrlPrompt stores data in:
- Config: `%LocalAppData%\UrlPrompt\config.json`
- Log: `%LocalAppData%\UrlPrompt\log.txt`

## Project Structure
- `main.cs`: all core logic (URL extraction, prompt UI, browser resolution, launch, config/log handling)
- `UrlPrompt.csproj`: .NET project configuration
- `install.bat`: portable installer (dynamic registry registration)
- `uninstall.bat`: registry cleanup script
- `install.reg`: registration template (placeholder-based)
- `uninstall.reg`: registry cleanup template

## Security Notes
- UrlPrompt only accepts absolute `http/https` URLs in the final open step.
- It does not sandbox browser behavior; it only controls pre-open confirmation and forwarding.

## Limitations
- Windows-only (WinForms + Windows protocol association)
- You still need to set UrlPrompt as the default protocol handler in system settings

## License
This project is licensed under the MIT License. See `LICENSE` for details.

---

## 中文版

`UrlPrompt` 是一个轻量级 Windows 工具：拦截系统要打开的 `http/https` 链接，先弹窗确认，再转发到你指定的真实浏览器。

### 主要用途
有些应用会直接打开链接。UrlPrompt 提供一个“打开前确认”层，方便你：
- 先检查链接是否可信
- 按需修改链接
- 快速复制链接
- 选择最终由哪个浏览器打开

### 功能特性
- 拦截 `http` / `https` 协议链接（在完成协议关联后）
- 打开前弹窗确认
- 支持编辑 URL，并做 `http/https` 合法性校验
- 支持浏览器选择：
  - Edge
  - Chrome
  - Firefox
  - 自定义 EXE 路径
- 自动保存浏览器偏好
- 记录简单日志（打开/错误）

### 环境要求
- Windows 10/11
- .NET 8 Runtime（框架依赖发布时）
- 开发需要 .NET 8 SDK

### 快速使用
1. 构建或发布 `UrlPrompt.exe`
2. 确保 `install.bat` 与 `UrlPrompt.exe` 在同一目录
3. 运行 `install.bat`
4. 在 Windows“默认应用”中把 `HTTP` 和 `HTTPS` 协议处理器设置为 `UrlPrompt`

完成后，应用通过系统协议打开网页时，会先经过 UrlPrompt 弹窗。

### 安装与卸载
- 推荐安装：运行 `install.bat`
- 推荐卸载：运行 `uninstall.bat`

`install.bat` 会动态获取当前目录路径，并将注册信息写入 `HKCU`（仅当前用户），不依赖固定盘符路径。

### `.reg` 文件说明
- `install.reg`：模板文件（包含占位符），主要用于参考
- `uninstall.reg`：可用于删除相关注册项

### 构建命令

```bash
dotnet build -c Release
```

可选发布（示例：单文件自包含）：

```bash
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

### 配置与日志位置
- 配置文件：`%LocalAppData%\UrlPrompt\config.json`
- 日志文件：`%LocalAppData%\UrlPrompt\log.txt`

### 文件结构
- `main.cs`：核心逻辑（URL 提取、弹窗、浏览器解析、启动、配置/日志）
- `UrlPrompt.csproj`：项目配置
- `install.bat`：可移植安装脚本
- `uninstall.bat`：卸载清理脚本
- `install.reg`：注册模板
- `uninstall.reg`：卸载模板

### 安全说明
- 最终打开前只接受绝对 `http/https` URL
- 该工具不隔离浏览器本身行为，仅负责“打开前确认 + 转发”

### 当前限制
- 仅支持 Windows
- 仍需在系统“默认应用”中手动将协议关联到 UrlPrompt

### 许可证
本项目采用 MIT 许可证，详见 `LICENSE` 文件。
