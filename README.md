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
            2. dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore // this should be added to the DataAccess Project
            3. In our DataAccess Project and class inherit from IdentityDbContext instead of just DbContext
            4. Now in OnModelCreating method add this line as the first line.
               "base OnModelCreating(ModelBuilder)"
            5. ADD THE BELOW TO THE MAIN PROJECT....
            6. dotnet add package Microsoft.Extensions.Identity.Stores --version 8.0.0
            7. dotnet add package Microsoft.AspNetCore.Identity.UI --version 8.0.0
            8. dotnet add package Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore
            9. dotnet add package Microsoft.EntityFrameworkCore.Design
            10. dotnet add package Microsoft.VisualStudio.Web.CodeGeneration.Design
            11. dotnet add package Microsoft.EntityFrameworkCore.SqlServer
            12. dotnet add package Microsoft.EntityFrameworkCore.Tools
            13. run command "dotnet aspnet-codegenerator identity -h"  // see scaffolding options
            14. run "dotnet aspnet-codegenerator identity --useDefaultUI"  // implement basic setup. Also see scaffolding options for other scenarios  
            15. I use "dotnet aspnet-codegenerator identity"  to install everything
            16. Note the generator will try to put in program.cs its own DBContext. Delete it and adjust this line to use our own existing context which we adjusted above to be IdentityDbContext.  This is the line to adjust in program.cs (builder.Services.AddDefaultIdentity.....)  
            17. We can optionally add <IdentityUser> to our  public class ApplicationDbContext : IdentityDbContext<IdentityUser> like that. TOTALLY OPTIONAL
            18. Add app.UseAuthentication() to program.cs. It must be added right before app.UseAuthorization()
            19. In appSettings.json Identity scaffolding tried to add a new connection.  We dont need it! Delete it!!!
            20. Now back in program.cs add two things
                1.  builder.Services.AddRazorPages();
                2.  app.MapRazorPages();
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
dotnet add package Microsoft.AspNetCore.Mvc.ViewFeatures // for things like SelectListItem 
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

# Scaffold an area
*** MUST BE IN PROJECT DIRECTORY NOT SOLUTION/ROOT DIRECTORY ***
dotnet aspnet-codegenerator area [<AreaNameToGenerate>]
A suggest will be made in recofiguring (adding a change in code) to the routing
in Program.cs. It is straight forward and it is just now to include the concept of areas
in the routing. Now we have areas, controller, action that specifies how a request is routed.
Change
```
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
```
to
```
app.MapControllerRoute(
    name: "default",
    pattern: "{area=Customer}/{controller=Home}/{action=Index}/{id?}");
```
Where "Customer" is the area that will be used by default. It could be any other one of our scaffoled areas.
- Move Controllers and Views to their areas, remembering to update the namespace as neccessary.
eg. [<project>].Controllers now becomes [<project>].Areas.[<AreaNameYouChose>].Controllers
Now annotate your controllers etc. to indicate which Area a controller for example belongs too.
eg. above the say CategoryController class in the Admin area annote it with [Area("Admin)]
 - NOW move the views that correspond to each area inside their respective view area.
Additionally must COPY "_ViewImports.cshtml" and "_ViewStart.cshtml" to the "Views" folder of each Area
- Now go back to your views and update the links where you have asp-controller, asp-action, and now also add asp-area and set this to appropriate area. Even the shared views like "_Layout.cshtml" must be updated

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
