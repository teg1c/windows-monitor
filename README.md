# 灵讯哨

这是一个 .NET WinForms 桌面程序，用外部截图 + OCR 的方式监控你选择的来源，并在识别文本命中关键词时提醒。来源可以选择窗口、整个桌面或某个显示器；右侧预览可以选择性框选局部区域，不框选时会 OCR 整个所选来源。

软件名：灵讯哨  
英文/可执行文件名：windows-monitor  
作者：tegic  
联系方式：35350826  
GitHub：[https://github.com/teg1c](https://github.com/teg1c)  
图标资产：`Assets/windows-monitor.ico`

边界说明：

- 不注入游戏进程，不读取游戏内存，不安装键鼠钩子，不隐藏或伪装进程，不绕过检测。
- 不能保证第三方程序不会被游戏检测到。请只在游戏规则允许的范围内使用窗口截图和提醒。

## UI 库

界面使用 AntdUI 作为 WinForms 第三方 UI 库：

- NuGet 包：`AntdUI`
- 中文文档：https://gitee.com/AntdUI/AntdUI/blob/main/doc/wiki/zh/Home.md

## 运行

```powershell
dotnet run --project .\windows-monitor.csproj
```

## 编译

```powershell
.\build.ps1
.\dist\windows-monitor.exe
```

构建完成后会生成：

- `release/windows-monitor.zip`：应用完整压缩包
- `release/windows-monitor-setup.exe`：安装/卸载程序

## 安装与更新

下载 `windows-monitor-setup.exe` 后直接运行即可安装。已安装时再次运行同一个 exe，可以选择安装/更新或卸载。

程序启动后会后台检查 GitHub 最新 Release，界面右上角也有“检查更新”按钮。发现新版本时，会下载最新 `windows-monitor-setup.exe` 并启动在线更新。

远程更新源：
```text
https://github.com/teg1c/windows-monitor/releases/latest
```

## GitHub Actions

仓库推送后会自动构建并上传 artifact。打版本标签时会自动发布 GitHub Release：

```powershell
git tag v1.0.0
git push origin v1.0.0
```

## 使用方式

1. 点击“刷新窗口”，选择窗口、整个桌面或某个显示器。
2. 点击“共享预览”，右侧会实时显示所选来源。
3. 可选：在预览图上拖拽框选要 OCR 的局部区域；不框选时监控整个所选来源。
4. 选择 OCR 模式，并在“命中关键词”里每行填写一个关键词。
5. 在“通知”页勾选一个或多个通知渠道。
6. 点击“开始监控”。

程序只做关键词监控：按 OCR 间隔识别当前监控画面，文本包含任意关键词就发送通知，并按冷却秒数去重。同一命中内容未变化时最多提醒 3 次；OCR 未命中后会重置计数。没有配置关键词时不能开始监控。

## OCR 模式

### wxocr API

保留原有 API 方式，默认地址：

```text
http://192.168.88.3:5000/ocr
```

调用格式：

```http
POST /ocr
Content-Type: application/json

{"image":"PNG 图片 base64"}
```

已兼容 `{"result":{"ocr_response":[...]}}` 和 `{"ocr_response":[...]}` 两种返回结构。

### 内置本地 OCR

选择 `local` 模式时，程序使用内置 PaddleOCR ChineseV5 本地模型识别，不需要启动 API 服务，也不需要安装 Tesseract。首次识别会加载模型，发布目录会因为内置模型和推理运行时明显变大。

### 本地命令

需要接自定义 OCR 命令时，选择 `command` 模式。程序会把截图保存成临时 PNG，然后调用本地命令，并读取 stdout。

默认按 Tesseract 配置：

```text
命令：tesseract
参数："{image}" stdout -l chi_sim+eng --psm 6
```

其中 `{image}` 会替换成本次临时截图路径。

也可以接 RapidOCR 或你自己的命令行封装，只要 stdout 输出识别文本即可。

## 推荐 OCR 组件

- Tesseract OCR：轻量、成熟、部署简单。适合先跑通本地离线 OCR。
- PaddleOCR：中文识别通常更强，但依赖更重，适合后续追求识别率。
- RapidOCR：偏轻量的开源 OCR 工具链，适合离线部署和命令行封装。

## 调参建议

- 误报频繁：缩小框选区域，避开底部按钮、动画、闪烁光效、鼠标经过的位置，或增加冷却秒数。
- 漏识别：扩大一点系统消息区域，降低 wxocr 最低置信度，或换更适合中文游戏字体的 OCR 引擎。
- Tesseract 识别差：确认安装了 `chi_sim` 中文语言包，并尝试调 `--psm` 参数。

配置保存到 `config.local.json`。

## Webhook 自定义

通知页支持多个 Webhook 渠道。你可以新增多个渠道，并在渠道列表里同时勾选多个启用项；命中关键词时会逐个发送，单个渠道失败会写入日志，不影响其它渠道。

每个渠道支持：

- 渠道预设：`generic`、`serverchan`、`bark`、`dingtalk`、`feishu`、`wecom`、`custom`
- 请求方法：默认 `POST`
- URL 模板
- Headers，每行一个，例如 `Content-Type: application/json`
- Body 模板

常用变量：

```text
{title} {body} {message} {window} {distance} {region} {ocrText} {time} {timestamp}
```

JSON 模板里直接用 `{body}`；URL 或表单场景可用 `{bodyUrl}`、`{titleUrl}`；纯文本场景可用 `{bodyRaw}`、`{titleRaw}`。

## 日志

程序会记录监控过程中的错误和关键事件，例如：

- 窗口截图失败
- OCR 调用失败
- Webhook 发送失败
- 提醒发送成功

日志文件：

```text
logs/app.log
```

界面“日志”页签可以查看、刷新、清空日志，并设置最大日志行数。默认最大保留 `1000` 行，避免日志无限增长。
