
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
# --------------------------------------  Common Functions Below ------------------------------------- #
# ---------------------------------------------------------------------------------------------------- #

function startlog {

    New-Item $script:logroot\$script:shortname -type directory -force | Out-Null
    $script:logfiledate =  (Get-Date -UFormat %y%m%d%H%M%S)
    $startdate =  (Get-Date -Format dd/MM/yy-HH:mm:ss)
    $script:logpath = "$script:logroot\$script:shortname"
    $script:logfile = "$script:logpath\$script:logfiledate-$script:shortname.log"
    Write "Logging to $script:logfile started at $startdate" | Out-File -Append $script:logfile
    Write "Logging started by $env:username on $env:computername" | Out-File -Append $script:logfile
    Write "Running $script:boxtitle ($script:shortname) - $script:version" | Out-File -Append $script:logfile

}

# ---------------------------------------------------------------------------------------------------- #

function stoplog {

    $enddate = (Get-Date -Format dd/MM/yy-HH:mm:ss)
    Write "Logging stopped by $env:username on $env:computername" | Out-File -Append $script:logfile
    Write "Logging to $script:logfile stopped at $enddate" | Out-File -Append $script:logfile
    Write "" | Out-File -Append $script:logfile
    exit

}

# ---------------------------------------------------------------------------------------------------- #

function pslog {

    param (
        [string] $logentry
        )
    $logdate = (Get-Date -Format dd/MM/yy-HH:mm:ss)
    Write "$logdate - $logentry" | Out-File -Append $script:logfile

}

# ---------------------------------------------------------------------------------------------------- #

function close {
    
    param (
        [string] $comment
        )
    experrors
    message "$comment" $script:defslp
    stoplog

}

# ---------------------------------------------------------------------------------------------------- #

function boxprereqs {

    pslog "Loading box prerequisites"
    $tres = $false
    try {
        [void] [System.Reflection.Assembly]::LoadWithPartialName("System.Windows.Forms")
        [void] [System.Reflection.Assembly]::LoadWithPartialName("System.Drawing")
        $tres = $true
        }
    catch
        {
        pslog "Failed to load form libraries"
        stoplog
        }
    if ($tres -eq $true)
        {
        pslog "Form libraries loaded"
        $script:LogoFont = New-Object System.Drawing.Font("Arial",20,[System.Drawing.FontStyle]::Regular)
        $script:TitleFont = New-Object System.Drawing.Font("Arial",15,[System.Drawing.FontStyle]::Regular)
        $script:NameFont = New-Object System.Drawing.Font("Arial",11,[System.Drawing.FontStyle]::Regular)
        $script:ProgFont = New-Object System.Drawing.Font("Arial",10,[System.Drawing.FontStyle]::Regular)
        $script:MainFont = New-Object System.Drawing.Font("Arial",8,[System.Drawing.FontStyle]::Regular)
        $script:ProcFont = New-Object System.Drawing.Font("Arial",7,[System.Drawing.FontStyle]::Regular)
        $script:SubFont = New-Object System.Drawing.Font("Arial",6,[System.Drawing.FontStyle]::Regular)
        $script:ButColor = "#FFFAFAFD"
        }

}

# ---------------------------------------------------------------------------------------------------- #

function importmods {

    pslog "Importing required PowerShell modules"
    $impmoderr = 0
    foreach ($mod in $script:xtramods)
        {
        pslog "Importing module $mod"
        try {
            import-module $mod -erroraction stop
            pslog "$mod imported successfully"
            }
        catch {
            pslog "Unable to import $mod"
            $impmoderr++
            }
        }
    if ($impmoderr -gt 0)
        {
        complete "ERROR" "Unable to import all required PowerShell modules"
        }
        else
        {
        pslog "All required PowerShell modules imported successfully"
        }

}

# ---------------------------------------------------------------------------------------------------- #

function importassembly {

    pslog "Importing required .NET assemblies"
    $impasserr = 0
    foreach ($assem in $script:assemblies)
        {
        pslog "Importing assembly $assem"
        try {
            add-type -assemblyname $assem -erroraction stop
            pslog "$assem imported successfully"
            }
        catch {
            pslog "Unable to import $assem"
            $impasserr++
            }
        }
    if ($impasserr -gt 0)
        {
        complete "ERROR" "Unable to import all required .NET assemblies"
        }
        else
        {
        pslog "All required .NET assemblies imported successfully"
        }

}

# ---------------------------------------------------------------------------------------------------- #

function message {

    param (
        [string]$param1,
        [string]$param2
        )

    pslog "$param1"

    $menutitle = $boxtitle
    $menuheight = 90
    $menuwidth = 350
    $menucolour = "White"

    $msgbox = New-Object System.Windows.Forms.Form
    $msgbox.Text = $menutitle
    $msgbox.Height = $menuheight
    $msgbox.Width = $menuwidth
    $msgbox.BackColor = $menucolour
    $msgbox.FormBorderStyle = [System.Windows.Forms.FormBorderStyle]::FixedSingle
    $msgbox.StartPosition = [System.Windows.Forms.FormStartPosition]::CenterScreen
    
    $msglabela = New-Object system.Windows.Forms.Label
    $msglabela.TextAlign = "MiddleCenter"
    $msglabela.Text = $param1
    $msglabela.Left = 0
    $msglabela.Top = 5
    $msglabela.Width = 340
    $msglabela.Height = 40
    $msglabela.Font = $script:MainFont
    $msgbox.controls.add($msglabela)

    $msgbox.Topmost = $True    
    $msgbox.Add_Shown({$msgbox.Activate()})
    [void] $msgbox.Show()
    [void] $msgbox.Focus()

    start-sleep -s $param2

    $msgbox.close()

}

# ---------------------------------------------------------------------------------------------------- #

function updatetbox1 ($param1,$param2,[string] $param3){

    $param1.Text += $param3
    $param1.Select($param1.Text.Length - 1, 0)
    $param1.ScrollToCaret()
    $param2.Refresh()
    start-sleep -s 1

}

# ---------------------------------------------------------------------------------------------------- #

function updatecount ([ref]$param1,$param2,$param3){

    $param1.value++
    $counttxt = $param1.value
    $param2.Text = $counttxt
    $param3.Refresh()

}

# ---------------------------------------------------------------------------------------------------- #

function createpw {

    $script:initpw = ([system.web.security.membership]::generatepassword(8,2))

}

# ---------------------------------------------------------------------------------------------------- #

function getadsrv {

    
    pslog "Determining AD server to use in domain $env:userdnsdomain"
    updatetbox1 $proctb1 $proc "Determining AD server to use in domain $env:userdnsdomain`r`n"
    $script:dcsrv = get-random -inputobject $script:dcarr
    pslog "Checking AD server $script:dcsrv is online"
    updatetbox1 $proctb1 $proc "Checking AD server $script:dcsrv is online`r`n"
    try {
        test-netconnection $script:dcsrv -port 389 -erroraction stop
        pslog "Using AD server $script:dcsrv"
        updatecount ([ref]$successcnt) $proctc2 $proc
        updatetbox1 $proctb1 $proc "Using AD server $script:dcsrv`r`n"
        }
    catch
        {
        updatecount ([ref]$errorcnt) $proctc4 $proc
        updatetbox1 $proctb1 $proc "Unable to connect to AD server $script:dcsrv`r`n"
        $proc.close()
        ccomplete "ERROR" "Unable to connect to AD server $script:dcsrv"
        }

}

# ---------------------------------------------------------------------------------------------------- #
# --------------------------------------  Common Functions Above ------------------------------------- #
# ---------------------------------------------------------------------------------------------------- #

# ---------------------------------------------------------------------------------------------------- #
# -------------------------------------  Specific Functions Below ------------------------------------ #
# ---------------------------------------------------------------------------------------------------- #

function introbox {

    pslog "Launch $script:boxtitle"

    $menutitle = $boxtitle
    $menuheight = 175
    $menuwidth = 300
    $menucolour = "White"

    $intbox = New-Object System.Windows.Forms.Form
    $intbox.Text = $menutitle
    $intbox.Height = $menuheight
    $intbox.Width = $menuwidth
    $intbox.BackColor = $menucolour
    $intbox.FormBorderStyle = [System.Windows.Forms.FormBorderStyle]::FixedSingle
    $intbox.StartPosition = [System.Windows.Forms.FormStartPosition]::CenterScreen
    
    $intlabela = New-Object system.Windows.Forms.Label
    $intlabela.TextAlign = "MiddleCenter"
    $intlabela.Text = $script:boxtitle
    $intlabela.Left = 0
    $intlabela.Top = 15
    $intlabela.Width = 280
    $intlabela.Height = 40
    $intlabela.Font = $script:LogoFont
    $intbox.controls.add($intlabela)

    $intbuta = New-Object System.Windows.Forms.Button
    $intbuta.Left = 40
    $intbuta.Top = 70
    $intbuta.Width = 90
    $intbuta.Height = 25
    $intbuta.BackColor = $script:ButColor
    $intbuta.Text = "Start"
    $intbuta.Add_Click({$script:resval="cont";$intbox.Close()})
    $intbox.Controls.Add($intbuta)

    $intbutb = New-Object System.Windows.Forms.Button
    $intbutb.Left = 155
    $intbutb.Top = 70
    $intbutb.Width = 90
    $intbutb.Height = 25
    $intbutb.BackColor = $script:ButColor
    $intbutb.Text = "Exit"
    $intbutb.Add_Click({$script:resval="exit";$intbox.Close();pslog "Exit selected by $env:username"})
    $intbox.Controls.Add($intbutb)

    $intlabelb = New-Object system.Windows.Forms.Label
    $intlabelb.TextAlign = "MiddleCenter"
    $intlabelb.Text = $script:version
    $intlabelb.Left = 0
    $intlabelb.Top = 95
    $intlabelb.Width = 280
    $intlabelb.Height = 40
    $intlabelb.Font = $script:SubFont
    $intbox.controls.add($intlabelb)

    $intbox.Topmost = $True    
    $intbox.Add_Shown({$intbox.Activate()})
    [void] $intbox.ShowDialog()
    [void] $intbox.Focus()

    $intbox.close()

    if ($script:resval -eq "cont")
        {
        pslog "Start selected by $env:username"
        }
    elseif ($script:resval -eq "exit")
        {
        close "Closing $boxtitle"
        }
    else
        {
        close "Closing $boxtitle"
        }

}

# ---------------------------------------------------------------------------------------------------- #

function infobox {

    pslog "Prompting for new user input file"

    do {

    $script:resval = $null
    $chkpath = $null

    $menutitle = $boxtitle
    $menuheight = 245
    $menuwidth = 300
    $menucolour = "White"

    $infbox = New-Object System.Windows.Forms.Form
    $infbox.Text = $menutitle
    $infbox.Height = $menuheight
    $infbox.Width = $menuwidth
    $infbox.BackColor = $menucolour
    $infbox.FormBorderStyle = [System.Windows.Forms.FormBorderStyle]::FixedSingle
    $infbox.StartPosition = [System.Windows.Forms.FormStartPosition]::CenterScreen
    
    $inflabela = New-Object system.Windows.Forms.Label
    $inflabela.TextAlign = "MiddleCenter"
    $inflabela.Text = "Specify Input File"
    $inflabela.Left = 0
    $inflabela.Top = 15
    $inflabela.Width = 280
    $inflabela.Height = 40

    $inflabela.Font = $script:TitleFont
    $infbox.controls.add($inflabela)

    $inflabelc = New-Object system.Windows.Forms.Label
    $inflabelc.TextAlign = "MiddleLeft"
    $inflabelc.Text = "Specify the FULL NAME AND PATH of the new user input file to process"
    $inflabelc.Left = 35
    $inflabelc.Top = 65
    $inflabelc.Width = 210
    $inflabelc.Height = 30
    $inflabelc.Font = $script:MainFont
    $infbox.controls.add($inflabelc)

    $inftxtc = New-Object System.Windows.Forms.TextBox
    $inftxtc.Left = 35
    $inftxtc.Top = 115
    $inftxtc.Width = 210
    $inftxtc.Height = 35
    $inftxtc.Font = $script:MainFont
    $infbox.controls.add($inftxtc)

    $infbuta = New-Object System.Windows.Forms.Button
    $infbuta.Left = 35
    $infbuta.Top = 155
    $infbuta.Width = 90
    $infbuta.Height = 25
    $infbuta.BackColor = $script:ButColor
    $infbuta.Text = "Process"
    $infbuta.Add_Click({$script:resval="cont";$script:usrinput=$inftxtc.text;$infbox.Close();pslog "Process selected by $env:username"})
    $infbox.Controls.Add($infbuta)

    $infbutb = New-Object System.Windows.Forms.Button
    $infbutb.Left = 155
    $infbutb.Top = 155
    $infbutb.Width = 90
    $infbutb.Height = 25
    $infbutb.BackColor = $script:ButColor
    $infbutb.Text = "Exit"
    $infbutb.Add_Click({$script:resval="exit";$infbox.Close();pslog "Exit selected by $env:username"})
    $infbox.Controls.Add($infbutb)

    $infbox.Topmost = $True    
    $infbox.Add_Shown({$infbox.Activate()})
    [void] $infbox.ShowDialog()
    [void] $infbox.Focus()

    $infbox.close()

    if ($inftxtc.text -ne "")
        {
        if (test-path -path $inftxtc.text)
            {
            $chkpath = "found"
            pslog "Input file $($inftxtc.text) found"
            }
        }
    if ($script:resval -eq "exit")
        {
        close "Exiting without input"
        }
    elseif ($inftxtc.text -eq "" -and $script:resval -eq "cont")
        {
        message "Input file field can not be blank" $script:defslp
        }
    elseif ($chkpath -ne "found" -and $script:resval -eq "cont")
        {
        message "Specified input file not found" $script:defslp
        }
    elseif ($inftxtc.text -ne "" -and $chkpath -eq "found" -and $script:resval -eq "cont")
        {
        message "Processing provided information" $script:defslp
        }
    else
        {
        $script:resval="exit"
        close "Exiting without input"
        }
    } until (($inftxtc.text -ne "" -and $chkpath -eq "found") -or $script:resval -eq "exit")


    if ($script:resval -eq "cont")
        {
        pslog "Processing file $script:usrinput"
        $script:userin = import-csv $script:usrinput
        pslog "Checking input file format for $(($script:usrinput).split("\")[-1])"
        $chkbal = @(compare-object $script:userin[0].psobject.properties.name $script:inparr -SyncWindow 0).length -eq 0
        if ($chkbal -eq $true)
            {
            pslog "Input file $(($script:usrinput).split("\")[-1]) is in the correct format"
            }
        elseif ($chkbal -eq $false)
            {
            complete "ERROR" "Input file $(($script:usrinput).split("\")[-1]) is not in the correct format"
            }
        else
            {
            complete "ERROR" "Unable to determine format of $(($script:usrinput).split("\")[-1])"
            }
        }
    elseif ($script:resval -eq "exit")
        {
        close "Closing $boxtitle"
        }
        else
        {
        close "Closing $boxtitle"
        }

}

# ---------------------------------------------------------------------------------------------------- #

function addusers {

    $script:maserr = @()
    $script:pwdarr = @()

    $successcnt = 0
    $warningcnt = 0
    $errorcnt = 0

    $menutitle = $boxtitle
    $menuheight = 320
    $menuwidth = 410
    $menucolor = "White"

    $proc = New-Object System.Windows.Forms.Form
    $proc.Text = $menutitle
    $proc.Height = $menuheight
    $proc.Width = $menuwidth
    $proc.BackColor = $menucolor
    $proc.FormBorderStyle = [System.Windows.Forms.FormBorderStyle]::FixedSingle
    $proc.StartPosition = [System.Windows.Forms.FormStartPosition]::CenterScreen
    
    $proclabela = New-Object system.Windows.Forms.Label
    $proclabela.TextAlign = "MiddleCenter"
    $proclabela.Text = $script:boxtitle
    $proclabela.Left = 25
    $proclabela.Top = 10
    $proclabela.Width = 350
    $proclabela.Height = 40
    $proclabela.Font = $script:TitleFont
    $proc.controls.add($proclabela)

    $proct1 = New-Object system.Windows.Forms.Label
    $proct1.TextAlign = "MiddleCenter"
    $proct1.Text = "*** Process Log ***"
    $proct1.Left = 0
    $proct1.Top = 50
    $proct1.Width = 400
    $proct1.Height = 20
    $proct1.Font = $script:MainFont
    $proc.controls.add($proct1)

    $proctb1 = New-Object system.Windows.Forms.RichTextBox
    $proctb1.Text = ""
    $proctb1.Left = 20
    $proctb1.Top = 85
    $proctb1.Width = 360
    $proctb1.Height = 125
    $proctb1.Font = $script:ProcFont
    $proc.controls.add($proctb1)

    $proct2 = New-Object system.Windows.Forms.Label
    $proct2.TextAlign = "MiddleCenter"
    $proct2.Text = "Success Events"
    $proct2.Left = 50
    $proct2.Top = 220
    $proct2.Width = 60
    $proct2.Height = 30
    $proct2.Font = $script:MainFont
    $proc.controls.add($proct2)

    $proctc2 = New-Object system.Windows.Forms.Label
    $proctc2.TextAlign = "MiddleCenter"
    $proctc2.ForeColor = "Green"
    $proctc2.Text = $successcnt
    $proctc2.Left = 50
    $proctc2.Top = 245
    $proctc2.Width = 60
    $proctc2.Height = 30
    $proctc2.Font = $script:NameFont
    $proc.controls.add($proctc2)

    $proct3 = New-Object system.Windows.Forms.Label
    $proct3.TextAlign = "MiddleCenter"
    $proct3.Text = "Warning Events"
    $proct3.Left = 170
    $proct3.Top = 220
    $proct3.Width = 60
    $proct3.Height = 30
    $proct3.Font = $script:MainFont
    $proc.controls.add($proct3)

    $proctc3 = New-Object system.Windows.Forms.Label
    $proctc3.TextAlign = "MiddleCenter"
    $proctc3.ForeColor = "Orange"
    $proctc3.Text = $warningcnt
    $proctc3.Left = 170
    $proctc3.Top = 245
    $proctc3.Width = 60
    $proctc3.Height = 30
    $proctc3.Font = $script:NameFont
    $proc.controls.add($proctc3)

    $proct4 = New-Object system.Windows.Forms.Label
    $proct4.TextAlign = "MiddleCenter"
    $proct4.Text = "Error Events"
    $proct4.Left = 290
    $proct4.Top = 220
    $proct4.Width = 60
    $proct4.Height = 30
    $proct4.Font = $script:MainFont
    $proc.controls.add($proct4)

    $proctc4 = New-Object system.Windows.Forms.Label
    $proctc4.TextAlign = "MiddleCenter"
    $proctc4.ForeColor = "Red"
    $proctc4.Text = $errorcnt
    $proctc4.Left = 290
    $proctc4.Top = 245
    $proctc4.Width = 60
    $proctc4.Height = 30
    $proctc4.Font = $script:NameFont
    $proc.controls.add($proctc4)

    $proc.Topmost = $True    
    $proc.Add_Shown({$proc.Activate()})
    [void] $proc.Show()
    [void] $proc.Focus()

    getadsrv
    $usrproccnt = 1
    foreach ($user in $script:userin)
        {
        $script:errarr = @()
        $usrproccnt++
        pslog "----- Processing new user from info on row $usrproccnt -----"
        updatetbox1 $proctb1 $proc "----- Processing new user from info on row $usrproccnt -----`r`n"
        $chkusrfor = $false
        if ($user.usertype -eq "Repex")
            {
            if ($user.usertype -eq "" -or $user.email -eq "" -or $user.firstname -eq "" -or $user.surname -eq "" -or $user.requester -eq "" -or $user.reqno -eq "" -or $user.company -eq "" -or $user.companycode -eq "" -or  $user.companycode -eq "")
                {
                pslog "ERROR - Incomplete info provided to create $(($user.usertype).toupper()) user on row $usrproccnt"
                updatecount ([ref]$errorcnt) $proctc4 $proc
                updatetbox1 $proctb1 $proc "ERROR - Incomplete info provided to create $(($user.usertype).toupper()) user on row $usrproccnt`r`n"
                $script:errarr += "ERROR - Incomplete info provided to create $(($user.usertype).toupper()) user on row $usrproccnt"
                }
            else
                {
                $chkusrfor = $true
                pslog "Complete info provided to create $(($user.usertype).toupper()) user on row $usrproccnt"
                }
            }
        elseif ($user.usertype -eq "3rdParty" -or $user.usertype -eq "Contractor" -or $user.usertype -eq "Engineer" -or $user.usertype -eq "FJSvcDesk" -or $user.usertype -eq "Mates" -or $user.usertype -eq "Office" -or $user.usertype -eq "Permanent")
            {
            if ($user.usertype -eq "" -or $user.email -eq "" -or $user.firstname -eq "" -or $user.surname -eq "" -or $user.requester -eq "" -or $user.reqno -eq "" -or $user.userid -eq "" -or $user.department -eq "" -or  $user.location -eq "")
                {
                pslog "ERROR - Incomplete info provided to create $(($user.usertype).toupper()) user on row $usrproccnt"
                updatecount ([ref]$errorcnt) $proctc4 $proc
                updatetbox1 $proctb1 $proc "ERROR - Incomplete info provided to create $(($user.usertype).toupper()) user on row $usrproccnt`r`n"
                $script:errarr += "ERROR - Incomplete info provided to create $(($user.usertype).toupper()) user on row $usrproccnt"
                }
            else
                {
                $chkusrfor = $true
                pslog "Complete info provided to create $(($user.usertype).toupper()) user on row $usrproccnt"
                }
            }
        else
            {
            pslog "ERROR - Unable to determine user type for user on row $usrproccnt"
            updatecount ([ref]$errorcnt) $proctc4 $proc
            updatetbox1 $proctb1 $proc "ERROR - Unable to determine user type for user on row $usrproccnt`r`n"
            $script:errarr += "ERROR - Unable to determine user type for user on row $usrproccnt"
            }
        if ($chkusrfor -eq $true)
            {
            $procname = ($user.firstname + " " + $user.surname)
            if ($user.usertype -eq "Repex")
                {
                pslog "Processing new $(($user.usertype).toupper()) user $procname"
                updatetbox1 $proctb1 $proc "Processing new $(($user.usertype).toupper()) user $procname`r`n"
                $idsafe = $false
                pslog "Generating $(($user.usertype).toupper()) UserID for $procname"
                updatetbox1 $proctb1 $proc "Generating $(($user.usertype).toupper()) UserID for $procname`r`n"
                do {
                    $usrnm = (get-random -minimum 1 -maximum 999).tostring('000')
                    $script:finid = $user.firstname[0]+$user.surname[0]+$user.companycode+$usrnm
                    $chkfinid = ($script:finid).tolower()
                    if (get-aduser -filter {samaccountname -eq $chkfinid})
                        {
                        pslog "Generated UserID $chkfinid already in use"
                        updatetbox1 $proctb1 $proc "Generated UserID $chkfinid already in use`r`n"
                        }
                        else
                        {
                        pslog "Generated UserID $chkfinid is unique"
                        updatetbox1 $proctb1 $proc "Generated UserID $chkfinid is unique`r`n"
                        $idsafe = $true
                        }
                    } until ($idsafe -eq $true)
                pslog "Unique UserID to create is $chkfinid"
                updatetbox1 $proctb1 $proc "Unique UserID to create is $chkfinid`r`n"
                $mlsafe = $false
                $mlnum = 0
                pslog "Generating $(($user.usertype).toupper()) UPN for $procname"
                updatetbox1 $proctb1 $proc "Generating $(($user.usertype).toupper()) UPN for $procname`r`n"
                do {
                    if ($mlnum -eq 0)
                        {
                        $script:finml = $user.firstname[0]+"_"+$user.surname+"_"+$user.companycode+$mlsuf
                        }
                        else
                        {
                        $script:finml = $user.firstname[0]+"_"+$user.surname+$mlnum+"_"+$user.companycode+$mlsuf
                        }
                    $chkfinml = ($script:finml).tolower()
                    if (get-aduser -filter {(userprincipalname -eq $chkfinml) -or (mail -eq $chkfinml)})
                        {
                        pslog "Generated UPN $chkfinml already in use"
                        updatetbox1 $proctb1 $proc "Generated UPN $chkfinml already in use`r`n"
                        }
                    else
                        {
                        pslog "Generated UPN $chkfinml is unique"
                        updatetbox1 $proctb1 $proc "Generated UPN $chkfinml is unique`r`n"
                        $mlsafe = $true
                        }
                    $mlnum++
                    } until ($mlsafe -eq $true)
                pslog "Unique UPN to create is $chkfinml"
                updatetbox1 $proctb1 $proc "Unique UPN to create is $chkfinml`r`n"
                $addmobno = "+44$($user.mobile)"
                $addcomp = "$($user.company) ($($user.companycode))"
                $usradpath = "OU=REPEX Users,OU=Third Party Contractors,OU=ADC Users,OU=ADC,DC=corpia,DC=coropGROUP,DC=NET"
                $chkid = $chkfinid.tolower()
                $chkue = $chkfinml.tolower()
                }
            else
                {
                pslog "Processing new $(($user.usertype).toupper()) user $procname"
                updatetbox1 $proctb1 $proc "Processing new $(($user.usertype).toupper()) user $procname`r`n"
                $addmobno = ""
                $addcomp = ""
                $usradpath = "OU=corpo Users,OU=ADC Users,OU=ADC,DC=corpia,DC=corpoGROUP,DC=NET"
                $chkid = ($user.userid).tolower()
                $chkue = ($user.email).tolower()
                }
            pslog "Processing ID $chkid for $procname"
            updatetbox1 $proctb1 $proc "Processing ID $chkid for $procname`r`n"
            $tres = $false
            try {
                $chkusr = get-aduser -server $script:dcsrv -filter {samaccountname -eq $chkid} -erroraction stop
                $tres = $true
                }
            catch {
                pslog "ERROR - Unable to retrieve UserID info for $chkid from AD"
                updatecount ([ref]$errorcnt) $proctc4 $proc
                updatetbox1 $proctb1 $proc "ERROR - Unable to retrieve UserID info for $chkid from AD`r`n"
                $script:errarr += "ERROR - Unable to retrieve UserID info for $chkid from AD"
                }
            if ($tres -eq $true)
                {
                if ($chkusr -ne $null)
                    {
                    pslog "WARNING - UserID $chkid already exists in AD"
                    updatecount ([ref]$warningcnt) $proctc3 $proc
                    updatetbox1 $proctb1 $proc "WARNING - UserID $chkid already exists in AD`r`n"
                    $script:errarr += "WARNING - UserID $chkid already exists in AD"
                    }
                elseif ($chkusr -eq $null)
                    {
                    pslog "UserID $chkid not found in AD"
                    updatecount ([ref]$successcnt) $proctc2 $proc
                    updatetbox1 $proctb1 $proc "UserID $chkid not found in AD`r`n"
                    }
                else {
                    pslog "ERROR - Unexpected error occured"
                    updatecount ([ref]$errorcnt) $proctc4 $proc
                    updatetbox1 $proctb1 $proc "ERROR - Unexpected error occured`r`n"
                    $script:errarr += "ERROR - Unexpected error occured"
                    }
                }
            elseif ($tres -eq $false)
                {
                pslog "ERROR - UserID info for $chkid not retrieved from AD due to error"
                updatecount ([ref]$errorcnt) $proctc4 $proc
                updatetbox1 $proctb1 $proc "ERROR - UserID info for $chkid not retrieved from AD due to error`r`n"
                $script:errarr += "ERROR - UserID info for $chkid not retrieved from AD due to error"
                }
            else
                {
                pslog "ERROR - Unexpected error occured"
                updatecount ([ref]$errorcnt) $proctc4 $proc
                updatetbox1 $proctb1 $proc "ERROR - Unexpected error occured`r`n"
                $script:errarr += "ERROR - Unexpected error occured"
                }
            $tres = $false
            try {
                $chkupn = get-aduser -filter {userprincipalname -eq $chkue} -erroraction stop
                $tres = $true
                }
            catch {
                pslog "ERROR - Unable to retrieve UPN info for $chkue from AD"
                updatecount ([ref]$errorcnt) $proctc4 $proc
                updatetbox1 $proctb1 $proc "ERROR - Unable to retrieve UPN info for $chkue from AD`r`n"
                $script:errarr += "ERROR - Unable to retrieve UPN info for $chkue from AD"
                }
            if ($tres -eq $true)
                {
                if ($chkupn -ne $null)
                    {
                    pslog "WARNING - UPN $chkue already exists in AD"
                    updatecount ([ref]$warningcnt) $proctc3 $proc
                    updatetbox1 $proctb1 $proc "WARNING - UPN $chkue already exists in AD`r`n"
                    $script:errarr += "WARNING - UPN $chkue already exists in AD"
                    }
                elseif ($chkupn -eq $null)
                    {
                    pslog "UPN $chkue not found in AD"
                    updatecount ([ref]$successcnt) $proctc2 $proc
                    updatetbox1 $proctb1 $proc "UPN $chkue not found in AD`r`n"
                    }
                else {
                    pslog "ERROR - Unexpected error occured"
                    updatecount ([ref]$errorcnt) $proctc4 $proc
                    updatetbox1 $proctb1 $proc "ERROR - Unexpected error occured`r`n"
                    $script:errarr += "ERROR - Unexpected error occured"
                    }
                }
            elseif ($tres -eq $false)
                {
                pslog "ERROR - UPN info for $chkue not retrieved from AD due to error"
                updatecount ([ref]$errorcnt) $proctc4 $proc
                updatetbox1 $proctb1 $proc "ERROR - UPN info for $chkue not retrieved from AD due to error`r`n"
                $script:errarr += "ERROR - UPN info for $chkue not retrieved from AD due to error"
                }
            else
                {
                pslog "ERROR - Unexpected error occured"
                updatecount ([ref]$errorcnt) $proctc4 $proc
                updatetbox1 $proctb1 $proc "ERROR - Unexpected error occured`r`n"
                $script:errarr += "ERROR - Unexpected error occured"
                }
            $tres = $false
            try {
                $chkeml = get-aduser -server $script:dcsrv -filter {mail -eq $chkue} -erroraction stop
                $tres = $true
                }
            catch {
                pslog "ERROR - Unable to retrieve Email address info for $chkue from AD"
                updatecount ([ref]$errorcnt) $proctc4 $proc
                updatetbox1 $proctb1 $proc "ERROR - Unable to retrieve Email address info for $chkue from AD`r`n"
                $script:errarr += "ERROR - Unable to retrieve Email address info for $chkue from AD"
                }
            if ($tres -eq $true)
                {
                if ($chkeml -ne $null)
                    {
                    pslog "WARNING - Email address $chkue already exists in AD"
                    updatecount ([ref]$warningcnt) $proctc3 $proc
                    updatetbox1 $proctb1 $proc "WARNING - Email address $chkue already exists in AD`r`n"
                    $script:errarr += "WARNING - Email address $chkue already exists in AD"
                    }
                elseif ($chkeml -eq $null)
                    {
                    pslog "Email address $chkue not found in AD"
                    updatecount ([ref]$successcnt) $proctc2 $proc
                    updatetbox1 $proctb1 $proc "Email address $chkue not found in AD`r`n"
                    }
                else {
                    pslog "ERROR - Unexpected error occured"
                    updatecount ([ref]$errorcnt) $proctc4 $proc
                    updatetbox1 $proctb1 $proc "ERROR - Unexpected error occured`r`n"
                    $script:errarr += "ERROR - Unexpected error occured"
                    }
                }
            elseif ($tres -eq $false)
                {
                pslog "ERROR - Email address info for $chkue not retrieved from AD due to error"
                updatecount ([ref]$errorcnt) $proctc4 $proc
                updatetbox1 $proctb1 $proc "ERROR - Email address info for $chkue not retrieved from AD due to error`r`n"
                $script:errarr +=  "ERROR - Email address info for $chkue not retrieved from AD due to error"
                }
            else
                {
                pslog "ERROR - Unexpected error occured"
                updatecount ([ref]$errorcnt) $proctc4 $proc
                updatetbox1 $proctb1 $proc "ERROR - Unexpected error occured`r`n"
                $script:errarr += "ERROR - Unexpected error occured"
                }
            if (($script:errarr | measure).count -le 0)
                {
                if ($user.usertype -eq "Repex")
                    {
                    $updsur = $chkue.split("_@")[1]
                    }
                else
                    {
                    $updsur = $chkue.split(".@")[1]
                    }
                $remsur = $updsur.replace("$(($user.surname).tolower())","")
                if ($remsur -eq "")
                    {
                    pslog "No additional digit detected to add to surname"
                    updatetbox1 $proctb1 $proc "No digit detected to add to surname`r`n"
                    if ($user.usertype -eq "Repex")
                        {
                        $newsur = $user.surname + " ($($user.companycode))"
                        }
                    else
                        {
                        $newsur = $user.surname
                        }
                    pslog "Name will be set to $($user.firstname + " " + $newsur)"
                    updatetbox1 $proctb1 $proc "Name will be set to $($user.firstname + " " + $newsur)`r`n"
                    }
                elseif ($remsur -ne "")
                    {
                    pslog "Detected digit $remsur to be added to surname"
                    updatetbox1 $proctb1 $proc "Detected digit $remsur to be added to surname`r`n"
                    if ($user.usertype -eq "Repex")
                        {
                        $newsur = $user.surname + $remsur + " ($($user.companycode))"
                        }
                    else
                        {
                        $newsur = $user.surname + $remsur
                        }
                    pslog "Name will be set to $($user.firstname + " " + $newsur)"
                    updatetbox1 $proctb1 $proc "Name will be set to $($user.firstname + " " + $newsur)`r`n"
                    }
                else
                    {
                    pslog "ERROR - Unexpected error occured"
                    updatecount ([ref]$errorcnt) $proctc4 $proc
                    updatetbox1 $proctb1 $proc "ERROR - Unexpected error occured`r`n"
                    $script:errarr += "ERROR - Unexpected error occured"
                    }
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
                    updatetbox1 $proctb1 $proc "UserID $chkid ($procname) created in AD`r`n"
                    }
                catch {
                    pslog "ERROR - Unable to create UserID $chkid ($procname)"
                    updatecount ([ref]$errorcnt) $proctc4 $proc
                    updatetbox1 $proctb1 $proc "ERROR - Unable to create UserID $chkid ($procname)`r`n"
                    $script:errarr += "ERROR - Unable to create UserID $chkid ($procname)"
                    }
                pslog "Confirming account $chkid ($procname) in AD"
                updatetbox1 $proctb1 $proc "Confirming account $chkid ($procname) is in AD`r`n"
                $chkcnt = 0
                $tres = $false
                do {
                    $chkcnt++
                    try {
                        get-aduser -server $script:dcsrv $chkid -erroraction stop
                        pslog "Account $chkid ($procname) confirmed in AD"
                        updatecount ([ref]$successcnt) $proctc2 $proc
                        updatetbox1 $proctb1 $proc "Account $chkid ($procname) confirmed in AD`r`n"
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
                        updatetbox1 $proctb1 $proc "Setting expiry date to NEVER for $chkid ($procname)`r`n"
                        try {
                            clear-adaccountexpiration -server $script:dcsrv -identity $chkid -erroraction stop
                            pslog "Expiry date for $chkid ($procname) set to NEVER"
                            updatecount ([ref]$successcnt) $proctc2 $proc
                            updatetbox1 $proctb1 $proc "Expiry date for $chkid ($procname) set to NEVER`r`n"
                            }
                        catch
                            {
                            pslog "ERROR - Unable to set expiry date to NEVER for $chkid ($procname)"
                            updatecount ([ref]$errorcnt) $proctc4 $proc
                            updatetbox1 $proctb1 $proc "ERROR - Unable to set expiry date to NEVER for $chkid ($procname)`r`n"
                            $script:errarr += "ERROR - Unable to set expiry date to NEVER for $chkid ($procname)"
                            }
                        }
                    else
                        {
                        $exdate = ((get-date).AddMonths(6))
                        pslog "Setting expiry date to $exdate for $chkid ($procname)"
                        updatetbox1 $proctb1 $proc "Setting expiry date to $exdate for $chkid ($procname)`r`n"
                        try {
                            set-adaccountexpiration -server $script:dcsrv -identity $chkid -datetime $exdate -erroraction stop
                            pslog "Expiry date for $chkid ($procname) set to $exdate"
                            updatecount ([ref]$successcnt) $proctc2 $proc
                            updatetbox1 $proctb1 $proc "Expiry date for $chkid ($procname) set to $exdate`r`n"
                            }
                        catch
                            {
                            pslog "ERROR - Unable to set expiry date to $exdate for $chkid ($procname)"
                            updatecount ([ref]$errorcnt) $proctc4 $proc
                            updatetbox1 $proctb1 $proc "ERROR - Unable to set expiry date to $exdate for $chkid ($procname)`r`n"
                            $script:errarr += "ERROR - Unable to set expiry date to $exdate for $chkid"
                            }
                        }
    ########## Groups
                    pslog "Adding $chkid ($procname) required to AD groups"
                    updatetbox1 $proctb1 $proc "Adding $chkid ($procname) required to AD groups`r`n"
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
                    updatetbox1 $proctb1 $proc "Adding info to $chkid ($procname)`r`n"
                    pslog "Getting existing info for $chkid ($procname)"
                    updatetbox1 $proctb1 $proc "Getting existing info for $chkid ($procname)`r`n"
                    $infcnt = 0
                    $tres = $false
                    do {
                        $infcnt++
                        try {
                            $updinf = get-aduser $chkid -properties * -erroraction stop | select -ExpandProperty info
                            pslog "Existing info retrieved for $chkid ($procname)"
                            updatecount ([ref]$successcnt) $proctc2 $proc
                            updatetbox1 $proctb1 $proc "Existing info retrieved for $chkid ($procname)`r`n"
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
                        updatetbox1 $proctb1 $proc "Updating info for $chkid ($procname)`r`n"
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
                            updatetbox1 $proctb1 $proc "Info for $chkid ($procname) updated successfully`r`n"
                            }
                        catch
                            {
                            pslog "ERROR - Unable to update info for $chkid ($procname)"
                            updatecount ([ref]$errorcnt) $proctc4 $proc
                            updatetbox1 $proctb1 $proc "ERROR - Unable to update info for $chkid ($procname)`r`n"
                            $script:errarr += "ERROR - Unable to update info for $chkid ($procname)"
                            }
                        }
                    else
                        {
                        pslog "ERROR - Unable to retrieve existing info for $chkid ($procname)"
                        updatecount ([ref]$errorcnt) $proctc4 $proc
                        updatetbox1 $proctb1 $proc "ERROR - Unable to retrieve existing info for $chkid ($procname)`r`n"
                        $script:errarr += "ERROR - Unable to retrieve existing info for $chkid ($procname)"
                        }
    ########## Password
                    pslog "Sending initial password for $procname to $($user.requester)"
                    updatetbox1 $proctb1 $proc "Sending initial password for $procname to $($user.requester)`r`n"
                    mailpwd $user.requester "$($user.firstname) $($user.surname)" $user.reqno $script:initpw 
                    }
                else
                    {
                    pslog "ERROR - Created account $chkid ($procname) not found in AD"
                    updatecount ([ref]$errorcnt) $proctc4 $proc
                    updatetbox1 $proctb1 $proc "ERROR - Created account $chkid ($procname) not found in AD`r`n"
                    $script:errarr += "ERROR - Created account $chkid ($procname) not found in AD"
                    }
                }
            elseif (($script:errarr | measure).count -gt 0)
                {
                pslog "WARNING - Either the UserID, Email address or UPN already exists in AD"
                updatecount ([ref]$warningcnt) $proctc3 $proc
                updatetbox1 $proctb1 $proc "WARNING - Either the UserID, Email address or UPN already exists in AD`r`n"
                $script:errarr +=  "WARNING - Either the UserID, Email address or UPN for $chkid already exists in AD"            
                }
            else
                {
                pslog "ERROR - Unexpected error occured"
                updatecount ([ref]$errorcnt) $proctc4 $proc
                updatetbox1 $proctb1 $proc "ERROR - Unexpected error occured`r`n"
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
    updatetbox1 $proctb1 $proc "Processing $($param1.toupper()) AD groups for $param2 ($param3)`r`n"
    pslog "Importing list of $($param1.toupper()) AD groups"
    updatetbox1 $proctb1 $proc "Importing list of $($param1.toupper()) AD groups`r`n"
    $tres = $false
    $grpin = "$script:rootpath\grp-$param1.list"
    try {
        $grplist = get-content $grpin -erroraction stop
        pslog "List of $($param1.toupper()) AD groups imported successfully"
        updatecount ([ref]$successcnt) $proctc2 $proc
        updatetbox1 $proctb1 $proc "List of $($param1.toupper()) AD groups imported successfully`r`n"
        $tres = $true
        }
    catch {
        pslog "ERROR - Unable to import list of $($param1.toupper()) AD groups"
        updatecount ([ref]$errorcnt) $proctc4 $proc
        updatetbox1 $proctb1 $proc "ERROR - Unable to import list of $($param1.toupper()) AD groups`r`n"
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
                updatetbox1 $proctb1 $proc "Group $adgrp found in AD`r`n"
                $tres = $true
                }
            catch {
                pslog "ERROR - Unable to find group $adgrp in AD"
                updatecount ([ref]$errorcnt) $proctc4 $proc
                updatetbox1 $proctb1 $proc "ERROR - Unable to find group $adgrp in AD`r`n"
                $script:errarr += "ERROR - Unable to find group $adgrp in AD"
                }
            if ($tres -eq $true)
                {
                try {
                    add-adgroupmember -server $script:dcsrv -identity $adgrp -members $param2 -erroraction stop
                    pslog "$param2 added to $adgrp"
                    updatecount ([ref]$successcnt) $proctc2 $proc
                    updatetbox1 $proctb1 $proc "$param2 added to $adgrp`r`n"
                    }
                catch {
                    pslog "ERROR - Unable to add $param2 to $adgrp"
                    updatecount ([ref]$errorcnt) $proctc4 $proc
                    updatetbox1 $proctb1 $proc "ERROR - Unable to add $param2 to $adgrp`r`n"
                    $script:errarr += "ERROR - Unable to add $param2 to $adgrp"
                    }
                }
            else
                {
                pslog "ERROR - Unexpected error occured"
                updatecount ([ref]$errorcnt) $proctc4 $proc
                updatetbox1 $proctb1 $proc "ERROR - Unexpected error occured`r`n"
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
    updatetbox1 $proctb1 $proc "Generating initial password email for $chkid ($procname)`r`n"

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
    <p class='title'>Info - Request $($param3.toupper())</p>
    "
    $mailclose =
    "
    <p class='text'>Thanks,</p>
    <p class='signoff'>corpo Windows Project Team</p>
    <br>
    <br>    
    "
    $mailcontent =
    "
    <table>
        <tr>
            <th><span class='header'>Request</span></th>
            <th><span class='header'>Name</span></th>
            <th><span class='header'>Password</span></th>
        </tr>
        <tr>
            <td><span class='info'>$($param3.toupper())</span></td>
            <td><span class='info'>$param2</span></td>
            <td><span class='info'>$param4</span></td>
        </tr>
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
    $to = "$param1"
    $bcc = "john.clark@corpo.co.uk"
    try {
        Send-MailMessage -From "joiner-o-matic@corpo.co.uk" -To $to -Bcc $bcc -Subject "Info - Request $($param3.toupper())" -SmtpServer $script:smtpsrv -BodyAsHtml -Body $mailbody -erroraction stop
        pslog "Initial password email for $chkid sent successfully"
        updatecount ([ref]$successcnt) $proctc2 $proc
        updatetbox1 $proctb1 $proc "Initial password email for $chkid sent successfully`r`n"
        }
    catch
        {
        pslog "ERROR - Unable to send initial password email for $chkid"
        updatecount ([ref]$errorcnt) $proctc4 $proc
        updatetbox1 $proctb1 $proc "ERROR - Unable to send initial password email for $chkid`r`n"
        $script:errarr += "ERROR - Unable to send initial password email for $chkid"
        }

}

# ---------------------------------------------------------------------------------------------------- #

function mailadm {

    pslog "Getting email of administrative user creating IDs ($env:username)"
    updatetbox1 $proctb1 $proc "Getting email of administrative user creating IDs ($env:username)`r`n"
    $admusr = ($env:username.split("-"))[0]
    $tres = $false
    try {
        $admmail = (get-aduser $admusr -properties * -erroraction stop | select mail).mail
        pslog "Administrative user email is $admmail for $admusr"
        updatecount ([ref]$successcnt) $proctc2 $proc
        updatetbox1 $proctb1 $proc "Administrative user email is $admmail for $admusr`r`n"
        $tres = $true
        }
    catch {
        pslog "ERROR - Unable to determine administrative user email"
        updatecount ([ref]$errorcnt) $proctc4 $proc
        updatetbox1 $proctb1 $proc "ERROR - Unable to determine administrative user email`r`n"
        $script:errarr += "ERROR - Unable to determine administrative user email"
        }
    if ($tres -eq $true)
        {
        if (($script:pwdarr | measure).count -gt 0)
            {
            pslog "Generating administrative user summary email"
            updatetbox1 $proctb1 $proc "Generating administrative user summary email`r`n"
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
                updatetbox1 $proctb1 $proc "Administrative user summary email sent successfully to $admmail`r`n"
                }
            catch
                {
                pslog "ERROR - Unable to send administrative user summary email to $admmail"
                updatecount ([ref]$errorcnt) $proctc4 $proc
                updatetbox1 $proctb1 $proc "ERROR - Unable to send administrative user summary email to $admmail`r`n"
                $script:errarr += "ERROR - Unable to send administrative user summary email to $admmail"
                }
            }
        else
            {
            pslog "No password info to send to dministrative user $admmail"
            updatetbox1 $proctb1 $proc "No password info to send to dministrative user $admmail`r`n"
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

    $menutitle = $boxtitle
    $menuheight = 155
    $menuwidth = 355
    $menucolour = "White"

    $confbox = New-Object System.Windows.Forms.Form
    $confbox.Text = $menutitle
    $confbox.Height = $menuheight
    $confbox.Width = $menuwidth
    $confbox.BackColor = $menucolour
    $confbox.FormBorderStyle = [System.Windows.Forms.FormBorderStyle]::FixedSingle
    $confbox.StartPosition = [System.Windows.Forms.FormStartPosition]::CenterScreen
    
    $conflabela = New-Object system.Windows.Forms.Label
    $conflabela.TextAlign = "MiddleCenter"
    $conflabela.Text = $param1
    $conflabela.Left = 0
    $conflabela.Top = 0
    $conflabela.Width = 350
    $conflabela.Height = 40
    $conflabela.Font = $script:NameFont
    $confbox.controls.add($conflabela)
    
    $conflabelb = New-Object system.Windows.Forms.Label
    $conflabelb.TextAlign = "MiddleCenter"
    $conflabelb.Text = $param2
    $conflabelb.Left = 0
    $conflabelb.Top = 30
    $conflabelb.Width = 350
    $conflabelb.Height = 40
    $conflabelb.Font = $script:MainFont
    $confbox.controls.add($conflabelb)

    $confbuta = New-Object System.Windows.Forms.Button
    $confbuta.Left = 130
    $confbuta.Top = 75
    $confbuta.Width = 90
    $confbuta.Height = 25
    $confbuta.BackColor = $script:ButColor
    $confbuta.Text = "Close"
    $confbuta.Add_Click({$confbox.Close();pslog "Close selected by $env:username"})
    $confbox.Controls.Add($confbuta)

    $confbox.Topmost = $True    
    $confbox.Add_Shown({$confbox.Activate()})
    [void] $confbox.ShowDialog()
    [void] $confbox.Focus()

    $confbox.close()

    close "Closing $boxtitle"

}

# ---------------------------------------------------------------------------------------------------- #
# -------------------------------------  Specific Functions Above ------------------------------------ #
# ---------------------------------------------------------------------------------------------------- #

# ---------------------------------------------------------------------------------------------------- #
# ------------------------------------------- Script Below ------------------------------------------- #
# ---------------------------------------------------------------------------------------------------- #

startlog

boxprereqs

importmods

importassembly

introbox

infobox

addusers

stoplog

# ---------------------------------------------------------------------------------------------------- #
# ------------------------------------------- Script Above ------------------------------------------- #
# ---------------------------------------------------------------------------------------------------- #
