# 365Mapper


## 1. Introduction
**365Mapper** is a desktop utility which maps your office 365 document library to your windows machine.

By mapping your cloud storage to your local devices, the biggest problems it solves are:

- Unlocking available cloud storage to users by giving them their well known and most familiar explorer experience.
- Enabling your organization’s IT team to easily manage various cloud storage drives. eg. If you want to map `M:` drive to your SharePoint online marketing document library and `O:` drive to your OneDrive for Business, you can. Your users don’t even need to know which storage they are using!
- Platform like Office 365 which is already being used by a majority of organizations can harness the power of the cloud easily without retraining costs.
- Access files anywhere, anytime. Just by installing the lightweight client to any machine and logging with your credentials, You can easily access your documents on the fly without needing complex procedures eg. Launching the browser, navigating to your file, download, access and then delete once you’ve finished.
- Secure your storage against viruses using your on-machine Anti-virus product.


## 2. Pre-requisites  

### 2.1	Supported Operating Systems  
365Mapper is supported on the following Windows workstation versions:  

-	Windows 7  
-	Windows 8  
-	Windows 8.1  
-	Windows 10  

### 2.2	Workstation Requirements  
It is recommended that workstations are patched to latest release levels.  In addition, there are three workstation requirements which must be met for 365mapper to work correctly:  

-	`https://tenancy.sharepoint.com` added to local intranet zone in IE settings. Alternatively, `https://*.sharepoint.com` can be added 
-	.NET Framework 4.5 must be installed 
-	The WebClient service must be started 

### 2.3	Workstation Recommendations  
It is highly recommended that the Microsoft hotfix below is installed as it remedies several issues relating to the mapping of drives with 365mapper. 

-	https://support.microsoft.com/kb/2846960

It is recommended to set the following Windows settings for WebDAV in registry: 

-	`HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WebClient\Parameters` 
-	DWORD: `FileSizeLimitInBytes`  

The default is `50000000` bytes (approx. 50MB).  We recommend increasing this to `4294967295` (approx. 4GB).  

Finally, when 365Mapper runs for the first time, it adds a registry setting to the client to automatically run the tool on logon. For centralised roll out and shared clients, it may be required to add this setting to the registry through Group Policy: 

-	Key: `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run`
-	REG_SZ: `365Drive` 
-	Value: Path to the 365 drive executable, for example `C:\Program Files\Identity 
Experts\365mapper\365Drive.Office365.NotificationManager.exe` 

### 2.4 Upgrading from Previous Versions
The following steps detail the process needed to upgrade from a previous version of 365Mapper to the latest version:
  
-	 Uninstall the previous 365Mapper client
-	 Clear down the credentials in Windows Credential Manager (Windows Credentials, 365drive.credential)
-	 Backup and then delete any registry keys (`HKEY_CURRENT_USER\Software\Identity Experts\365mapper`)
-	 Restart
-	 Install the latest client from the releases
-	 Add the tenancy name to the `TenancyName` key (`HKEY_CURRENT_USER\Software\Identity Experts\365mapper`)
-	 Add Mappings, unless you wish to only have the Default SharePoint site (`S:`) and OneDrive (`O:`) mappings 
-	 Reboot


## 3.	365Mapper Client Installation  

### 3.1	Installation  
Run the 365Mapper MSI for the appropriate architecture (x86 or x64) from the [ZIP](https://github.com/identity-experts/O365Mapper/releases/download/v2.0.0.0/365Mapper_2.0.0.0.zip) file provided in the [releases](https://github.com/identity-experts/O365Mapper/releases).
   
#### 3.1.1 MSI Exec Options  
The MSI installer can be run with the ‘quiet’ option. To do this, run the following:  

- `msiexec /i 365mapper.msi /qn`  

### 3.2	Adding sharepoint.com and login.microsoftonline.com to Trusted Zone  
Three sites must be trusted to ensure the 365Mapper is able to connect: 

-	`tenant.sharepoint.com`* 
-	`tenant-my.sharepoint.com`* 
- `login.microsoftonline.com`  

*Alternatively, add `*.sharepoint.com` 

#### 3.2.1 Method 1: Group Policy  
From GPMC, select the following:  
-	User Configuration > Policies > Administrative Templates > Windows Components > 
Internet Explorer > Internet Control Panel > Security Page   
  
-	Edit the Site to Zone Assignment List 
  
-	Add `tenant.sharepoint.com` with a value of `1`* 
-	Add `tenant-my.sharepoint.com` with a value of `1`*
- Add `login.microsoftonline.com` with a value of `1` 

*Alternatively, add `*.sharepoint.com` with a value of `1` 
     
-	Click OK and apply the policy to users 

#### 3.2.2 Method 2: Registry  
-	Load up either an existing group policy or create a new one, edit it and then navigate to: 
    -	User Configuration > Preferences > Registry  
    -	Right-click on Registry and select New > Registry Item  
-	Select and enter the following values for sharepoint.com: 
    - Action: Create  
    -	Hive: `HKEY_CURRENT_USER` Key Path: 
    -	`SOFTWARE\Microsoft\Windows\CurrentVersion\Internet Settings\ZoneMap\Domains\sharepoint.com\`  
    -	Value Name: `https` 
    -	Value Type: REG_DWORD  
    -	Value Data: `1`  
-	Select and enter the following values for microsoftonline.com: 
    - Action: Create  
    -	Hive: `HKEY_CURRENT_USER` Key Path: 
    -	`SOFTWARE\Microsoft\Windows\CurrentVersion\Internet Settings\ZoneMap\Domains\microsoftonline.com\login` 
    -	Value Name: `https` 
    -	Value Type: REG_DWORD  
    -	Value Data: `1`  
-	Click OK  
  	  	 
#### 3.2.3 	Method 3: Directly in Internet Explorer  
In Internet Explorer, select the cog icon in the top right corner, and then **Internet Options** from the menu: 
    
-	Add `tenant.sharepoint.com` to the `zone`* 
-	Add `tenant-my.sharepoint.com` to the `zone`* 
- Add `login.microsoftonline.com` to the `zone` 

*Alternatively, add `*.sharepoint.com` to the `zone` 
   
-	Click OK.  
  	  	  
### 3.3	Ensuring the WebClient service is started  
365Mapper requires the WebClient service to be started on all workstations.  If this doesn’t start automatically, using Group Policy, add the following in Policy Editor:  

-	Navigate to Computer Configuration > Preferences > Control Panel Settings > Services  
-	Right-click in the window and choose New > Service  
-	Change Startup to Automatic, enter WebClient for the service name and change Service Action to Start Service  
-	Click on OK and ensure you link the policy to any Active Directory Organisational Units containing users or workstations you wish to use 365Mapper.  

### 3.4	Registry Changes  
It is recommended to set the following Windows settings for WebDAV in registry: 
-	`HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WebClient\Parameters`  
-	DWORD: `FileSizeLimitInBytes`  
The default is `50000000` bytes (approx. 50MB).  We recommend increasing this to `4294967295` (approx. 4GB). 


## 4	Using the 365Mapper Client  

### 4.1	Client configuration  
When a user first logs on with the 365Mapper client running, the client will inform the user to sign in. The clock icon on the task bar will indicate a response in required by the user.  
   
A prompt should automatically appear for the user, at which point they can enter their Office 365 credentials. After entering their credentials and clicking Save, the Windows Credential Manager stores the Office 365 details against their profile. These will then be used each time the user logs onto the client in question.  
    
If authentication is successful, the client will verify the licence and map any required drives. 
   
The drives can then be accessed through Windows Explorer.  
    
### 4.2	Configuring Drive Mappings Overrides 
If the default mappings configured in the Identity Experts store are not required for all users, each user can have a manual override put in place. If a registry setting is configured, the store settings will not be checked. If they are not present, then the default mappings from the store are used. Here are the steps to manage drive mapping details from registry: 

-	Open the registry editor  
-	Go to following path `HKEY_CURRENT_USER\SOFTWARE\Identity Experts\365mapper`   
-	Create a new SubKey under `365mapper` Key called `Mappings` as shown in following diagram: 
 
-	Create a new String value (REG_SZ) inside the Mappings Key with following details: 
    -	Name: The drive letter (e.g. `E`,`F`,`Z`,`N…`)  
    -	Data: Separated by semicolon ( ; ), add the following data values: 
        -	The first value contains the URL   o If its OneDrive for business OR SharePoint Root site collection default library set the value as follows  
            -	`[ODB]` – will be treated as logged in users’ OneDrive for Business library  
            -	`[SPO]` – will be treated as root site collection’s default document library (`https://tenancy.sharepoint.com/Shared Documents`)  
        - For any other document library, pass Document Library URL (e.g. `https://tenancy.sharepoint.com/sites/sales/Shared Documents`) 
    -	The second value is the label of the drive 
    -	Third option is optional which is expecting un-map parameter. Which means if we need to un-map a drive, we need to pass `[x]`
    -	An example of the Data value would be `https://tenancy.sharepoint.com/sites/testsitecoll/Shared Documents;Docs:[x]`  
-	You can add multiple string values for multiple drive mappings 

### 4.3	ADFS / AADConnect SSO Settings  
ADFS / AADConnect SSO is handled automatically by 365Mapper without any extra inputs. The way this works is explained below. The logon name, or UserPrincipleName (UPN), is selected from user’s Azure Active Directory UPN value, however if the Office 365 UPN is not stored in the Azure Active Directory UPN field, please contact us to have an exceptional 365Mapper executable to read the UPN from a different field. 


## 5. Frequently Asked Questions 

### 5.1	My mapped drive size shows the same as local drive 
This is a known issue in WebDav which Microsoft have documented at: 

-	https://support.microsoft.com/en-us/help/2386902/webdav-mapped-drive-reportsincorrect-drive-capacity  

In addition, it isn’t possible to show the remaining drive size of a SharePoint document library because: 

-	The SharePoint document library does not have any library limit. 30 million documents can be added however Microsoft doesn’t have any hard size limit on document library  
-	A document library sits under a SharePoint site collection and that site collection size quota can be set in SharePoint Admin area. A site collection with a size quota of, for example, 10GB may have several libraries. In this scenario, the 10GB uses a come first serve policy so although the library has a maximum of 10GB, it may be sharing this quota with other libraries. 

### 5.2	Some of our users cannot authenticate 
In the event of drives not being mapped, the user can hover over the 365Mapper logo for an indication of the issue.  
  
There is is a known bug within earlier versions of the solution with regards passwords containing special characters, for example an ampersand ( & ). This was resolved in version 1.0.0.2 so an upgrade is recommended, or the workaround for older versions is to not use special characters in the password. 
If credentials have recently changed in Office 365, the user can update these either by right clicking the 365Mapper log in the taskbar and selecting Sign In, or by removing the 365drive.credential from Windows Credential manager which will force the re-entry of the credentials.  
 
### 5.3	How do I manage Automated Client Updates? 
365Mapper should automatically update if you exit and relaunch the app.   
If it does not, then you may need to delete the `DontAskForUpdates` registry key found at `HKEY_CURRENT_USER\Software\Identity Experts\365mapper`. Once removed, you should be prompted to upgrade on next launch. 
However, often installations are managed by administrators with end users not having the appropriate permissions. In this instance, you can disable the auto upgrade feature by setting the following string value in the registry:  

-	Path: `HKEY_CURRENT_USER\Software\Identity Experts\365mapper`  
-	String Key Name: `DontAskForUpdates`  
-	Value: `1`  

Please make sure this is a key for `HKEY_CURRENT_USER` and this KEY is available before 365Mapper is launched. If the user logging in has a remote runtime profile, the writing of this registry key must happen first before the launch of 365Mapper. 
 
### 5.4	What authentication methods does 365Mapper support? 
365Mapper supports a variety of authentication methods including: 

-	In cloud authentication (including synchronised passwords) 
-	Pass-Through Authentication 
-	Active Directory Federated Services (ADFS) 
-	Multi-Factor Authentication 

The authentications, as of version 1.0.0.1 also work with F5 BigIP integration. 

### 5.5	Will 365Mapper work now we have enabled Multi-Factor Authentication (MFA)? 
Yes. 365Mapper works with the supported Microsoft MFA functions such as: 

-	Phone call 
-	SMS text message 
-	Microsoft Authenticator application 

After the stored credentials are used for the first part of the authentication, a window will appear asking the user if they would like to prompt for MFA, or delay the prompt for up to 24 hours. After confirming an MFA prompt, one of the MFA methods will be performed by 
Microsoft and once verification is complete, the user can select **Verify Now** to complete their MFA. Once complete, the drives will be mapped  
Please be aware, an MFA token expires after 24 hours, at which point the MFA prompt will be required 

### 5.6	What version of 365Mapper am I using? 
By double clicking the 365Mapper taskbar icon, the user can verify the version they are using. Alternatively, the registry can be checked at: 

- `HKEY_CURRENT_USER\Software\Identity Experts\365mapper`  
- String Value: `Version` 
  
### 5.7	My 365Mapper will not run automatically when I log on 
When 365Mapper runs for the first time, it adds a registry setting to the client to automatically run the tool on logon. For centralised roll out and shared clients, it may be required to add this setting to the registry through Group Policy: 

-	Key: `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run` 
-	REG_SZ: `365Drive` 
-	Value: Path to the 365 drive executable, for example `C:\Program Files\Identity Experts\365mapper\365Drive.Office365.NotificationManager.exe` 
