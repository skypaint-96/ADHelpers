$f = {
    import-module '.\adhelpers.dll' -scope local;
    $version = (get-module AdHelpers).version;
    remove-module adhelpers -force;
    New-ModuleManifest -Path '.\AdHelpers.psd1' `
        -RootModule 'ADHelpers' `
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
}

start-process powershell -argumentlist ('-Command ' + $f.tostring())
