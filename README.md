# file-generator
Test-file generator using .NET Core

Helpful links for generating the publish as an executable instead of a DLL:
https://blog.jongallant.com/2017/09/dotnet-core-console-app-create-exe-instead-of-dll/
https://docs.microsoft.com/en-us/dotnet/core/rid-catalog#using-rids

Commandline to generate a publish as a windows 10 compatible executable:
dotnet publish -c "Release" -r win10-x64

For other platforms, switch out the win10-x64 with your desired platform. See the microsoft documentation for examples.