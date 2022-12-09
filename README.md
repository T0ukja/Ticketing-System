# Ticketing-System
Ticketing system made using C# Blazor with EWS and Database


How to start using this system?
<ol>
<li>First create cred.txt file to root of the project (/BlazorApp1)</li>
<li>Second Fill the credt.txt file with your email where you wanna receive tickets
 <ul>
 <li> fill cred.txt file like this </li>
      <li>email password https://outlook.office365.com/EWS/Exchange.asmx</li>
 
    

</li>
</ul>
<li>You need to create mongodb database and configure to this project</li>
<li> You only need to create database and create 4 collections to it</li>
<li>Next when you have created collections, you need to give those credentials to this software</li>
<li>Second Fill the credt.txt file with your email where you wanna receive tickets
 <ul>
 <li> open/create appsettings.json file </li>
 <li>copy paste this code block and change values with your own database information</li>
 
 ***
 {
    "Logging": {
        "LogLevel": {
            "Default": "Information",
            "Microsoft.AspNetCore": "Warning"
        },
        "AllowedHosts": "*"
    },

    "EmailDatabase": {
        "ConnectionString": "secret",
        "DatabaseName": "secret",
        "CollectionName": "secret"

    },


    "LoginDatabase": {
        "ConnectionString": "secret",
        "DatabaseName": "secret",
        "CollectionName": "secret"

    },


    "CommentDatabase": {
        "ConnectionString": "secret",
        "DatabaseName": "secret",
        "CollectionName": "secret"

    },

    "HistoryDatabase": {
        "ConnectionString": "secret",
        "DatabaseName": "secret",
        "CollectionName": "secret"

    },
    "NewEmailsDatabase": {
        "ConnectionString": "secret",
        "DatabaseName": "secret",
        "CollectionName": "secret"

    }
}

***
 
    

</li>
</ul>


<li>Packages that are installed to this project already (good to know)</li>
<ul>
<p>https://www.nuget.org/packages/BCrypt.Net-Next/4.0.3?_src=template</p>
<p>https://www.nuget.org/packages/Blazored.SessionStorage/2.2.0?_src=template</p>
<p>https://www.nuget.org/packages/Microsoft.Exchange.WebServices/2.2.0?_src=template</p>
<p>https://www.nuget.org/packages/MongoDB.Bson/2.18.0?_src=template</p>
<p>https://www.nuget.org/packages/MongoDB.Driver/2.18.0?_src=template</p>
<p>https://www.nuget.org/packages/Syncfusion.Blazor.Buttons/20.3.0.56?_src=template</p>
<p>https://www.nuget.org/packages/BlazorStrap/5.0.106?_src=template</p>
<p>https://www.nuget.org/packages/Blazorise.Icons.FontAwesome/1.1.3.1?_src=template</p>
<p>https://www.nuget.org/packages/Blazorise.Bootstrap/1.1.3.1?_src=template</p>
</ul>
<li>Test the software, should work without problems</li>
</ol>
