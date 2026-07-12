///////////////////////////////////////////////////////////////////////////////
// ADDINS
///////////////////////////////////////////////////////////////////////////////

#addin nuget:?package=Pragmatic.CakeCI&version=0.1.22

///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var cakeMixFile = CiArgument("cakemix", "build.cakemix");
var target = CiArgument("target", "Default");
var configuration = CiArgument("configuration", "Release");
var versionNumber = CiArgument("VersionOverride");

BuildManifest buildManifest;

var nugetArgs = new NugetArgs
{
    Source = CiArgument("NugetSource"),
    ApiKey = CiArgument("NugetApiKey"),
};
var containerArgs = new ContainerArgs
{
    Registry = CiArgument("ContainerRegistry"),
    Token = CiArgument("ContainerRegistryToken"),
    UserName = CiArgument("ContainerRegistryUserName")
};
var sonarArgs = new SonarArgs
{
    Org = CiArgument("SonarOrg"),
    Token = CiArgument("SonarToken"),
    ProjectKey = CiArgument("SonarProjectKey"),
    ProjectName = CiArgument("SonarProjectName"),
	Branch = CiArgument("SonarBranch"),
    HostUrl = CiArgument("SonarHostUrl", "http://localhost:9000")
};

// Artifact Folders
var artifactsFolder = "./artifacts";
var packagesFolder = System.IO.Path.Combine(artifactsFolder, "packages");
var swaggerFolder = System.IO.Path.Combine(artifactsFolder, "swagger");
var postmanFolder = System.IO.Path.Combine(artifactsFolder, "postman");

// Setup / Teardown
///////////////////////////////////////////////////////////////////////////////
Setup(context =>
{
	buildManifest = LoadBuildManifest(cakeMixFile);

	// Clean artifacts
	if (System.IO.Directory.Exists(artifactsFolder))
		System.IO.Directory.Delete(artifactsFolder, true);
});

Teardown(context =>
{

});

///////////////////////////////////////////////////////////////////////////////
// Tasks
///////////////////////////////////////////////////////////////////////////////
Task("__Version")
	.Does(() => versionNumber = CiVersion(versionNumber));

Task("BuildAndTest")
	.Does(() => CiTest());

Task("BuildAndBenchmark")
	.Does(() => CiBenchmark());

Task("BuildAndSonarScan")
	.Does(() =>
	{
		sonarArgs.Validate();
		CiLint();
		CiSonarScannerBegin(sonarArgs, artifactsFolder);
		CiTest();
		CiBenchmark();
		CiSonarScannerEnd(sonarArgs);
	});

Task("NugetPackAndPush")
	.Does(() =>
	{
		nugetArgs.Validate();
		CiLint();
		versionNumber = CiVersion(versionNumber);
		CiTest();
		CiBenchmark();
		CiNugetPack(buildManifest, packagesFolder, versionNumber);
		CiNugetPush(nugetArgs, packagesFolder);
	});

Task("LocalNugetPackAndPush")
	.Does(() =>
	{		
		nugetArgs.Validate();
		versionNumber = CiVersion(versionNumber);
		CiTest();
		CiBenchmark();
		CiNugetPack(buildManifest, packagesFolder, versionNumber);
		CiNugetPush(nugetArgs, packagesFolder);
	});

Task("DockerPackAndPush")
	.Does(() =>
	{
		containerArgs.Validate();
		versionNumber = CiVersion(versionNumber);
		CiLint();
		CiTest();
		CiBenchmark();
		CiDockerLogin(containerArgs);
		CiDockerBuild(buildManifest, containerArgs, versionNumber);
		CiDockerPush(buildManifest, containerArgs, versionNumber);
	});

Task("FullPackAndPush")
	.Does(() =>
	{
		nugetArgs.Validate();
		containerArgs.Validate();
		sonarArgs.Validate();
		CiLint();
		versionNumber = CiVersion(versionNumber);
		CiSonarScannerBegin(sonarArgs, artifactsFolder);
		CiTest();
		CiBenchmark();
		CiNugetPack(buildManifest, packagesFolder, versionNumber);
		CiDockerLogin(containerArgs);
		CiDockerBuild(buildManifest, containerArgs, versionNumber);
		CiSonarScannerEnd(sonarArgs);
		CiNugetPush(nugetArgs, packagesFolder);
		CiDockerPush(buildManifest, containerArgs, versionNumber);
	});

Task("Default")
	.Does(() =>
	{
		CiLint();
		CiTest();
		CiBenchmark();
	});

RunTarget(target);
