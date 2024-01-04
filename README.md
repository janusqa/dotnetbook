1. Install repo and SDK
   1. sudo apt update
   2. sudo apt upgrade
   3. wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
   4. sudo dpkg -i packages-microsoft-prod.deb
   5. rm packages-microsoft-prod.deb
   6. sudo apt update
   7. sudo apt upgrade
   8. sudo apt-get install -y dotnet-sdk-7.0

2. Install C# plugin for vscode

3. dotnet new gitignore
4. dotnet new tool-manifest

6. Scaffold the type of app you want
   1. dotnet new sln
   2. dotnet new console --output [<foldername/namespace>] --framework net7.0  //console app
   3. dotnet new classlib --output [<foldername/namespace>] --framework net7.0  //class library app
   4. dotnet new mstest --output [<foldername/namespace>] //unit test app
   5. after adding your various projects need to now add them to the solution you oringally created
      1. dotnet sln add [<foldername/namespace>]/[<foldername/namespace>].csproj
   6. to reference a classlib in a console/desktop app
      1. dotnet add [<namespace-folder-console-desktop>]/[<namespace-folder-console-desktop>].csproj reference [<namespace-folder-classlib>]/[<namespace-folder-classlib>].csproj
      2. in the case of test projects... 
         1. dotnet add [<testproject>]/[<testproject>].csproj reference [<mainproject>/<mainproject>].csproj
         2. Run test: dotnet test [<testproject>/<testproject>].csproj

7. CRTL+SHIFT+P.  In command palette type ">.NET: Generate Assets for Build and Debug".
   
8. dotnet run // run source code 
9. dotnet run --project [<project-name>] eg. dotnet run --project day01

10. dotnet publish -c release -r ubuntu.16.04-x64 --self-contained  // compile console app as "executable"

# Linting
dotnet new editorconfig

add the below to .vscode/settings.json
{
  "omnisharp.enableRoslynAnalyzers": true,
  "omnisharp.enableEditorConfigSupport": true
}

change .editorconfig to contain only 
```
root = true

[*.cs]
dotnet_analyzer_diagnostic.category-Style.severity = warning

dotnet_diagnostic.IDE0003.severity = none
dotnet_diagnostic.IDE0008.severity = none
dotnet_diagnostic.IDE0058.severity = none
dotnet_diagnostic.IDE0160.severity = none
```

add "<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>"  to .csproj file.


QuickStart
----
dotnet new gitignore
dotnet new tool-manifest
dotnet new sln --name mySolution
dotnet new mvc --output myProject --name myProject --framework net8.0 (create an ASP.net Core mvc project) 
dotnet sln add myProject/myProject.csproj
dotnet run --project myProject

Optional
--------
// Add a package from nuget to a project
dotnet add [<PROJECT>] package [<PACKAGE>]
dotnet add myProject package Microsoft.Z3 --version 4.11.2

// Remove a package installed with nuget to a project
dotnet remove [<PROJECT>] package [<PACKAGE>]

// List packages in a project
dotnet list [<PROJECT>] package 
