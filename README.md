#### _Attention: Please use this sample here: https://github.com/relativitydev/export-api-helper. This project is no longer being maintained._ 


# Object Manager - Export API Sample
Two .NET console apps demonstrating Export API consumption.

## Introduction
The Object Manager - Export API is designed for efficient extracted text export from Relativity. It is able to export the text regardless of the storage method (SQL Server or DataGrid). 

## Dependencies
All of the DLLs you need should be in the `dlls` folder at the root. As versions update, however, we recommend using the libraries from your SDK. 

## Using the Helper
Using the `Relativity.ObjectManager.ExportApiHelper` library is the recommended way to consume the Export API. It wraps a lot of the complexity that comes with managing threads and creating/disposing services. Currently, this sample does _not_ actually write the text to the file system. To run the sample, make sure you input your credentials in the app.config file. Here is an example:

```XML
<appSettings>
  <add key="RelativityBaseURI" value="https://instance-name" />
  <add key="RelativityUserName" value="bob.da.builder@relativity.com" />
  <add key="RelativityPassword" value="YesWeCan!123" />
  <add key="WorkspaceId" value="1017295"/>
</appSettings>
```

## ExportAPISample
This console app consumes the Export API at a lower level (without the above helper) and _does_ write the text to .txt files. The sample spins up threads that are dedicated to streaming the text to a file. If the extracted text is empty or does not exist, then a file is not created, but the document identifier is noted in the created load file. Use the below app.config example as a template:

```XML
<appSettings>
  <add key="RelativityBaseURI" value="http://relativitydevvm.mshome.net/Relativity.REST/api" />
  <add key="RelativityUserName" value="relativity.admin@relativity.com" />
  <add key="RelativityPassword" value="" />
  <add key="WorkspaceId" value="1017273"/>
  <add key="ExportFolder" value="C:\Data\ExportApi"/>
  <add key="ThreadCount" value="4"/>
</appSettings>
```
