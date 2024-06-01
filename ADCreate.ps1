
########################################################################################################
#                                                                                                      #
# Name:                 createad.ps1                                                                   #
# Usage:                Create AD user account                                                         #
# Author:               John Clark                                                                   #
#                                                                                                      #
# Version   Date    Who     Comment                                                                    #
# 0.0.0.1   220126  CMC     Initial Version                                                            #
# 0.0.0.2   220211  CMC     Added routing to add number from UPN to surname if exists                  #
# 0.0.0.3   220214  CMC     Process box and error counts added                                         #
# 0.0.0.4   220222  CMC     Fixed expirydate issue and added PDC check                                 #
# 0.0.0.5   220224  CMC     Added info update section                                                  #
# 0.0.0.6   220225  CMC     Group add function added to handle all group add types                     #
# 0.0.0.7   220228  CMC     Added mailing of passwords to requester and admin                          #
# 0.0.0.8   220228  CMC     Added closure confirmation                                                 #
# 0.0.0.9   220318  CMC     Repex user creation incorperated                                           #
# 0.0.1.0   220321  CMC     Update server choice to array of AWS-Datacentre DC's (except 01)           #
# 0.0.1.1   220321  CMC     Added loop to to retrieve user Info to avoid failures                      #
# 0.0.1.2   220322  CMC     Spreadsheet header validation check added                                  #
# 0.0.1.3   220323  CMC     Validation of user input check added                                       #
# 0.0.1.4   230612  CMC     Update create password variables                                           #
#                                                                                                      #
########################################################################################################

# ---------------------------------------------------------------------------------------------------- #
# -------------------------------------  Global Script Stuff Below ----------------------------------- #
# ---------------------------------------------------------------------------------------------------- #

$script:version = "Version: 0.0.1.4"
$script:boxtitle = "Create User"
$script:shortname = "createad"
$script:rootpath = $PSScriptRoot
$script:logroot = "c:\logs"
$script:smtpsrv = "smtp.corpo.co.uk"
$script:xtramods = "ActiveDirectory"
$script:assemblies = "System.Web"
$script:mlsuf = "@corpo.co.uk"
$script:dcarr = "EC2-AD-AWS-02","EC2-AD-AWS-03","EC2-AD-AWS-06","EC2-AD-AWS-08"
$script:inparr = @("usertype","email","firstname","surname","requester","reqno","userid","department","location","company","companycode","mobile")
$script:defcnt = 30
$script:defslp = 2

cls

# ---------------------------------------------------------------------------------------------------- #
# -------------------------------------  Global Script Stuff Above ----------------------------------- #
# ---------------------------------------------------------------------------------------------------- #
# ---------------------------------------------------------------------------------------------------- #

function addusers {

    $script:maserr = @()
    $script:pwdarr = @()

    $successcnt = 0
    $warningcnt = 0
    $errorcnt = 0
    $usrproccnt = 1
    foreach ($user in $script:userin)
        {
                createpw
                $tres = $false
                try {
                    new-aduser `
                    -server $script:dcsrv `
                    -accountpassword (convertto-securestring $script:initpw -asplaintext -force) `
                    -changepasswordatlogon $true `
                    -company $addcomp `
                    -department $user.department `
                    -description "$(($user.usertype).toupper()) User" `
                    -displayname ($user.firstname + " " + $user.surname) `
                    -emailaddress $user.email `
                    -enabled $true `
                    -givenname $user.firstname `
                    -mobilephone $addmobno `
                    -name ($user.firstname + " " + $newsur) `
                    -office $user.location `
                    -path $usradpath `
                    -samaccountname $chkid `
                    -surname $newsur `
                    -userprincipalname $chkue `
                    -erroraction stop
                    pslog "UserID $chkid ($procname) created in AD"
                    }
                catch {
                    pslog "ERROR - Unable to create UserID $chkid ($procname)"
                    updatecount ([ref]$errorcnt) $proctc4 $proc
                    $script:errarr += "ERROR - Unable to create UserID $chkid ($procname)"
                    }
                pslog "Confirming account $chkid ($procname) in AD"
                $chkcnt = 0
                $tres = $false
                do {
                    $chkcnt++
                    try {
                        get-aduser -server $script:dcsrv $chkid -erroraction stop
                        pslog "Account $chkid ($procname) confirmed in AD"
                        updatecount ([ref]$successcnt) $proctc2 $proc
                        $addpwdarr = [ordered]@{'reqno'=($user.reqno).toupper();'requester'=$user.requester;'type'=($user.usertype).toupper();'userid'=$chkid;'username'=$procname;'initpw'=$script:initpw}
                        $script:pwdarr += new-object -typename psobject -property $addpwdarr
                        $tres = $true
                        }
                    catch
                        {
                        pslog "Account $chkid ($procname) not found in AD ($chkcnt/$script:defcnt)"
                        }
                    start-sleep -s $script:defslp
                } until ($tres -eq $true -or $chkcnt -eq $script:defcnt)
                if ($tres -eq $true)
                    {
    ########## Expiry
                    pslog "Setting expiry date for $chkid ($procname)"
                    updatetbox1 $proctb1 $proc "Setting expiry date for $chkid ($procname)`r`n"
                    if ($user.usertype -eq "Permanent")
                        {
                        pslog "Setting expiry date to NEVER for $chkid ($procname)"
                        try {
                            clear-adaccountexpiration -server $script:dcsrv -identity $chkid -erroraction stop
                            pslog "Expiry date for $chkid ($procname) set to NEVER"
                            updatecount ([ref]$successcnt) $proctc2 $proc
                            }
                        catch
                            {
                            pslog "ERROR - Unable to set expiry date to NEVER for $chkid ($procname)"
                            updatecount ([ref]$errorcnt) $proctc4 $proc
                            $script:errarr += "ERROR - Unable to set expiry date to NEVER for $chkid ($procname)"
                            }
                        }
                    else
                        {
                        $exdate = ((get-date).AddMonths(6))
                        pslog "Setting expiry date to $exdate for $chkid ($procname)"
                        try {
                            set-adaccountexpiration -server $script:dcsrv -identity $chkid -datetime $exdate -erroraction stop
                            pslog "Expiry date for $chkid ($procname) set to $exdate"
                            updatecount ([ref]$successcnt) $proctc2 $proc
                            }
                        catch
                            {
                            pslog "ERROR - Unable to set expiry date to $exdate for $chkid ($procname)"
                            updatecount ([ref]$errorcnt) $proctc4 $proc
                            $script:errarr += "ERROR - Unable to set expiry date to $exdate for $chkid"
                            }
                        }
    ########## Groups
                    pslog "Adding $chkid ($procname) required to AD groups"
                    if ($user.usertype -eq "FJSvcDesk")
                        {
                        addgrps "default" $chkid $procname
                        addgrps "fjsvcDesk" $chkid $procname
                        }
                    elseif ($user.usertype -eq "Mates")
                        {
                        addgrps "default" $chkid $procname
                        addgrps "mates" $chkid $procname
                        }
                    elseif ($user.usertype -eq "Repex")
                        {
                        addgrps "repex" $chkid $procname
                        }
                    else
                        {
                        addgrps "default" $chkid $procname
                        }
    ########## Info
                    pslog "Adding info to $chkid ($procname)"
                    pslog "Getting existing info for $chkid ($procname)"
                    $infcnt = 0
                    $tres = $false
                    do {
                        $infcnt++
                        try {
                            $updinf = get-aduser $chkid -properties * -erroraction stop | select -ExpandProperty info
                            pslog "Existing info retrieved for $chkid ($procname)"
                            updatecount ([ref]$successcnt) $proctc2 $proc
                            $tres = $true
                            }
                        catch {
                            pslog "Unable to retrieve existing info for $chkid ($procname) ($infcnt/$script:defcnt)"
                            }
                        start-sleep -s $script:defslp
                    } until ($tres -eq $true -or $infcnt -eq $script:defcnt)
                    if ($tres -eq $true)
                        {
                        pslog "Updating info for $chkid ($procname)"
                        $adddte = get-date
                        if ($updinf -ne $null)
                            {
                            $addinf = "$($updinf)`r`n`r`ncorpo Joiner-O-Matic`r`nUser $chkid Created`r`nDate: $adddte`r`nType: $(($user.usertype).toupper())`r`nReqNo: $(($user.reqno).toupper())`r`nBy: $env:username`r`nOn: $env:computername"
                            }
                        else
                            {
                            $addinf = "corpo Joiner-O-Matic`r`nUser $chkid Created`r`nDate: $adddte`r`nType: $(($user.usertype).toupper())`r`nReqNo: $(($user.reqno).toupper())`r`nBy: $env:username`r`nOn: $env:computername"
                            }
                        try {
                            set-aduser $chkid -replace @{info = $addinf} -erroraction stop
                            pslog "Info for $chkid ($procname) updated successfully"
                            updatecount ([ref]$successcnt) $proctc2 $proc
                            }
                        catch
                            {
                            pslog "ERROR - Unable to update info for $chkid ($procname)"
                            updatecount ([ref]$errorcnt) $proctc4 $proc
                            $script:errarr += "ERROR - Unable to update info for $chkid ($procname)"
                            }
                        }
                    else
                        {
                        pslog "ERROR - Unable to retrieve existing info for $chkid ($procname)"
                        updatecount ([ref]$errorcnt) $proctc4 $proc
                        $script:errarr += "ERROR - Unable to retrieve existing info for $chkid ($procname)"
                        }
    ########## Password
                    pslog "Sending initial password for $procname to $($user.requester)"
                    mailpwd $user.requester "$($user.firstname) $($user.surname)" $user.reqno $script:initpw 
                    }
                else
                    {
                    pslog "ERROR - Created account $chkid ($procname) not found in AD"
                    updatecount ([ref]$errorcnt) $proctc4 $proc
                    $script:errarr += "ERROR - Created account $chkid ($procname) not found in AD"
                    }
                }
            elseif (($script:errarr | measure).count -gt 0)
                {
                pslog "WARNING - Either the UserID, Email address or UPN already exists in AD"
                updatecount ([ref]$warningcnt) $proctc3 $proc
                $script:errarr +=  "WARNING - Either the UserID, Email address or UPN for $chkid already exists in AD"            
                }
            else
                {
                pslog "ERROR - Unexpected error occured"
                updatecount ([ref]$errorcnt) $proctc4 $proc
                $script:errarr += "ERROR - Unexpected error occured"
                }
            clear-variable -name ("chkusr","chkupn","chkeml")
            }
        $script:maserr += $script:errarr
        }
    mailadm
    $proc.close()
    complete "COMPLETE" "User ID creation process complete"
        
}

# ---------------------------------------------------------------------------------------------------- #

function addgrps {

    param (
        [string]$param1,
        [string]$param2,
        [string]$param3
        )
    pslog "Processing $($param1.toupper()) AD groups for $param2 ($param3)"
    pslog "Importing list of $($param1.toupper()) AD groups"
    $tres = $false
    $grpin = "$script:rootpath\grp-$param1.list"
    try {
        $grplist = get-content $grpin -erroraction stop
        pslog "List of $($param1.toupper()) AD groups imported successfully"
        updatecount ([ref]$successcnt) $proctc2 $proc
        $tres = $true
        }
    catch {
        pslog "ERROR - Unable to import list of $($param1.toupper()) AD groups"
        updatecount ([ref]$errorcnt) $proctc4 $proc
        $script:errarr += "ERROR - Unable to import list of $($param1.toupper()) AD groups"
        }
    if ($tres -eq $true)
        {
        foreach ($adgrp in $grplist) {
            $tres = $false
            try {
                $getgrp = get-adgroup -server $script:dcsrv $adgrp -erroraction stop
                pslog "Group $adgrp found in AD"
                updatecount ([ref]$successcnt) $proctc2 $proc
                $tres = $true
                }
            catch {
                pslog "ERROR - Unable to find group $adgrp in AD"
                updatecount ([ref]$errorcnt) $proctc4 $proc
                $script:errarr += "ERROR - Unable to find group $adgrp in AD"
                }
            if ($tres -eq $true)
                {
                try {
                    add-adgroupmember -server $script:dcsrv -identity $adgrp -members $param2 -erroraction stop
                    pslog "$param2 added to $adgrp"
                    updatecount ([ref]$successcnt) $proctc2 $proc
                    }
                catch {
                    pslog "ERROR - Unable to add $param2 to $adgrp"
                    updatecount ([ref]$errorcnt) $proctc4 $proc
                    $script:errarr += "ERROR - Unable to add $param2 to $adgrp"
                    }
                }
            else
                {
                pslog "ERROR - Unexpected error occured"
                updatecount ([ref]$errorcnt) $proctc4 $proc
                $script:errarr += "ERROR - Unexpected error occured"
                }
            }
        }

}

# ---------------------------------------------------------------------------------------------------- #

function mailpwd {

    param (
        [string]$param1,
        [string]$param2,
        [string]$param3,
        [string]$param4
        )

    pslog "Generating initial password email for $chkid ($procname)"

    $maildate = (Get-Date -UFormat "%A %d/%m/%Y")
    $to = "$param1"
    $bcc = "john.clark@corpo.co.uk"
    try {
        Send-MailMessage -From "joiner-o-matic@corpo.co.uk" -To $to -Bcc $bcc -Subject "Info - Request $($param3.toupper())" -SmtpServer $script:smtpsrv -BodyAsHtml -Body $mailbody -erroraction stop
        pslog "Initial password email for $chkid sent successfully"
        updatecount ([ref]$successcnt) $proctc2 $proc
        }
    catch
        {
        pslog "ERROR - Unable to send initial password email for $chkid"
        updatecount ([ref]$errorcnt) $proctc4 $proc
        $script:errarr += "ERROR - Unable to send initial password email for $chkid"
        }

}

# ---------------------------------------------------------------------------------------------------- #

function mailadm {

    pslog "Getting email of administrative user creating IDs ($env:username)"
    $admusr = ($env:username.split("-"))[0]
    $tres = $false
    try {
        $admmail = (get-aduser $admusr -properties * -erroraction stop | select mail).mail
        pslog "Administrative user email is $admmail for $admusr"
        updatecount ([ref]$successcnt) $proctc2 $proc
        $tres = $true
        }
    catch {
        pslog "ERROR - Unable to determine administrative user email"
        updatecount ([ref]$errorcnt) $proctc4 $proc
        $script:errarr += "ERROR - Unable to determine administrative user email"
        }
    if ($tres -eq $true)
        {
        if (($script:pwdarr | measure).count -gt 0)
            {
            pslog "Generating administrative user summary email"
            $maildate = (Get-Date -UFormat "%A %d/%m/%Y")
            $mailstyle =
            "
            <style type='text/css'>
            p.header{color:#000099;font-family:Calibri;font-size:14}
            p.info{color:#993300;font-family:Calibri;font-size:12}
            p.text{color:#4C4C4C;font-family:Calibri;font-size:14}
            p.textunred{color:#cc0000;font-family:Calibri;font-size:14;text-decoration:underline}
            p.title{color:#4C4C4C;font-family:Calibri;font-size:20;font-weight:bold}
            p.signoff{color:#4C4C4C;font-family:Calibri;font-size:17;font-weight:bold}
            span.boldtxt{color:#4C4C4C;font-family:Calibri;font-size:14;font-weight:bold}
            span.boldgrn{color:#006600;font-family:Calibri;font-size:14;font-weight:bold}
            span.count{color:#cc0066;font-family:Calibri;font-size:14;font-weight:bold}
            span.header{color:#000099;font-family:Calibri;font-size:14}
            span.info{color:#993300;font-family:Calibri;font-size:12}
            span.bold{color:#006600;font-family:Calibri;font-size:14;font-weight:bold}
            table {border-spacing:150px} 
            td,th {text-align:left;padding:5px}
            </style>
            "
            $mailhead =
            "
            <p class='title'>Joiners Details</p>
            "
            $mailclose =
            "
            <p class='text'>Thanks,</p>
            <p class='signoff'>corpo Windows Project Team</p>
            <br>
            <br>    
            "
            foreach ($usrpwd in $script:pwdarr) {
                $script:mailpwdarray +=
                "
                <tr>
                    <td><span class='info'>$(($usrpwd.reqno).toupper())</span></td>
                    <td><span class='info'>$($usrpwd.requester)</span></td>
                    <td><span class='info'>$($usrpwd.type)</span></td>
                    <td><span class='info'>$($usrpwd.userid)</span></td>
                    <td><span class='info'>$($usrpwd.username)</span></td>
                    <td><span class='info'>$($usrpwd.initpw)</span></td>
                </tr>
                "
                }
            $mailcontent =
            "
            <table>
                <tr>
                    <th><span class='header'>Request</span></th>
                    <th><span class='header'>Requester</span></th>
                    <th><span class='header'>Type</span></th>
                    <th><span class='header'>UserID</span></th>
                    <th><span class='header'>Name</span></th>
                    <th><span class='header'>Password</span></th>
                </tr>
            $script:mailpwdarray
            </table>
            "
            $mailbody =
            "
            $mailstyle
            <body>
            $mailhead
            <p>
            $mailcontent
            $mailclose
            </body>
            "
            $to = "$admmail"
            $bcc = "john.clark@corpo.co.uk"
            try {
                Send-MailMessage -From "joiner-o-matic@corpo.co.uk" -To $to -Bcc $bcc -Subject "Joiners Summary" -SmtpServer $script:smtpsrv -BodyAsHtml -Body $mailbody -erroraction stop
                pslog "Administrative user summary email sent successfully to $admmail"
                updatecount ([ref]$successcnt) $proctc2 $proc
                }
            catch
                {
                pslog "ERROR - Unable to send administrative user summary email to $admmail"
                updatecount ([ref]$errorcnt) $proctc4 $proc
                $script:errarr += "ERROR - Unable to send administrative user summary email to $admmail"
                }
            }
        else
            {
            pslog "No password info to send to dministrative user $admmail"
            }
        }

}

# ---------------------------------------------------------------------------------------------------- #

function experrors {

    if ($script:maserr -gt 0)
        {
        $script:errfile = "$script:logpath\$script:logfiledate-$script:shortname-err.log"
        pslog "Exporting errors and/or warnings to $script:errfile"
        $script:maserr | out-file $script:errfile
        start $script:errfile
        } 
    else
        {
        pslog "No errors or warnings found to export"
        }

}

# ---------------------------------------------------------------------------------------------------- #

function complete {

    param (
        $param1,
        $param2,
        $param3
        )
    
    pslog "$param2"
}

# ---------------------------------------------------------------------------------------------------- #
# -------------------------------------  Specific Functions Above ------------------------------------ #
# ---------------------------------------------------------------------------------------------------- #

# ---------------------------------------------------------------------------------------------------- #
# ------------------------------------------- Script Below ------------------------------------------- #
# ---------------------------------------------------------------------------------------------------- #

startlog

importmods

importassembly

stoplog


# ---------------------------------------------------------------------------------------------------- #
# ------------------------------------------- Script Above ------------------------------------------- #
# ---------------------------------------------------------------------------------------------------- #
