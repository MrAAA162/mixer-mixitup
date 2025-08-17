# .NET 8.0 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that an .NET 8.0 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 8.0 upgrade.
3. Upgrade MixItUp.Installer\MixItUp.Installer.csproj
4. Upgrade MixItUp.Reporter\MixItUp.Reporter.csproj
5. Upgrade MixItUp.Uninstaller\MixItUp.Uninstaller.csproj
6. Upgrade MixItUp.Base\MixItUp.Base.csproj
7. Upgrade MixItUp.SignalR.Client\MixItUp.SignalR.Client.csproj

## Settings

### Excluded projects

| Project name                                   | Description                 |
|:-----------------------------------------------|:---------------------------:|
| MixItUp.WPF\MixItUp.WPF.csproj                 | Explicitly excluded         |

### Aggregate NuGet packages modifications across all projects

| Package Name                        | Current Version | New Version | Description                         |
|:------------------------------------|:---------------:|:-----------:|:------------------------------------|
| EntityFramework                      |   6.4.4         |  6.5.1      | Deprecated, security vulnerability  |
| Microsoft.ApplicationInsights        |   2.20.0        |  2.23.0     | Deprecated                          |
| Microsoft.AspNetCore.Connections.Abstractions | 6.0.3 | 8.0.19 | Recommended for .NET 8.0            |
| Microsoft.AspNetCore.Http.Connections.Client | 6.0.3 | 8.0.19 | Recommended for .NET 8.0            |
| Microsoft.AspNetCore.Http.Connections.Common | 6.0.3 | 8.0.19 | Recommended for .NET 8.0            |
| Microsoft.AspNetCore.SignalR.Client  |   6.0.3         |  8.0.19     | Recommended for .NET 8.0            |
| Microsoft.AspNetCore.SignalR.Client.Core | 6.0.3 | 8.0.19 | Recommended for .NET 8.0            |
| Microsoft.AspNetCore.SignalR.Common  |   6.0.3         |  8.0.19     | Recommended for .NET 8.0            |
| Microsoft.AspNetCore.SignalR.Protocols.Json | 6.0.3 | 8.0.19 | Recommended for .NET 8.0            |
| Microsoft.Bcl.AsyncInterfaces        |   6.0.0         |  8.0.0      | Recommended for .NET 8.0            |
| Microsoft.Data.Sqlite                |   6.0.3         |  8.0.19     | Recommended for .NET 8.0            |
| Microsoft.Data.Sqlite.Core           |   6.0.3         |  8.0.19     | Recommended for .NET 8.0            |
| Microsoft.Extensions.Configuration   |   6.0.1         |  8.0.0      | Recommended for .NET 8.0            |
| Microsoft.Extensions.Configuration.Abstractions | 6.0.0 | 8.0.0 | Recommended for .NET 8.0            |
| Microsoft.Extensions.Configuration.Binder | 6.0.0 | 8.0.2 | Recommended for .NET 8.0            |
| Microsoft.Extensions.DependencyInjection | 6.0.0 | 8.0.1 | Recommended for .NET 8.0            |
| Microsoft.Extensions.DependencyInjection.Abstractions | 6.0.0 | 8.0.2 | Recommended for .NET 8.0            |
| Microsoft.Extensions.Features        |   6.0.3         |  8.0.19     | Recommended for .NET 8.0            |
| Microsoft.Extensions.Logging         |   6.0.0         |  8.0.1      | Recommended for .NET 8.0            |
| Microsoft.Extensions.Logging.Abstractions | 6.0.1 | 8.0.3 | Recommended for .NET 8.0            |
| Microsoft.Extensions.Options         |   6.0.0         |  8.0.2      | Recommended for .NET 8.0            |
| Microsoft.Extensions.Primitives      |   6.0.0         |  8.0.0      | Recommended for .NET 8.0            |
| System.IO.Ports                      |   6.0.0         |  8.0.0      | Recommended for .NET 8.0            |
| System.Speech                        |   7.0.0         |  8.0.0      | Recommended for .NET 8.0            |

### Project upgrade details

#### MixItUp.Installer\MixItUp.Installer.csproj modifications

Project properties changes:
  - Target framework should be changed from `.NETFramework,Version=v4.6.1` to `net8.0-windows`

Feature upgrades:
  - Convert project file to SDK-style.

Other changes:
  - Address all breaking changes and incompatibilities for .NET 8.0.

#### MixItUp.Reporter\MixItUp.Reporter.csproj modifications

Project properties changes:
  - Target framework should be changed from `.NETFramework,Version=v4.6.1` to `net8.0-windows`

Feature upgrades:
  - Convert project file to SDK-style.

Other changes:
  - Address all breaking changes and incompatibilities for .NET 8.0.

#### MixItUp.Uninstaller\MixItUp.Uninstaller.csproj modifications

Project properties changes:
  - Target framework should be changed from `.NETFramework,Version=v4.6.1` to `net8.0-windows`

Feature upgrades:
  - Convert project file to SDK-style.

Other changes:
  - Address all breaking changes and incompatibilities for .NET 8.0.

#### MixItUp.Base\MixItUp.Base.csproj modifications

NuGet packages changes:
  - System.IO.Ports should be updated from `6.0.0` to `8.0.0` (*recommended for .NET 8.0*)

Other changes:
  - Address all breaking changes and incompatibilities for .NET 8.0.

#### MixItUp.SignalR.Client\MixItUp.SignalR.Client.csproj modifications

NuGet packages changes:
  - Microsoft.AspNetCore.SignalR.Client should be updated from `6.0.3` to `8.0.19` (*recommended for .NET 8.0*)

Other changes:
  - Address all breaking changes and incompatibilities for .NET 8.0.
