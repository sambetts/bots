function Start-Sleep($seconds) {
    $doneDT = (Get-Date).AddSeconds($seconds)
    while($doneDT -gt (Get-Date)) {
        $secondsLeft = $doneDT.Subtract((Get-Date)).TotalSeconds
        $percent = ($seconds - $secondsLeft) / $seconds * 100
        Write-Progress -Activity "Sleeping" -Status "Sleeping..." -SecondsRemaining $secondsLeft -PercentComplete $percent
        [System.Threading.Thread]::Sleep(500)
    }
    Write-Progress -Activity "Sleeping" -Status "Sleeping..." -SecondsRemaining 0 -Completed
}

#Install dependancies
write-host "Info: Install dependancies" -foregroundcolor magenta
If (-not(Get-InstalledModule Microsoft.Online.SharePoint.PowerShell)) {
    Write-Host "Module Microsoft.Online.SharePoint.PowerShell does not exist, installing." -foregroundcolor magenta
    Install-Module -Name Microsoft.Online.SharePoint.PowerShell
	} else {
	write-host "Info: Module Microsoft.Online.SharePoint.PowerShell already Installed" -foregroundcolor magenta
}

If (-not(Get-InstalledModule PnP.PowerShell)) {
    Write-Host "Module PnP.PowerShell does not exist, installing." -foregroundcolor magenta
    Install-Module -Name PnP.PowerShell
	} else {
	write-host "Info: Module PnP.PowerShell already Installed" -foregroundcolor magenta
}

If (-not(Get-InstalledModule AzureAD)) {
    Write-Host "Module AzureAD does not exist, installing." -foregroundcolor magenta
    Install-Module -Name AzureAD
	} else {
	write-host "Info: Module AzureAD already Installed" -foregroundcolor magenta
}


If(!(Get-Module -Name Microsoft.Online.SharePoint.PowerShell -ListAvailable | Select Name,Version)){
    Install-Module -Name Microsoft.Online.SharePoint.PowerShell -Scope CurrentUser
}



write-host "Info: Updating modules to latest versions" -foregroundcolor magenta
Update-Module -Name Microsoft.Online.SharePoint.PowerShell
Update-Module -Name PnP.PowerShell
Update-Module -Name AzureAD

Write-Host "Please input your Tenant Name (You can find it in your Sharepoint URL before .sharepoint.com  https://contoso.sharepoint.com): "
$TenantName = Read-Host
Write-Host "Please input a name for your Site: "
$SiteName = Read-Host

$TenantAdminURL = "https://" + $TenantName + "-admin.sharepoint.com"
$TenantClientURL = "https://" + $TenantName + ".sharepoint.com"
$SiteURL = $TenantClientURL + "/sites/" + $SiteName
$SiteDescription = "Site for the " + $SiteName + " app"
$StorageQuota = "26214400" #This is the default value
$SiteTemplate = "STS#3"

Connect-PnPOnline -Interactive -Url $TenantAdminURL -ForceAuthentication


#verify if site already exists in SharePoint Online
write-host "Info: Checking if site already exists" -foregroundcolor magenta
$siteExists = Get-PnPTenantSite | where{$_.url -eq $SiteURL}

#verify if site already exists in the recycle bin
write-host "Info: Checking if site already exists in the recycle bin" -foregroundcolor magenta
$siteExistsInRecycleBin = Get-PnPTenantDeletedSite | where{$_.url -eq $SiteURL}

#create site if it doesn't exists
if (($siteExists -eq $null) -and ($siteExistsInRecycleBin -eq $null)) {
    write-host "Info: Creating $($SiteName)" -foregroundcolor magenta
    #Create a new site with the Doc Centre template BDR#0
    New-PnPSite  -Type TeamSite -Title $SiteName -Description $SiteDescription -Alias $SiteName -IsPublic 
	write-host "Info: Site $($SiteName) Created" -foregroundcolor magenta
}
elseif ($siteExistsInRecycleBin -ne $null){
    write-host "Error: $($SiteURL) exists in the Recyclebin" -foregroundcolor red
    exit
}

write-host "Info: Waiting for provisioning to complete" -foregroundcolor magenta
Start-Sleep(30)
write-host "Info: Creating lists" -foregroundcolor magenta
Connect-PnPOnline -Interactive -Url $SiteURL -ForceAuthentication
#Create the courses list
New-PnPList -Title 'Courses' -Template GenericList -Url Lists/Courses -ErrorAction Continue

Add-PnPField -List "Courses" -DisplayName "Start" -InternalName "Start" -Type DateTime -AddToDefaultView | Out-Null
Add-PnPField -List "Courses" -DisplayName "End" -InternalName "End" -Type DateTime -AddToDefaultView | Out-Null
Add-PnPField -List "Courses" -DisplayName "WelcomeMessage" -InternalName "WelcomeMessage" -Type Text -AddToDefaultView | Out-Null
Add-PnPField -List "Courses" -DisplayName "Facilitator" -InternalName "Trainer" -Type User -AddToDefaultView | Out-Null
Add-PnPField -List "Courses" -DisplayName "DaysBeforeToSendReminders" -InternalName "DaysBeforeToSendReminders" -Type Integer -AddToDefaultView | Out-Null
Add-PnPField -List "Courses" -DisplayName "Image" -InternalName "Image" -Type Thumbnail -AddToDefaultView | Out-Null
Add-PnPField -List "Courses" -DisplayName "TeamID" -InternalName "TeamID" -Type Text -AddToDefaultView | Out-Null
Add-PnPField -List "Courses" -DisplayName "ChannelID" -InternalName "ChannelID" -Type Text -AddToDefaultView | Out-Null
Set-PnPField -List "Courses" -Identity "Title" -Values @{Required=$false}

#Create the Course Checklist List
New-PnPList -Title 'Course Checklist' -Template GenericList -Url Lists/Course%20Checklist -ErrorAction Continue
Add-PnPField -List "Course Checklist" -DisplayName "CourseID" -InternalName "CourseID" -Type Integer -AddToDefaultView | Out-Null
Add-PnPField -List "Course Checklist" -DisplayName "Description" -InternalName "Description" -Type Text -AddToDefaultView | Out-Null
Add-PnPField -List "Course Checklist" -DisplayName "Resource to use" -InternalName "Resourcetouse" -Type URL -AddToDefaultView | Out-Null
Add-PnPField -List "Course Checklist" -DisplayName "AssociatedModule" -InternalName "AssociatedModule" -Type Text -AddToDefaultView | Out-Null
Add-PnPField -List "Course Checklist" -DisplayName "Learning Program" -InternalName "Module" -Type Text -AddToDefaultView | Out-Null
Set-PnPField -List "Course Checklist" -Identity "Title" -Values @{Required=$false}

#Create the Course Checklist Confirmation List
New-PnPList -Title 'Checklist Confirmations' -Template GenericList -Url Lists/Checklist%20Confirmations -ErrorAction Continue
Add-PnPField -List "Checklist Confirmations" -DisplayName "DoneBy" -InternalName "DoneBy" -Type User -AddToDefaultView | Out-Null
Add-PnPField -List "Checklist Confirmations" -DisplayName "CheckListID" -InternalName "CheckListID" -Type Integer -AddToDefaultView | Out-Null
Set-PnPField -List "Checklist Confirmations" -Identity "Title" -Values @{Required=$false}

#Create the Course Attendance Confirmation List
New-PnPList -Title 'Course Attendance' -Template GenericList -Url Lists/Course%20Attendance -ErrorAction Continue
Add-PnPField -List "Course Attendance" -DisplayName "AssignedUser" -InternalName "AssignedUser" -Type User -AddToDefaultView | Out-Null
Add-PnPField -List "Course Attendance" -DisplayName "QARole" -InternalName "QARole" -Type Text -AddToDefaultView | Out-Null
Add-PnPField -List "Course Attendance" -DisplayName "QAOrg" -InternalName "QAOrg" -Type Text -AddToDefaultView | Out-Null
Add-PnPField -List "Course Attendance" -DisplayName "QACountry" -InternalName "QACountry" -Type Text -AddToDefaultView | Out-Null
Add-PnPField -List "Course Attendance" -DisplayName "QASpareTimeActivities" -InternalName "QASpareTimeActivities" -Type Text -AddToDefaultView | Out-Null
Add-PnPField -List "Course Attendance" -DisplayName "QAMobilePhoneNumber" -InternalName "QAMobilePhoneNumber" -Type Text -AddToDefaultView | Out-Null
Add-PnPField -List "Course Attendance" -DisplayName "CourseAttendanceID" -InternalName "CourseattendanceID" -Type Integer -AddToDefaultView | Out-Null
Add-PnPField -List "Course Attendance" -DisplayName "BotContacted" -InternalName "BotContacted" -Type Boolean -AddToDefaultView | Out-Null
Add-PnPField -List "Course Attendance" -DisplayName "IntroductionDone" -InternalName "IntroductionDone" -Type Boolean -AddToDefaultView | Out-Null
Set-PnPField -List "Course Attendance" -Identity "Title" -Values @{Required=$false}


write-host "Info: Your Site is ready at " $SiteURL -foregroundcolor magenta
