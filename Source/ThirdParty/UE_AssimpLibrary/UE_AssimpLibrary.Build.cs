// Fill out your copyright notice in the Description page of Project Settings.

using System.IO;
using System.Diagnostics;
using UnrealBuildTool;

public class UE_AssimpLibrary : ModuleRules
{

	public string BinFolder(ReadOnlyTargetRules Target)
	{
		if(Target.Platform == UnrealTargetPlatform.Mac)
			return Path.GetFullPath(Path.Combine(ModuleDirectory, "../../../Binaries/Mac/"));
		else if(Target.Platform == UnrealTargetPlatform.IOS)
			return Path.GetFullPath(Path.Combine(ModuleDirectory, "../../../Binaries/IOS/"));
		if(Target.Platform == UnrealTargetPlatform.Win64)
			return Path.GetFullPath(Path.Combine(ModuleDirectory, "../../../Binaries/Win64/"));
		if(Target.Platform == UnrealTargetPlatform.Android)
			return Path.GetFullPath(Path.Combine(ModuleDirectory, "../../../Binaries/Android/"));
		if(Target.Platform == UnrealTargetPlatform.Linux)
			return Path.GetFullPath(Path.Combine(ModuleDirectory, "../../../Binaries/Linux/"));
		return "";
	}
	
  private void InvokeCMake(string args)
  {
    var cmakeExe = System.Environment.GetEnvironmentVariable("CMAKE_EXE_PATH");
    if (cmakeExe == null) {
      if(Target.Platform == UnrealTargetPlatform.Win64) {
        cmakeExe = @"C:\\Program Files\\CMake\\bin\\cmake.exe";
      }
      else {
        cmakeExe = @"/usr/bin/cmake";
      }
    }

    ProcessStartInfo startInfo = new ProcessStartInfo();
    startInfo.FileName = cmakeExe;
    startInfo.Arguments = args;
    startInfo.UseShellExecute = false;
    startInfo.RedirectStandardOutput = false;
    startInfo.RedirectStandardError = false;
    startInfo.WorkingDirectory = Path.GetFullPath(Path.Combine(ModuleDirectory, "assimp"));

    using (Process process = Process.Start(startInfo)) {
      process.WaitForExit();
    }
  }

  public void BuildAssimpLibrary()
  {
    System.Console.Write("Configuring...");
    InvokeCMake(@"-G Ninja -DCMAKE_BUILD_TYPE=Release CMakeLists.txt -DLIBRARY_SUFFIX:STRING=");

    System.Console.Write("Building...");
    InvokeCMake(@"--build .");
  }

	public UE_AssimpLibrary(ReadOnlyTargetRules Target) : base(Target)
  {
    Type = ModuleType.External;

    BuildAssimpLibrary();
    string BinaryFolder = BinFolder(Target);
    PublicIncludePaths.Add(Path.Combine(ModuleDirectory,"assimp" , "include"));
    if (Target.Platform == UnrealTargetPlatform.Win64)
    {
      // Add the import library
      PublicAdditionalLibraries.Add(Path.Combine(ModuleDirectory,"assimp" ,"lib", "assimp.lib"));
      //RuntimeDependencies.Add(Path.Combine(ModuleDirectory,"assimp" , "bin","Release","assimp.dll"));

      // Delay-load the DLL, so we can load it from the right place first
      PublicDelayLoadDLLs.Add(Path.Combine(ModuleDirectory,"assimp" , "bin", "assimp.dll"));

      Directory.CreateDirectory(BinaryFolder);
      string AssimpDll = Path.Combine(ModuleDirectory, "assimp", "bin", "assimp.dll");
      string BinPath =Path.Combine(ModuleDirectory, BinaryFolder, "assimp.dll");

      CopyFile(AssimpDll, BinPath);
    }
    else if (Target.Platform == UnrealTargetPlatform.Mac)
    {
      // Add the import library
      PublicAdditionalLibraries.Add(Path.Combine(ModuleDirectory,"assimp" ,"bin", "libassimp.dylib"));

      // Ensure that the DLL is staged along with the executable
      RuntimeDependencies.Add(Path.Combine(ModuleDirectory,"assimp", "bin", "libassimp.5.dylib"));
      AdjustSymlinks("dylib");

      Directory.CreateDirectory(BinaryFolder);
      string AssimpDylib = Path.Combine(ModuleDirectory, "assimp", "bin", "libassimp.dylib");
      string BinPath =Path.Combine(ModuleDirectory, BinaryFolder, "libassimp.dylib");

      CopyFile(AssimpDylib, BinPath);
    }
    else if (Target.Platform == UnrealTargetPlatform.Linux)
    {
      // Add the import library
      PublicAdditionalLibraries.Add(Path.Combine(ModuleDirectory,"assimp" ,"bin", "libassimp.so"));

      // Ensure that the DLL is staged along with the executable
      RuntimeDependencies.Add(Path.Combine(ModuleDirectory, "assimp", "bin", "libassimp.so.5"));
      AdjustSymlinks("so.5*");

      Directory.CreateDirectory(BinaryFolder);
      string AssimpSo = Path.Combine(ModuleDirectory, "assimp", "bin", "libassimp.so");
      string BinPath = Path.Combine(ModuleDirectory, BinaryFolder, "libassimp.so");

      CopyFile(AssimpSo, BinPath);
    }
  }

	public void CopyFile(string Source, string Dest)
	{
		System.Console.WriteLine("Copying {0} to {1}", Source, Dest);
		if (System.IO.File.Exists(Dest))
		{
			System.IO.File.SetAttributes(Dest, System.IO.File.GetAttributes(Dest) & ~System.IO.FileAttributes.ReadOnly);
		}
		try
		{
			//Make Folder
		
			System.IO.File.Copy(Source, Dest, true);
		}
		catch (System.Exception ex)
		{
			System.Console.WriteLine("Failed to copy file: {0}", ex.Message);
		}
	}

  // Unfortunately we need to convert symlinks to actual copies because Unreal doesn't follow symlinks
  // when packaging a game which results in errors like "Size mismatch ...".
  public void AdjustSymlinks(string ext)
  {
      var libassimpBinFolder = Path.Combine(ModuleDirectory, "assimp", "bin");
      var libassimpFiles = Directory.GetFiles(libassimpBinFolder, "libassimp*." + ext);
      foreach (var filePath in libassimpFiles)
      {
        var fileInfo = Directory.ResolveLinkTarget(filePath, true);
        if (fileInfo == null) {
          continue;
        }

        // This is a symlink; convert to an actual copy of the original file
        string fileName = filePath.Substring(libassimpBinFolder.Length + 1);
        File.Delete(filePath);
        File.Copy(fileInfo.ToString(), filePath, true);
      }
  }
}
