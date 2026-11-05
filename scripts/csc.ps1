param(
  [string]$out,
  [string]$kind,        # dll | exe | winexe
  [string]$platform,    # anycpu | x86 | x64
  [string]$srcDir,
  [string[]]$refs
)
# Minimal C# compiler driver using the Roslyn assemblies shipped with PowerShell 7.
$pw = Split-Path ([System.Diagnostics.Process]::GetCurrentProcess().MainModule.FileName)
Add-Type -Path (Join-Path $pw 'Microsoft.CodeAnalysis.dll')
Add-Type -Path (Join-Path $pw 'Microsoft.CodeAnalysis.CSharp.dll')

$parseOpts = [Microsoft.CodeAnalysis.CSharp.CSharpParseOptions]::new([Microsoft.CodeAnalysis.CSharp.LanguageVersion]::CSharp7_3)
$trees = @()
foreach ($f in (Get-ChildItem -Path $srcDir -Filter *.cs -Recurse)) {
  $txt = [System.IO.File]::ReadAllText($f.FullName)
  $trees += [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText($txt, $parseOpts, $f.FullName, [System.Text.Encoding]::UTF8)
}
$mrefs = @()
$refs = @($refs | ForEach-Object { $_ -split ',' } | Where-Object { $_ -ne '' })
foreach ($r in $refs) { $mrefs +=[Microsoft.CodeAnalysis.MetadataReference]::CreateFromFile($r) }

switch ($kind) {
  'dll'    { $ok = [Microsoft.CodeAnalysis.OutputKind]::DynamicallyLinkedLibrary }
  'exe'    { $ok = [Microsoft.CodeAnalysis.OutputKind]::ConsoleApplication }
  'winexe' { $ok = [Microsoft.CodeAnalysis.OutputKind]::WindowsApplication }
}
switch ($platform) {
  'x86' { $pf = [Microsoft.CodeAnalysis.Platform]::X86 }
  'x64' { $pf = [Microsoft.CodeAnalysis.Platform]::X64 }
  default { $pf = [Microsoft.CodeAnalysis.Platform]::AnyCpu }
}
$opts = [Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions]::new($ok)
$opts = $opts.WithPlatform($pf).WithOptimizationLevel([Microsoft.CodeAnalysis.OptimizationLevel]::Release).WithAllowUnsafe($true)
$name = [System.IO.Path]::GetFileNameWithoutExtension($out)
$comp = [Microsoft.CodeAnalysis.CSharp.CSharpCompilation]::Create($name, [Microsoft.CodeAnalysis.SyntaxTree[]]$trees, [Microsoft.CodeAnalysis.MetadataReference[]]$mrefs, $opts)
$fs = [System.IO.File]::Create($out)
$res = $comp.Emit($fs)
$fs.Close()
$errs = 0
foreach ($d in $res.Diagnostics) {
  if ($d.Severity -eq [Microsoft.CodeAnalysis.DiagnosticSeverity]::Error) { $errs++; Write-Output ("ERR " + $d.ToString()) }
  elseif ($d.Severity -eq [Microsoft.CodeAnalysis.DiagnosticSeverity]::Warning -and $d.Id -ne 'CS1701' -and $d.Id -ne 'CS1702') { Write-Output ("WRN " + $d.ToString()) }
}
if (-not $res.Success) { Remove-Item $out -ErrorAction SilentlyContinue; Write-Output "BUILD FAILED ($errs errors)"; exit 1 }
Write-Output "OK $out"
