# ReportServer
This project implements plugin architecture for report generation where each report template is a plugin (dll). This gives the flexibility to add, edit, remove report templates without touching the application code.

The application is designed to run in windows application tray and poll for incoming report request json files. The polling frequency is configurable. This application can easily be modified as a windows service, worker service etc...

#Architecture
The application loads libraries dynamically from a predefined plugin location (configurable via settings). Monitors a predefined location for incoming request json files with a predefined schema. 
### Basic Json Schema Expected
```json
{
    "TemplateName":"Temaplate A",
    "ReportData":{}
}
```
The `TemplateName` is very important as it is used to dynamically select a report plugin which implements the `IExtensibility` interface. After selecting the report plugin, the application hands over the whole json payload to the dynamically loaded library. The report template library can then parse the json and either
 - directly generate a report based on template and json data.
 - fetch data from a database by using the json properties as parameters.

The plugin library has the filexibility to use 
- which ever reports it needs to use (Uses crystal reports to fetech data in the sample plugins projects, ReportA, ReportB and ReportC)
- fetch data which ever way it needs (Can use dapper/EF, etc... fetech data.)

![Report Server Architecture code map image](https://github.com/SwatInc/ReportServer/blob/master/ReportServerArchitecture.png)
