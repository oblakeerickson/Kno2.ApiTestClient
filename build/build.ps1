$script:GitPath = 'C:\Program Files (x86)\Git\cmd\git.exe'
properties {

  #PARAMS
  $version = "1.0.0.0"
  $build_level = "Debug"
  $build_number = "build_number_not_set"
  $commit_hash = "hash_not_set"
  $nuget_config_path = ""

  #PATHS
  $build_dir = Split-Path $psake.build_script_file
  $solution_dir = Split-Path $build_dir
  $build_output = "$build_dir\artifacts"
  $srcDir = "$solution_dir\src"
  $nuget_dir = "$build_dir\..\src\packages\"
  $package_dir = "$build_dir\..\packages\"
  $artifacts_dir = "$build_dir\..\artifacts"

  #SLN INFO
  $company_name = "Kno2"
  $solution_name = "Kno2.ApiTestClient"
  $solution_file = "$srcDir\$solution_name.sln"
  $client_apps = @("Kno2.ApiTestClient")

  #ILMerge
  $ilmerge_path = "ilmerge.2.14.1208\tools\ILMerge.exe"

  #Client Settings
  $BaseUri = ""
  $ClientId = ""
  $ClientSecret = ""
  $DirectMessageDomain  = ""
}

include tools\psake_utils.ps1
include tools\config-utils.ps1
include tools\nuget-utils.ps1
include tools\git-utils.ps1
include tools\ilmerge-utils.ps1

task default -depends Compile, Ilmerge, SetClientSettings, Get-ReleaseNotes, Get-Version

task Compile -depends Clean, Package-Restore {
  $script:build_level = $build_level
  $version = getVersion
  Exec {
  	msbuild "$solution_file" `
  	        /m /nr:false /p:VisualStudioVersion=12.0 `
  	        /t:Rebuild /nologo /v:m `
  	        /p:Configuration="$script:build_level" `
  	        /p:Platform="Any CPU" /p:TrackFileAccess=false
  }
}

task Clean {
  foreach($assembly in $assemblies + $client_apps + $web_apps + $tests){
    $bin = "$srcDir\$assembly\bin\"
    $obj = "$srcDir\$assembly\obj\"
    Write-Host "Removing $bin"
    Delete-Directory($bin)
    Write-Host "Removing $obj"
    Delete-Directory($obj)
  }
}

task Ilmerge {
	Create-Directory $artifacts_dir
	$ilmergePath = (Join-Path $package_dir $ilmerge_path)
	$mergedexe = (Join-Path $artifacts_dir TestClient.exe)
	$sourceExe = (get-childitem $srcDir\$solution_name\bin\$build_level\*.exe)[0]
	$sourceDlls = get-childitem $srcDir\$solution_name\bin\$build_level\*.dll

	ilmerge $ilmergePath $mergedexe $sourceExe $sourceDlls
}

task SetClientSettings {
	$sourceExe = (get-childitem $srcDir\$solution_name\bin\$build_level\*.exe)[0]
	$config = "$sourceExe.config"
  $outputConfig = (Join-Path $artifacts_dir TestClient.exe.config)
	Copy-Item -Path $config -Destination $outputConfig

	if([string]::IsNullOrEmpty($BaseUri) -eq $false){
	  Set-ApplicationSetting -fileName $outputConfig -name "BaseUri" -value $BaseUri
	}

	if([string]::IsNullOrEmpty($ClientId) -eq $false){
	  Set-ApplicationSetting -fileName $outputConfig -name "ClientId" -value $ClientId
	}

	if([string]::IsNullOrEmpty($ClientSecret) -eq $false){
	  Set-ApplicationSetting -fileName $outputConfig -name "ClientSecret" -value $ClientSecret
	}

	if([string]::IsNullOrEmpty($DirectMessageDomain) -eq $false){
	  Set-ApplicationSetting -fileName $outputConfig -name "DirectMessageDomain" -value $DirectMessageDomain
	}

  $version = getVersion
  $gitHash = getHash
	Set-ApplicationSetting -fileName $outputConfig -name "EmrSessionValue" -value $env:computername-$version-$gitHash
}
