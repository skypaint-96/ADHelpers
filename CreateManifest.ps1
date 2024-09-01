#$domain = [AppDomain]::CreateDomain("NewDomain");


#$domain.DoCallBack({
import-module ".\publish\adhelpers\adhelpers.dll" -scope local
$version = (get-module AdHelpers).version
remove-module adhelpers -force

New-ModuleManifest -Path '.\publish\adhelpers\AdHelpers.psd1' `
-RootModule "ADHelpers" `
-moduleversion $version `
-Guid '55b99733-8488-493e-a3cd-05cfcea90974' `
-Author 'Mason Kerr' `
-CompanyName 'Pasifico' `
-Copyright '(c) 2024 Mason Kerr. All rights reserved.' `
-CmdletsToExport '*' `
-VariablesToExport '*' `
-LicenseUri 'https://github.com/skypaint-96/ADHelpers/blob/master/LICENSE.txt' `
-ProjectUri 'https://github.com/skypaint-96/ADHelpers' `
-DefaultCommandPrefix 'ADEX'

exit
#});


#[AppDomain]::Unload($domain)