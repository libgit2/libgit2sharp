param($installPath, $toolsPath, $package, $project)

Import-Module (Join-Path $toolsPath VS.psd1)
$nativeBinDirectory = Join-Path $installPath "NativeBinaries"

if ($project.Type -eq 'Web Site') {
    # TODO : To Implement and check if it works
    # $projectRoot = Get-ProjectRoot $project
    # if (!$projectRoot) {
    #     return;
    # }

    # $binDirectory = Join-Path $projectRoot "bin"
    # $libDirectory = Join-Path $installPath "lib\net35"
    # Add-FilesToDirectory $libDirectory $binDirectory
    # Add-FilesToDirectory $nativeBinDirectory $binDirectory
}
elseif($project.ExtenderNames -contains "WebApplication") {
	$depAsm = Ensure-Folder $Project "_bin_deployableAssemblies";
	if($depAsm) {
	    $nativeBinaries = Ensure-Folder $depAsm "NativeBinaries";
	    if($nativeBinaries) {
		    $amd64 = Ensure-Folder $nativeBinaries "amd64";
		    if($amd64) {
			    $amd64dir = (Join-Path $nativeBinDirectory "amd64")		
			    Add-ProjectItem $amd64 (Join-Path $amd64dir "git2-65e9dc6.dll");
			    Add-ProjectItem $amd64 (Join-Path $amd64dir "git2-65e9dc6.pdb");
		    }
		    $x86 = Ensure-Folder $nativeBinaries "x86";
		    if($x86) {
			    $x86dir = (Join-Path $nativeBinDirectory "x86")
			    Add-ProjectItem $x86 (Join-Path $x86dir "git2-65e9dc6.dll");
			    Add-ProjectItem $x86 (Join-Path $x86dir "git2-65e9dc6.pdb");
		    }
        }
	}
}
Remove-Module VS
