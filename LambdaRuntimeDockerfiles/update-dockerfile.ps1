param ([Parameter(Mandatory)]$Dockerfile)

$ErrorActionPreference = "Suspend"

# Updates the Dockerfile with next ASP.NET Core version and sha256 hash if available
function Update-Dockerfile ([string]$path) {
    $nextVersion = Get-NextASPNETVersion -Dockerfile $path

    $shaFilePath = "${nextVersion}-sha.txt"

    $sha512Uri = "https://dotnetcli.blob.core.windows.net/dotnet/checksums/${nextVersion}-sha.txt"

    Invoke-WebRequest -Uri $sha512Uri -OutFile $shaFilePath
    if (!$?) {
        Write-Host "Failed to download checksums for ${nextVersion}. ${nextVersion} is not available yet."
        return
    }

    $arch = Get-Architecture -Dockerfile $path

    $artifact = "aspnetcore-runtime-${nextVersion}-linux-${arch}.tar.gz"
    $sha512 = Get-SHA512 -Artifact $artifact -Path $shaFilePath

    (Get-Content $path) -replace 'ARG ASPNET_VERSION=.*', "ARG ASPNET_VERSION=${nextVersion}" -replace 'ASPNET_SHA512=.*', $sha512 | Out-File $path

    Write-Host "Updated ${path} to ${nextVersion}."

    # This allows sharing the $path variable between steps
    # which is needed to update the description of the PR
    Write-Host "::set-output name=${path}::- Updated ${path} to ${nextVersion}.\n\t - Artifact: ${artifact}\n\t -SHA512 Source: ${sha512Uri}"
}

# Returns SHA512 of given ASP.NET Core version from the give SHA512 file
function Get-SHA512 ([string]$artifact, [string]$path) {
    $line = Select-String -Path $path -Pattern $artifact | Select-Object -Property Line -ExpandProperty Line
    Write-Host $line

    $sha512 = $line.Split(" ")[0]
    return $sha512
}

function Get-Architecture ([string]$Dockerfile) {
    if ($Dockerfile.Contains("amd64")) {
        return "x64"
    } elseif ($Dockerfile.Contains("arm64")) {
        return "arm64"
    } else {
        throw "Unsupported architecture"
    }
}

# Returns the next ASP.NET version to be updated in the Dockerfile
function Get-NextASPNETVersion ([string]$Dockerfile) {
    $line = Select-String -Path $Dockerfile -Pattern "ARG ASPNET_VERSION=" | Select-Object -Property Line -ExpandProperty Line
    $currentVersion = $line.Split("=")[1]
    Write-Host "Current ASPNET version: ${currentVersion}"

    $nextVersion = Update-PatchVersion -Version $currentVersion
    Write-Host "Next ASPNET version: ${nextVersion}"

    return $nextVersion
}

# Returns the next path version of the given version
function Update-PatchVersion ([string]$version) {
    $components = $version.Split(".");
    $major = $components[0];
    $minor = $components[1];
    $patch = $components[2];
    $patch = [int]$patch + 1;
    $newVersion = $major + "." + $minor + "." + $patch;
    return $newVersion;
}

Update-Dockerfile $Dockerfile