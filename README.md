# LISS Text Lab · 文字处理实验室

一个离线优先的 Windows 文字工作台，将 2015–2016 年的 **改错字、分词工具、FindText** 整理成三个互通工作区。原来的想法保留在源码档案中，当前版本使用 .NET 10、WinForms 与独立核心库实现。

[作品介绍](https://heroliss.github.io/liss-works/projects/text-lab/) · [GitHub](https://github.com/heroliss/text-lab) · [Windows 最新版](https://github.com/heroliss/text-lab/releases/latest)

## 使用

解压 Windows x64 发行包，运行 `Liss.TextLab.exe`，无需安装 .NET。所有内容在本机处理，不上传文本或模型。

### 分词与词频

- 粘贴文字或打开文本，选择精确、搜索、全模式，然后开始分词。
- 同屏查看词元、原文位置、词频表和频次条形图；导出 UTF-8 文本。
- 加入不含空白的自定义词，对当前会话生效。
- “批量处理目录”处理 TXT / MD / CSV，保存相对目录结构。输出文件附加 `.segmented.txt`；输入与输出目录必须相互独立，原文不会覆盖。
- 分词由 **Faithlife.JiebaNet.Segmenter / Jieba** 提供：前缀词典 DAG、最大概率路径动态规划和 HMM。它属于第三方算法实现，应用侧负责处理流程、界面、编码、统计、取消与导出。

### 校对与词向量

- 内置 15 条独立整理的固定用语规则；建议明确显示依据，由用户逐条确认。它不是训练得到的纠错模型，未发现建议也不代表文本完全正确。
- 可自行导入有权使用的 word2vec 二进制 `.bin` 或 UTF-8 文本 `.vec` / `.txt`。文本第一行是 `词数 维数`，随后每行是 `词语 浮点数…`。
- 加载后使用归一化余弦相似度，按两侧窗口给出上下文参考；低相似度只表示语义差异，不直接判为错字。
- “语义近邻”接受以空格分隔的多个词，计算合成方向的近邻，排除查询词本身。
- 原 2016 `vectors.bin` 的训练来源与分发许可未能确认，因此没有打包、复制或公开。默认不附带 word2vec 词向量模型，也不声称具备通用中文纠错准确率。

### FindText 文件内容检索

- 选择实际目录，输入关键词，可设置扩展名、大小写、单文件大小和结果行上限。
- BOM 优先识别 UTF-8 / UTF-16 / UTF-32；无 BOM 时先完整验证 UTF-8，否则使用 GB18030。也可以手动指定无 BOM 文本编码。自动检测具有歧义，遇到编码不符时应手动选择。
- 每个文件只选择一种编码搜索，不会因双重解码输出重复结果。显示相对路径、行号、编码及上下文；一行多处命中保留出现次数。
- 后台扫描支持取消并保留已找到结果；遇到权限、删除、二进制内容或大小限制会记录跳过原因。符号链接和目录联接不跟随，避免环路。
- 预览使用已捕获的匹配行，不会在选中结果时打开整个大文件。可导出 JSON。

## 边界

当前面向 Windows x64。交互式分词最多 200 万 UTF-16 字符，校对最多 20 万，批量单文件最多 8 MiB。跨 4,096 字符处理块边界的长词可能拆开；优先在句末分块。

文件搜索默认单文件 64 MiB、10,000 个结果行；可在界面提高至 2 GiB、100 万行。KMP 匹配跨读取块连续进行，极长单行也只保留有限上下文；大量结果行仍会消耗更多内存，请根据机器内存设置。批量处理中途取消保留已完成文件；输出失败会提示，不会删除原文。

词向量文件最多 512 MiB、200 万词、4,096 维，并且向量元素总数最多 1 亿。导入会校验重复词、截断、非法维数、零向量和非有限浮点数。近邻使用逐词扫描与固定大小优先队列，不构建词数平方的矩阵；加载与检索可取消。实际内存还包含词表、索引和 .NET 对象开销。

## 开发与验证

```powershell
dotnet build src/TextLab.App/TextLab.App.csproj -c Release
dotnet run --project tests/TextLab.Checks/TextLab.Checks.csproj -c Release -- artifacts/core-checks.json
powershell -File scripts/publish.ps1
```

核心检查覆盖中文分词与位置、重叠搜索词、词频、自定义词、Unicode 分块、规则应用与过期保护、双侧上下文、文本与二进制 word2vec、损坏模型、编码、去重、取消与部分结果、大小与结果限制、长行预览、批量输出隔离。

真实场景入口（正常窗口和相同业务代码）：

```powershell
.\Liss.TextLab.exe --showcase=segment
.\Liss.TextLab.exe --showcase=proofread
.\Liss.TextLab.exe --showcase=search
```

搜索场景仅在临时目录 `Liss.TextLab/showcase` 写入公开合成示例，包括 UTF-8、GB18030 和 C# 文件，不读取个人资料。

## 来源

`legacy/manifest.json` 记录 11 个原始业务源码文件的相对备份位置、字节数和 SHA-256。它们逐字节保存，不参与当前编译。没有复制旧可执行文件、数据库、证书、模型、语料或用户配置。

保留的思路包括：word2vec 读取和归一化、局部上下文相似度、人工确认候选、批量分词与编码转换，以及带行号的递归文件检索。当前文本规则是新增的小型规则集，不冒充原模型效果。NLPIR 原生依赖已由开放的 Jieba 实现替代。

第三方说明与许可证见 `THIRD-PARTY-NOTICES.md` 和 `licenses/`。
