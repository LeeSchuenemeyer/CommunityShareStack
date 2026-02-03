$versionFile = Join-Path $PSScriptRoot "..\\VERSION.txt"
$changelogFile = Join-Path $PSScriptRoot "..\\Pages\\Admin\\Changelog\\Index.cshtml.cs"

if (!(Test-Path $versionFile)) {
  throw "VERSION.txt not found."
}

$version = (Get-Content $versionFile -Raw).Trim()
$parts = $version.Split(".")
if ($parts.Length -ne 3) {
  throw "Version must be in MAJOR.MINOR.PATCH format."
}

$major = [int]$parts[0]
$minor = [int]$parts[1]
$patch = [int]$parts[2] + 1
$newVersion = "$major.$minor.$patch"

Set-Content -Path $versionFile -Value $newVersion

if (!(Test-Path $changelogFile)) {
  throw "Changelog file not found."
}

$date = Get-Date
$entry = @"
            new ChangelogEntry
            {
                Version = "$newVersion",
                Date = new DateTime($($date.Year), $($date.Month), $($date.Day)),
                Notes = new List<string>
                {
                    "TODO: add release notes"
                }
            },

"@

$content = Get-Content $changelogFile -Raw
$pattern = "public List<ChangelogEntry> Entries { get; set; } = new List<ChangelogEntry>\s*\{\s*"
if ($content -notmatch $pattern) {
  throw "Could not find changelog entries list."
}

$updated = [System.Text.RegularExpressions.Regex]::Replace($content, $pattern, "`$0`r`n$entry", 1)
Set-Content -Path $changelogFile -Value $updated

Write-Host "Bumped version to $newVersion and added changelog placeholder."
