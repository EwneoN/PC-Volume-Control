#
# ActivateUrl.ps1
#
# Run this script if you want to activate the url manually. 
# Otherwise set AutoCreateUrlReservations to true in app.config 
# and ensure a ServiceUser is also set and the service will attempt tp handle this automatically.
# If using https run ActivateHttps.ps1 as well.
#
param(
  [parameter(Mandatory=$true)][string]$url,
  [parameter(Mandatory=$true)][string]$user
)

If([string]::IsNullOrWhiteSpace($url))
{
   echo 'url cannot be null, empty or whitespace'
}

If([string]::IsNullOrWhiteSpace($user))
{
   echo 'user cannot be null, empty or whitespace'
}

$cmdLine = 'netsh http add urlacl url="{0}" user="{1}"' -f $url, $user

Invoke-Expression $cmdLine
Exit