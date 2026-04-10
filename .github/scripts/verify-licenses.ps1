param(
    [string]$RootPath = ".",
    [string]$PolicyFile = ".github/license-policy.json"
)

$ErrorActionPreference = "Stop"

function Get-Json {
    param([string]$Path)

    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw "Required file not found: $Path"
    }

    return (Get-Content -Path $Path -Raw | ConvertFrom-Json -Depth 64)
}

function Normalize-LicenseExpressionTokens {
    param([string]$Expression)

    if ([string]::IsNullOrWhiteSpace($Expression)) {
        return @()
    }

    $clean = $Expression -replace "\(", " " -replace "\)", " " -replace "\+", " "
    $parts = $clean -split "\s+OR\s+|\s+AND\s+|\s+WITH\s+|\s+" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

    return $parts | ForEach-Object { $_.Trim() } | Where-Object { $_ -and ($_ -ne "OR") -and ($_ -ne "AND") -and ($_ -ne "WITH") } | Select-Object -Unique
}

function ConvertTo-HashSet {
    param([object[]]$Items)

    $set = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($item in $Items) {
        if (-not [string]::IsNullOrWhiteSpace($item)) {
            [void]$set.Add($item)
        }
    }

    return $set
}

function Validate-Licenses {
    param(
        [string[]]$Tokens,
        [System.Collections.Generic.HashSet[string]]$Allowed,
        [string]$PackageId,
        [string]$Version,
        [string]$Source,
        [System.Collections.Generic.List[string]]$Failures
    )

    if ($Tokens.Count -eq 0) {
        $Failures.Add("$Source package $PackageId@$Version has no parseable SPDX license expression.")
        return
    }

    foreach ($token in $Tokens) {
        if (-not $Allowed.Contains($token)) {
            $Failures.Add("$Source package $PackageId@$Version has disallowed license token '$token'.")
        }
    }
}

$root = Resolve-Path $RootPath
$policyPath = Join-Path $root $PolicyFile
$policy = Get-Json -Path $policyPath

$allowedSet = ConvertTo-HashSet -Items $policy.allowedSpdxIds
$failures = New-Object 'System.Collections.Generic.List[string]'

# Validate blocked NuGet package/version policy from direct package references
$csprojFiles = Get-ChildItem -Path $root -Recurse -Filter *.csproj | Select-Object -ExpandProperty FullName
$blockedRules = @()
if ($policy.blockedNuGetPackages) {
    $blockedRules = @($policy.blockedNuGetPackages)
}

foreach ($csprojPath in $csprojFiles) {
    [xml]$xml = Get-Content -Path $csprojPath -Raw
    $packageRefs = $xml.Project.ItemGroup.PackageReference
    if (-not $packageRefs) {
        continue
    }

    foreach ($ref in @($packageRefs)) {
        $id = $ref.Include
        $version = $ref.Version
        if ([string]::IsNullOrWhiteSpace($id) -or [string]::IsNullOrWhiteSpace($version)) {
            continue
        }

        foreach ($rule in $blockedRules) {
            if ($id -ieq $rule.id -and $version -match $rule.versionRegex) {
                $failures.Add("Blocked NuGet package policy violated: $id $version. Reason: $($rule.reason)")
            }
        }
    }
}

# Validate NuGet licenses from resolved packages list
$nugetJsonRaw = dotnet list "$root/SoftwareDeliveryPlanner.slnx" package --format json --include-transitive
$nugetJson = $nugetJsonRaw | ConvertFrom-Json -Depth 100

$nugetOverrides = @{}
if ($policy.nugetLicenseOverrides) {
    $policy.nugetLicenseOverrides.PSObject.Properties | ForEach-Object {
        $nugetOverrides[$_.Name] = $_.Value
    }
}

$seenNuget = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)

foreach ($project in $nugetJson.projects) {
    foreach ($framework in $project.frameworks) {
        $allPackages = @()
        if ($framework.topLevelPackages) {
            $allPackages += @($framework.topLevelPackages)
        }
        if ($framework.transitivePackages) {
            $allPackages += @($framework.transitivePackages)
        }

        foreach ($pkg in $allPackages) {
            $pkgId = $pkg.id
            $pkgVersion = $pkg.resolvedVersion
            if ([string]::IsNullOrWhiteSpace($pkgId) -or [string]::IsNullOrWhiteSpace($pkgVersion)) {
                continue
            }

            $identity = "$pkgId@$pkgVersion"
            if (-not $seenNuget.Add($identity)) {
                continue
            }

            $licenseExpression = $null
            if ($nugetOverrides.ContainsKey($pkgId)) {
                $licenseExpression = $nugetOverrides[$pkgId]
            }
            else {
                $nuspecUrl = "https://api.nuget.org/v3-flatcontainer/$($pkgId.ToLowerInvariant())/$pkgVersion/$($pkgId.ToLowerInvariant()).nuspec"
                try {
                    $nuspec = Invoke-RestMethod -Uri $nuspecUrl -Method Get
                    if ($nuspec.package.metadata.license) {
                        $licenseExpression = [string]$nuspec.package.metadata.license.'#text'
                    }
                }
                catch {
                    $failures.Add("NuGet package $identity license metadata could not be fetched from $nuspecUrl")
                    continue
                }
            }

            $tokens = Normalize-LicenseExpressionTokens -Expression $licenseExpression
            Validate-Licenses -Tokens $tokens -Allowed $allowedSet -PackageId $pkgId -Version $pkgVersion -Source "NuGet" -Failures $failures
        }
    }
}

# Validate npm licenses from package-lock
$packageLockPath = Join-Path $root "SoftwareDeliveryPlanner.Blazor/package-lock.json"
if (Test-Path -Path $packageLockPath -PathType Leaf) {
    $lock = Get-Content -Path $packageLockPath -Raw | ConvertFrom-Json -AsHashtable
    $packages = $lock["packages"]

    foreach ($entry in $packages.GetEnumerator()) {
        $pkgData = $entry.Value
        if (-not $pkgData.ContainsKey("license")) {
            continue
        }

        $licenseExpression = [string]$pkgData["license"]
        $name = if ($entry.Key -eq "") { "root" } else { $entry.Key }
        $version = if ($pkgData.ContainsKey("version")) { [string]$pkgData["version"] } else { "unknown" }

        $tokens = Normalize-LicenseExpressionTokens -Expression $licenseExpression
        Validate-Licenses -Tokens $tokens -Allowed $allowedSet -PackageId $name -Version $version -Source "npm" -Failures $failures
    }
}
else {
    $failures.Add("NPM lock file not found at $packageLockPath")
}

if ($failures.Count -gt 0) {
    Write-Host "License verification failed:" -ForegroundColor Red
    foreach ($failure in $failures) {
        Write-Host " - $failure" -ForegroundColor Red
    }
    exit 1
}

Write-Host "License verification passed." -ForegroundColor Green
