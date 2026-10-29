<#
.SYNOPSIS
  Trusts the release signing certificate and installs the NPI Registry Command Palette extension.

.DESCRIPTION
  Run from the folder containing the downloaded release files (the .cer and the .msix files).
  Importing the certificate into "Trusted People" needs an elevated PowerShell.
  Afterwards, run "Reload" in Command Palette (the "Reload Command Palette extensions" entry).
#>
#Requires -RunAsAdministrator
[CmdletBinding()]
param(
    [string]$Directory = $PSScriptRoot
)

$ErrorActionPreference = 'Stop'

$arch = if ($env:PROCESSOR_ARCHITECTURE -eq 'ARM64') { 'ARM64' } else { 'x64' }

$cer = Get-ChildItem -Path $Directory -Filter '*.cer' | Select-Object -First 1
$msix = Get-ChildItem -Path $Directory -Filter "*_$arch.msix" | Select-Object -First 1
if (-not $cer)  { throw "No .cer file found in $Directory" }
if (-not $msix) { throw "No *_$arch.msix file found in $Directory" }

Write-Host "Trusting certificate $($cer.Name) (Local Machine > Trusted People)"
Import-Certificate -FilePath $cer.FullName -CertStoreLocation 'Cert:\LocalMachine\TrustedPeople' | Out-Null

Write-Host "Installing $($msix.Name)"
Add-AppxPackage -Path $msix.FullName

Write-Host 'Installed. In Command Palette, run "Reload" and open "NPI Registry".'
