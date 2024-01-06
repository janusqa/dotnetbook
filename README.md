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

3. Create a soluton
   1. dotnet new gitignore
   2. dotnet new tool-manifest
   3. dotnet tool install dotnet-ef (I think this may be optional if you are intending to use Entity Framework. It's a big topic)
   4. dotnet tool install dotnet-aspnet-codegenerator 
   5. dotnet tool update dotnet-ef
   6. dotnet tool update dotnet-aspnet-codegenerator
   
4. Scaffold the type of app you want
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
   7. dotnet new mvc -au Individual -uld --output [<foldername/namespace>] --framework net7.0  // ASP.Net core web app mvc
      1. The -au Individual paramater makes it use Individual User accounts. The -uld has it use SQL Server instead of SQLite. 
      2. change connecting string in appsettings.json
      3. dotnet ef database update
      4. dotnet add package Microsoft.VisualStudio.Web.CodeGeneration.Design
      5. If PROJECT IS ALREADY SET UP we can still set up -au (authentication) and -uld (use local database)
         1. run the following commands to set up -au (authentication)
            1. dotnet add package Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore
            2. dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
            3. dotnet add package Microsoft.AspNetCore.Identity.UI
            4. dotnet add package Microsoft.EntityFrameworkCore.SqlServer
            5. dotnet add package Microsoft.EntityFrameworkCore.Tools
         2. Now update migrations after adding these packages
            1. dotnet ef migrations add InitialCreate
            2. dotnet ef database update
      6. run the following commands to set up -uld (use local database)
         1. dotnet add package Microsoft.EntityFrameworkCore.SqlServer
         2. dotnet add package Microsoft.EntityFrameworkCore.Tools
         3. Update your appsettings.json file to use SQL Server connection string. Modify the DefaultConnection to point to your SQL Server instance.
         4. Create a "Data" dir in project and add a "ApplicationDBcontex.cs" to it in wich you will define an ApplicationDbContext class. See project to see what this class is like.
         5. Register "ApplicationDbContext" in services area of "Program.cs"
            1. Add ...
            ```
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
               options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
            );
            ```
         6. EF/ConnectionString now configure so now run "dotnet ef database update" to create database
         7. To add a table, in "ApplicationDBContext" class create as many "Dbset" properties as needed.
            ex: Dbset<[<ModelName>]> [<TableName>] {get; set;}
            now perform the apply migrations below
         8. Apply migrations
            1. dotnet ef migrations add [<NameOfMigrationCanBeAnythingYouWantSoMakeItDescriptive>]
            2. dotnet ef database update
            3. IMPORTANT: IF YOU HAVE MOVED Data AND Migrations to their own start libray the comand to use would be
            ```
            dotnet ef migrations add MyMigration --project YourClassLibraryProjectName --startup-project YourWebAppProjectName 
            
            dotnet ef database update --startup-project path\to\your\startup\project.csproj
            ```
            The startup project is where the DbContext is.



5. CRTL+SHIFT+P.  In command palette type ">.NET: Generate Assets for Build and Debug".
   
6. dotnet run // run source code 
7. dotnet run --project [<project-name>] eg. dotnet run --project day01

8.  dotnet publish -c release -r ubuntu.16.04-x64 --self-contained  // compile console app as "executable"

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

# Additional scaffolding for ASP.NET Core Web Application (WHEN DONE MANUALLLY)
*** MUST BE IN PROJECT DIRECTORY NOT SOLUTION/ROOT DIRECTORY ***
dotnet add package Microsoft.VisualStudio.Web.CodeGeneration.Design
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.EntityFrameworkCore.Tools
```
replace "Server=(localdb)\\mssqllocaldb;Database=jokes;Trusted_Connection=True;MultipleActiveResultSets=true
with     "Server=localhost,1433;Database=jokes;User Id=sa;Password=P@ssw0rd;TrustServerCertificate=True;MultipleActiveResultSets=true"
```
# Uninstall tools
dotnet tool uninstall [<PACKAGE_NAME>] --global
dotnet tool uninstall [<PACKAGE_NAME>]   // package installed to the project itself

# Scaffold a controller based on a model, and create associated views too
*** MUST BE IN PROJECT DIRECTORY NOT SOLUTION/ROOT DIRECTORY ***
dotnet aspnet-codegenerator controller --controllerName JokesController --model Joke --dataContext ApplicationDbContext --relativeFolderPath Controllers --referenceScriptLibraries --useDefaultLayout --layout "/Views/Shared/_Layout.cshtml" --force

# Scaffold a view 
*** MUST BE IN PROJECT DIRECTORY NOT SOLUTION/ROOT DIRECTORY ***
dotnet aspnet-codegenerator view Search Create --model Joke --dataContext ApplicationDbContext --relativeFolderPath Views/Jokes --referenceScriptLibraries --useDefaultLayout --partialView

# Updataing the DB via migrations
dotnet ef migrations add [<NameOfMigrationGoesHere>]  // Add a migration
dotnet ef migrations remove [<NameOfMigrationGoesHere>]  // remove a migration
dotnet ef database update


QuickStart
----
dotnet new gitignore
dotnet new tool-manifest
dotnet new sln --name mySolution
dotnet new mvc --output myProject --name myProject --framework net8.0 (create an ASP.net Core mvc project) 
dotnet sln add myProject/myProject.csproj
dotnet run --project myProject
dotnet run --project myProject --launch-profile https  // starts app with a profile in myProject/Properties/launchSettings.json
dotnet watch --project myProject --launch-profile https // this hot reloads changes to views
// open browser to http://localhost:5162.  This infromation is in myProject/Properties/launchSettings.json
// use  myProject/Properties/launchSettings.json to change which port app is accessible from if you like

Optional
--------
// Add a package from nuget to a project
dotnet add [<PROJECT>] package [<PACKAGE>]
dotnet add myProject package Microsoft.Z3 --version 4.11.2

// Remove a package installed with nuget to a project
dotnet remove [<PROJECT>] package [<PACKAGE>]

// List packages in a project
dotnet list [<PROJECT>] package 
