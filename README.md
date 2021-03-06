# KNo2.ApiTestClient
Kno2 API Test client for integrators

## How to build ##
The included build system requires a windows machine running Powershell v3 or
later.

The build bootstraps the psake build tool, downloads nuget.exe and retrieves the
necessary nuget packages for the solution.  After the build is complete the build
script will merge the the assemblies (ilmerge ) into the standard artifacts
folder as a single executable.

After you have cloned the repository open a powershell prompt and cd to path/to/your/repo.

As a prerequisite you will need a key (client id) a secret (client 
secret) and have your machines public ip address whitelisted in by your Kno2 admin in the organization settings.

You will need the following values for the build command:

- **your\_base\_uri** such as http:\\example.kno2-stage.com
- **your\_client\_id**
- **your\_client\_secret**

Using these values execute the following

    .\build\run.ps1 -baseUri your_base_uri -ClientId your_client_id -ClientSecret your_client_secret

You can also add an optional switch to set the Direct Message Domain which is used to verify direct messages.  If you don't pass this the test client will assume one such as example.direct.kno2-stage.com

    .\build\run.ps1 -baseUri your_base_uri -ClientId your_client_id -ClientSecret your_client_secret -DirectMessageDomain your_direct_address_domain

The test client is going pass a http header to help in tracking your usage in http debugging tools.


## Running the client ##

The build output drops the application to the standard path/to/your/repo/artifacts folder where you can simply call the console application from the command line.


This header is formatted as follows:

**emr-session**: **your\_machine\_name\-semver\-githash**

as an example:

**emr-session**: **developer01-0.0.2-1egae10**

The test client will write out various messages to the console window with error message showing in red.

