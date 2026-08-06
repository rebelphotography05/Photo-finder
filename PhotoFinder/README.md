# Photo Finder

This is a simple Windows Photo Finder application.

What it does
- Reads a text file containing photo names separated by commas (e.g. photo1, photo2, another-photo)
- Searches a selected folder (recursively) for image files with matching file names (matches by filename without extension, case-insensitive)
- Shows matching files, allows you to open a selected photo or copy all matches to a folder on your Desktop

How this repo is delivered
- I added a GitHub Actions workflow that builds a self-contained single-file Windows x64 executable when code is pushed.
- After the workflow completes you can download the built .exe from the Actions run artifacts.

How to build locally (if you want)
- Install .NET 7 SDK on Windows: https://dotnet.microsoft.com/en-us/download
- Open a command prompt in the repository root and run:
  dotnet publish PhotoFinder -c Release -r win-x64 /p:PublishSingleFile=true /p:PublishTrimmed=true /p:SelfContained=true -o ./publish
- The executable will be in publish\ folder.

Usage
- Open the exe, choose a folder to search and a text file with comma-separated names, then click "Find Photos".