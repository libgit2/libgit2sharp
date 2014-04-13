param($installPath, $toolsPath, $package, $project)

Import-Module (Join-Path $toolsPath VS.psd1)
if ($project.Type -eq 'Web Site') {
    # TODO : To Implement and check if it works
    # $projectRoot = Get-ProjectRoot $project
    # if (!$projectRoot) {
    #    return;
    # }

    # $binDirectory = Join-Path $projectRoot "bin"
    # $libDirectory = Join-Path $installPath "lib\net35"
    # $nativeBinDirectory = Join-Path $installPath "NativeBinaries"

    # Remove-FilesFromDirectory $libDirectory $binDirectory
    # Remove-FilesFromDirectory $nativeBinDirectory $binDirectory
}
elseif($project.ExtenderNames -contains "WebApplication") {
	$depAsm = Get-ChildProjectItem $Project "_bin_deployableAssemblies";
	if($depAsm) {
	    $nativeBinaries = Ensure-Folder $depAsm "NativeBinaries";
	    if($nativeBinaries) {
		    $amd64 = Get-ChildProjectItem $nativeBinaries "amd64";
		    if($amd64) {
			    Remove-Child $amd64 "git2-65e9dc6.dll";
			    Remove-Child $amd64 "git2-65e9dc6.pdb";
			    Remove-EmptyFolder $amd64;
		    }
		    $x86 = Get-ChildProjectItem $nativeBinaries "x86";
		    if($x86) {
			    Remove-Child $x86 "git2-65e9dc6.dll";
			    Remove-Child $x86 "git2-65e9dc6.pdb";

			    Remove-EmptyFolder $x86;
		    }
        }
	}
	Remove-EmptyFolder $depAsm
}
else {
}
Remove-Module VS
