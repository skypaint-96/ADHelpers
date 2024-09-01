# ADHelpers

A little side project to create tools that I find usefull day to day while working on the service desk.

## Currently limited features

- `Compare-ObjectMemberships [-Identites] <string[]> [-IdentityType {SamAccountName | Name | UserPrincipalName | DistinguishedName | Sid | Guid}] [<CommonParameters>]`
  Outputs a dictionary of the groups unique to the accounts specified in the identities parameter as well as a
    "CommonGroups" entry for groups that all objects are in.
- `Get-LockoutStatus [-Identity] <string> [-IdentityType {SamAccountName | Name | UserPrincipalName | DistinguishedName | Sid | Guid}] [<CommonParameters>]`
  Returns an user's last login/failed login/expiry times etc.
- `Get-UserNames [-Identity] <string> [-IdentityType {SamAccountName | Name | UserPrincipalName | DistinguishedName | Sid | Guid}] [<CommonParameters>]`
  Used to get the names of an AD user, useful as it shows if there are any missmatches/misspellings.

## Instalation

As certificates to be able to publish to the powershell gallery are expensive, you will need to install this manualy.

1. You can [download](https://github.com/skypaint-96/ADHelpers/releases/) and extract the release package.
2. If you initialy want to test this module, just open the inner folder in powershell and run `Import-Module .\adhelpers.psd1 -scope local`.
3. If it seems to work fine for you then you can copy the `ADHelpers` folder to any directory found in `$env:PSModulePath`
4. Once in the directory of your choosing you can run `import-module adhelpers`

## Not working?

Feel free to leave an [issue](https://github.com/skypaint-96/ADHelpers/issues) ticket.
