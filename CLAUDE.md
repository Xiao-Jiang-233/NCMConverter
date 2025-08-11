# CLAUDE.md

此文件为 Claude Code (claude.ai/code) 在处理此代码库中的代码时提供指导。

## 项目概述

这是一个 WPF 桌面应用程序，用于将 NCM 文件（网易云音乐加密文件）转换为标准音频格式，如 MP3 或 FLAC。该应用程序使用 `libncmdump` 库进行实际的解密过程。

## 架构

该应用程序遵循 MVVM (Model-View-ViewModel) 模式：

1. **View (MainWindow.xaml)**: 使用 Material Design 主题的 UI 布局
2. **ViewModel (MainViewModel.cs)**: 包含应用程序逻辑、数据绑定和命令处理
3. **Model (FileItem)**: 表示转换队列中的文件
4. **Services**:
   - `NcmConvertService.cs`: 使用 ncmdump.exe 处理实际的 NCM 文件转换
   - `LibDownloader.cs`: 管理 libncmdump 库的自动下载和更新
   - `NeteaseCrypt.cs`: libncmdump.dll 的 P/Invoke 包装器（目前在主流程中未使用）

## 主要组件

### 主要组件
- `MainWindow.xaml` - 主应用程序窗口 UI
- `MainViewModel.cs` - 应用程序逻辑和数据管理
- `NcmConvertService.cs` - NCM 文件转换服务
- `LibDownloader.cs` - 库更新管理

### 数据流
1. 用户通过 UI 添加 NCM 文件（文件/文件夹对话框）
2. 文件被添加到 ViewModel 中的 ObservableCollection
3. 点击"开始处理"时，每个文件按顺序处理
4. NcmConvertService 调用 ncmdump.exe 转换文件
5. 进度和状态更新显示在 UI 中

## 构建和开发命令

### 构建项目
```bash
dotnet build
```

### 运行应用程序
```bash
dotnet run --project NCMConverter/NCMConverter.csproj
```

### 发布应用程序
```bash
dotnet publish -c Release -r win-x64 --self-contained true
```

### 恢复包
```bash
dotnet restore
```

## 开发注意事项

1. 应用程序需要 `libncmdump.dll` 和 `ncmdump.exe` 才能运行
2. 这些库在首次运行时会自动从 GitHub 发布版本下载
3. 应用程序支持将文件转换到其源目录或自定义目录
4. UI 使用 Material Design 主题
5. 应用程序使用 Windows Forms 对话框进行文件夹浏览（与 WPF 互操作）

## 依赖项

- .NET 8.0
- MaterialDesignThemes
- MaterialDesignColors
- libncmdump (用于 NCM 解密的外部库)