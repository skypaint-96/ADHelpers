#$domain = [AppDomain]::CreateDomain("NewDomain");


#$domain.DoCallBack({

$path = (resolve-path '.\publish\adhelpers\').Path

New-ModuleManifest -Path ($path + 'AdHelpers.psd1') `
-RootModule "ADHelpers" `
-moduleversion ([System.Reflection.Assembly]::LoadFrom(($path + 'AdHelpers.dll')).GetCustomAttributesData()[7].constructorarguments.value) `
-Guid '55b99733-8488-493e-a3cd-05cfcea90974' `
-Author 'Mason Kerr' `
-CompanyName 'Pasifico' `
-Copyright '(c) 2024 Mason Kerr. All rights reserved.' `
-CmdletsToExport '*' `
-VariablesToExport '*' `
-LicenseUri 'https://github.com/skypaint-96/ADHelpers/blob/master/LICENSE.txt' `
-ProjectUri 'https://github.com/skypaint-96/ADHelpers' `
-DefaultCommandPrefix 'ADEX'

#});


#[AppDomain]::Unload($domain)