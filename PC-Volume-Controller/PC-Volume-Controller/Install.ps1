#
# Install.ps1
#
param(
  [string]$serviceName="PC Volume Contoller",
  [string]$displayName="PC Volume Contoller",
  [string]$user = "",
  [string]$password = ""
)

$binPath = Join-Path -path $PWD -childpath "\Pc-Volume-Controller.exe"
$userParams = ''

If(![string]::IsNullOrWhiteSpace($user))
{
  $userParams += ' user= "{0}"' -f $user 
}

If(![string]::IsNullOrWhiteSpace($password))
{
  $userParams += ' password= "{0}"' -f $password 
}

$cmdLine = 'sc.exe create "{0}" binpath= "{1}" displayname= "{2}"' -f $serviceName, $binPath, $displayName 

If(![string]::IsNullOrWhiteSpace($userParams))
{
  $cmdLine += $userParams
}

Invoke-Expression $cmdLine
Exit