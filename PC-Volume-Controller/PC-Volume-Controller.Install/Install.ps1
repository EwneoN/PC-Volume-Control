#
# Install.ps1
#
param(
  [string]$serviceName="PC Volume Contoller",
  [Parameter(Mandatory=$True)]
  [string]$binPath,
  [string]$user = "",
  [string]$password = ""
)

sc create $serviceName binPath= $binPath user= $user password= $password