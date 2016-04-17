#
# ActivateHttps.ps1
#
# Run this script if using https.
# Parameter certhash should not contain spaces.
# If it does this script will not activate https correctly.
# Parameter appid can be any random Guid.
#
param(
  [parameter(Mandatory=$true)][string]$ipport,
  [parameter(Mandatory=$true)][string]$certhash,
  [parameter(Mandatory=$true)][string]$appid
)

If([string]::IsNullOrWhiteSpace($ipport))
{
   echo 'ipport cannot be null, empty or whitespace'
}

If([string]::IsNullOrWhiteSpace($certhash))
{
   echo 'certhash cannot be null, empty or whitespace'
}

If([string]::IsNullOrWhiteSpace($appid))
{
   echo 'appid cannot be null, empty or whitespace'
}

$cmdLine = 'netsh http add sslcert ipport={0} certhash={1} appid={2} clientcertnegotiation=enable"' -f $ipport, $certhash, $appid

Invoke-Expression $cmdLine
Exit