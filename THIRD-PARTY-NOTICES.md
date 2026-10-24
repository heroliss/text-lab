# Third-party notices

## Faithlife.JiebaNet.Segmenter 0.1.1

MIT licensed, derived from anderscui/jieba.NET and fxsjy/jieba. Provides segmentation, dictionary resources and HMM data. The Faithlife build embeds/compresses its resources, so the release requires no external dictionary download.

- https://github.com/Faithlife/jieba.NET
- https://www.nuget.org/packages/Faithlife.JiebaNet.Segmenter/0.1.1
- https://github.com/Faithlife/jieba.NET/blob/master/LICENSE
- https://github.com/fxsjy/jieba/blob/master/LICENSE

Exact MIT notice is retained in `licenses/jieba.NET-MIT.txt` and `licenses/jieba-MIT.txt`. Dictionary/HMM resources are distributed by the upstream repository/package under its MIT license; no NLPIR proprietary DLL or data is included.

## Newtonsoft.Json

Transitive dependency of the segmentation package, MIT licensed. See `licenses/Newtonsoft.Json-MIT.txt`.

## .NET runtime / Windows Forms

The self-contained Windows release redistributes the Microsoft .NET runtime. See the runtime notices in `licenses/dotnet-LICENSE.txt` and `licenses/dotnet-THIRD-PARTY-NOTICES.txt`.

## Historical code and models

Original source snapshots may contain references to historical NLPIR, Jieba, or Microsoft Pinyin APIs. They are archived for provenance and not compiled or redistributed as binary dependencies. Their former DLLs and data are not included.

The unknown-provenance `vectors.bin` from the old backup is excluded. Imported models remain on the user's machine and are not bundled, uploaded, or copied into the repository.
