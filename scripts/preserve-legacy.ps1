param([string]$BackupRoot = 'E:\--Backup--\手动备份\自写程序备份\自写程序备份')
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$files = @(
    @('2016年\2016.10.31改错字\源码\gaicuozi\WordVectors.cs', '2016-proofreading\WordVectors.cs'),
    @('2016年\2016.10.31改错字\源码\gaicuozi\MarkedText.cs', '2016-proofreading\MarkedText.cs'),
    @('2016年\2016.10.31改错字\源码\gaicuozi\Form1.cs', '2016-proofreading\Form1.cs'),
    @('2016年\2016.10.31改错字\源码\gaicuozi\ColorConverter.cs', '2016-proofreading\ColorConverter.cs'),
    @('2016年\分词工具\分词工具\分词工具\Form1.cs', '2016-segmentation\Form1.cs'),
    @('2016年\分词工具\分词工具\分词工具\ParagraphProcess.cs', '2016-segmentation\ParagraphProcess.cs'),
    @('2016年\FindText1.0\FindText\Program.cs', '2016-findtext\Program.cs'),
    @('2015年\分词程序 - 原版\分词程序\分词类.cs', '2015-segmentation-original\分词类.cs'),
    @('2015年\分词程序 - 原版\分词程序\Form1.cs', '2015-segmentation-original\Form1.cs'),
    @('2015年\分词程序 - web版\分词程序\分词类.cs', '2015-segmentation-web\分词类.cs'),
    @('2015年\分词程序 - web版\分词程序\Form1.cs', '2015-segmentation-web\Form1.cs')
)
$manifest = foreach ($item in $files) {
    $source = Join-Path $BackupRoot $item[0]
    $destination = Join-Path (Join-Path $taskRoot 'legacy') $item[1]
    New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null
    if (!(Test-Path -LiteralPath $destination)) { Copy-Item -LiteralPath $source -Destination $destination }
    $sourceHash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
    $destinationHash = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash
    if ($sourceHash -ne $destinationHash) { throw "Original hash mismatch: $destination" }
    [pscustomobject]@{ source = $item[0]; archived = ('legacy\' + $item[1]); sha256 = $sourceHash.ToLowerInvariant(); bytes = (Get-Item -LiteralPath $destination).Length }
}
$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $taskRoot 'legacy\manifest.json') -Encoding utf8
