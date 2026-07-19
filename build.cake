///////////////////////////////////////////////////////////////////////////////
// ADDINS
///////////////////////////////////////////////////////////////////////////////

#addin nuget:?package=Pragmatic.CakeCI&version=1.0.0

///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var cakeMixFile = CiArgument("cakemix", "build.cakemix");
var target = CiArgument("target", "Default");
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

Task("__LintCheck")
	.Does(() => CiLint());

Task("__ValidateSonarArgs")
	.Does(() => sonarArgs.Validate());

Task("__ValidateNugetArgs")
	.Does(() => nugetArgs.Validate());

Task("__ValidateDockerArgs")
	.Does(() => containerArgs.Validate());

Task("__BeginSonarScan")
	.IsDependentOn("__ValidateSonarArgs")
	.Does(() => CiSonarScannerBegin(sonarArgs, artifactsFolder));

///////////////////////////////////////////////////////////////////////////////
// Public Tasks
///////////////////////////////////////////////////////////////////////////////
Task("BuildAndTest")
	.Does(() => CiTest());

Task("BuildAndBenchmark")
	.Does(() => CiBenchmark());

Task("BuildAndSonarScan")
	.IsDependentOn("__LintCheck")
	.IsDependentOn("__BeginSonarScan")
	.Does(() =>
	{
		try
		{
			CiTest();
			CiBenchmark();
		}
		finally
		{
			CiSonarScannerEnd(sonarArgs);
		}
	});

Task("NugetPackAndPush")
	.IsDependentOn("__LintCheck")
	.IsDependentOn("__ValidateNugetArgs")
	.IsDependentOn("__Version")
	.IsDependentOn("__BeginSonarScan")
	.Does(() =>
	{
		try
		{
			CiTest();
			CiBenchmark();
			CiNugetPack(buildManifest, packagesFolder, versionNumber);
		}
		finally
		{
			CiSonarScannerEnd(sonarArgs);
		}

		CiNugetPush(nugetArgs, packagesFolder);
	});

Task("DockerPackAndPush")
	.IsDependentOn("__LintCheck")
	.IsDependentOn("__ValidateDockerArgs")
	.IsDependentOn("__Version")
	.IsDependentOn("__BeginSonarScan")
	.Does(() =>
	{
		try
		{
			CiTest();
			CiBenchmark();
			CiDockerBuild(buildManifest, containerArgs, versionNumber);
		}
		finally
		{
			CiSonarScannerEnd(sonarArgs);
		}

		try
		{
			CiDockerLogin(containerArgs);
			CiDockerPush(buildManifest, containerArgs, versionNumber);
		}
		finally
		{
			CiDockerLogout(containerArgs);
		}
	});

Task("FullPackAndPush")
	.IsDependentOn("__LintCheck")
	.IsDependentOn("__ValidateNugetArgs")
	.IsDependentOn("__ValidateDockerArgs")
	.IsDependentOn("__Version")
	.IsDependentOn("__BeginSonarScan")
	.Does(() =>
	{
		try
		{
			CiTest();
			CiBenchmark();
			CiNugetPack(buildManifest, packagesFolder, versionNumber);
			CiDockerBuild(buildManifest, containerArgs, versionNumber);
		}
		finally
		{
			CiSonarScannerEnd(sonarArgs);
		}
		
		try
		{
			CiDockerLogin(containerArgs);
			CiDockerPush(buildManifest, containerArgs, versionNumber);
			CiNugetPush(nugetArgs, packagesFolder);
		}
		finally
		{
			CiDockerLogout(containerArgs);
		}
	});

Task("Default")
	.IsDependentOn("__LintCheck")
	.Does(() =>
	{
		CiTest();
		CiBenchmark();
	});

RunTarget(target);