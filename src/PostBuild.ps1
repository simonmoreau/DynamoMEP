param ($Configuration, $TargetName, $ProjectDir, $TargetPath, $TargetDir)
write-host $Configuration
write-host $TargetName
write-host $ProjectDir
write-host $TargetPath
write-host $TargetDir



## sign the dll
$cert=Get-ChildItem -Path Cert:\CurrentUser\My -CodeSigningCert

Set-AuthenticodeSignature -FilePath $TargetPath -Certificate $cert -IncludeChain All -TimestampServer "http://timestamp.comodoca.com/authenticode"
$TargetPathCustomization = $TargetPath -replace ".dll",".customization.dll"
Set-AuthenticodeSignature -FilePath $TargetPathCustomization -Certificate $cert -IncludeChain All -TimestampServer "http://timestamp.comodoca.com/authenticode"

$RenamedTargetName ="DynamoMEP"

function CopyToFolder($packagePath) {
	# Remove previous package
	Remove-Item ($packagePath + "\" + $RenamedTargetName) -Recurse

	## Create the folder structure
	New-Item -ItemType Directory -Path ($packagePath + "\" + $RenamedTargetName + "\" + "bin")
	New-Item -ItemType Directory -Path ($packagePath + "\" + $RenamedTargetName + "\" + "dyf")
	New-Item -ItemType Directory -Path ($packagePath + "\" + $RenamedTargetName + "\" + "extra")

	## Copy the files
	xcopy /Y ($ProjectDir + "pkg.json") ($packagePath + "\" + $RenamedTargetName)
	## xcopy /Y ($ProjectDir + "Samples\*.*") ($packagePath + "\" + $RenamedTargetName + "\" + "extra")
	xcopy /Y ($TargetDir + "\*.dll*") ($packagePath + "\" + $RenamedTargetName + "\" + "bin")
	xcopy /Y ($ProjectDir + "DynamoMEP_DynamoCustomization.xml") ($packagePath + "\" + $RenamedTargetName + "\" + "bin")
}

$packagePath = "C:\Users\Simon\AppData\Roaming\Dynamo\Dynamo Revit\2.3\packages"
CopyToFolder $packagePath

$packagePath2 = "C:\Users\Simon\AppData\Roaming\Dynamo\Dynamo Revit\2.5\packages"
CopyToFolder $packagePath2

$packagePath3 = "C:\Users\Simon\AppData\Roaming\Dynamo\Dynamo Revit\2.6\packages"
CopyToFolder $packagePath3

## Zip the package
$ReleasePath="G:\My Drive\05 - Travail\Revit Dev\DynamoDev\DynamoMEP\Releases"
$ReleaseZip= ($ReleasePath + "\" + $RenamedTargetName + ".zip")

if ( Test-Path -Path $ReleasePath ) {
  7z a -tzip $ReleaseZip ($packagePath + "\" + $RenamedTargetName)
}