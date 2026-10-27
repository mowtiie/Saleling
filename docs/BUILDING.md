# Building the .exe
Run this from the project folder:

```powershell
.\build-package.ps1
```

It builds everything from scratch and creates `publish\Saleling-v<version>.zip` (the `publish` folder is not committed to Git). The zip contains:

* `Saleling.UI.exe`, a single exe that runs without installing .NET.
* `Saleling.UI.dll.config`, the database settings. Keep it next to the exe.
* `Database\Saleling.DB.dacpac`, the database.
* `SETUP.txt`, step-by-step instructions for whoever installs it.

On the other computer, deploy the database from SSMS (right-click **Databases**, then **Deploy Data-tier Application**), set `DatabaseProfile` in the config file, and run the exe. The database is not inside the exe, so that computer still needs its own SQL Server.

The script needs the .NET 8 SDK and Visual Studio (with the Data storage and processing workload).

The app version is set in `Saleling.UI/Saleling.UI.csproj` (`<Version>`). It shows on the login screen and in the zip's file name.
