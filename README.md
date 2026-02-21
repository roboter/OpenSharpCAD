# OpenSharpCAD
Parametric CAD using C#

## Video
[![YouTube Demo](https://img.youtube.com/vi/dlQ3HhHRtSU/0.jpg)](https://youtu.be/dlQ3HhHRtSU)

## Description
OpenSharpCAD is a parametric CAD system that allows you to design 3D parts using C#. It is inspired by OpenSCAD but leverages the power and familiarity of the .NET ecosystem.

## Getting Started

### Prerequisites
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Installation
Clone the repository including all submodules:
```bash
git clone --recursive https://github.com/roboter/OpenSharpCAD.git
cd OpenSharpCAD
```

## Build Instructions

### Mac
Build and run using the .NET CLI:
```bash
# Build the project
dotnet build OpenSharpCAD/OpenSharpCAD.csproj

# Run the application
dotnet run --project OpenSharpCAD/OpenSharpCAD.csproj -f net10.0
```

### Windows
You can use either Visual Studio or the .NET CLI:

**Option 1: Visual Studio**
1. Open `OpenSharpCAD.sln` in Visual Studio 2022.
2. Set `OpenSharpCAD` as the Startup Project.
3. Press `F5` to build and run.

**Option 2: .NET CLI**
```powershell
# Build the project
dotnet build OpenSharpCAD/OpenSharpCAD.csproj

# Run the application
dotnet run --project OpenSharpCAD/OpenSharpCAD.csproj
```

## Links
- Original project from MatterHackers: [MatterCAD - Design Your 3D Parts In C#](https://www.matterhackers.com/news/mattercad-design-your-3d-parts-in-csharp)

## Example
```csharp
CsgObject bar = new Box(20, 5.8, 12, createCentered: false);
```

## Screenshot
![Screenshot](Screenshot%202018-03-30%2022.56.56.png)
