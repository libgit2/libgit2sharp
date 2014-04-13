function Get-VsFileSystem {
    $componentModel = Get-VSComponentModel
    $fileSystemProvider = $componentModel.GetService([NuGet.VisualStudio.IFileSystemProvider])
    $solutionManager = $componentModel.GetService([NuGet.VisualStudio.ISolutionManager])
    
    $fileSystem = $fileSystemProvider.GetFileSystem($solutionManager.SolutionDirectory)
    
    return $fileSystem
}

function Add-FilesToDirectory ($srcDirectory, $destDirectory) {
    ls $srcDirectory -Recurse -Filter *.dll  | %{
        $srcPath = $_.FullName

        $relativePath = $srcPath.Substring($srcDirectory.Length + 1)
        $destPath = Join-Path $destDirectory $relativePath
        
        $fileSystem = Get-VsFileSystem
        if (!(Test-Path $destPath)) {
            $fileStream = $null
            try {
                $fileStream = [System.IO.File]::OpenRead($_.FullName)
                $fileSystem.AddFile($destPath, $fileStream)
            } catch {
                # We don't want an exception to surface if we can't add the file for some reason
            } finally {
                if ($fileStream -ne $null) {
                    $fileStream.Dispose()
                }
            }
        }
    }
}

function Remove-FilesFromDirectory ($srcDirectory, $destDirectory) {
    $fileSystem = Get-VsFileSystem
    
    ls $srcDirectory -Recurse -Filter *.dll | %{
        $relativePath = $_.FullName.Substring($srcDirectory.Length + 1)
        $fileInBin = Join-Path $destDirectory $relativePath
        if ($fileSystem.FileExists($fileInBin) -and ((Get-Item $fileInBin).Length -eq $_.Length)) {
            # If a corresponding file exists in bin and has the exact file size as the one inside the package, it's most likely the same file.
            try {
                $fileSystem.DeleteFile($fileInBin)
            } catch {
                # We don't want an exception to surface if we can't delete the file
            }
        }
    }
}

function Get-ProjectRoot($project) {
    try {
        $project.Properties.Item("FullPath").Value
    } catch {

    }
}

function Get-ChildProjectItem($parent, $name) {
	try {
		return $parent.ProjectItems.Item($name);
	} catch {
	
	}
}

function Ensure-Folder($parent, $name) {
	$item = Get-ChildProjectItem $parent $name
	if(!$item) {
		$item = (Get-Interface $parent.ProjectItems "EnvDTE.ProjectItems").AddFolder($name)
	}
	return $item;
}

function Remove-Child($parent, $name) {
	$item = Get-ChildProjectItem $parent $name
	if($item) {
		(Get-Interface $item "EnvDTE.ProjectItem").Delete()
	}
}

function Remove-EmptyFolder($item) {
	if($item.ProjectItems.Count -eq 0) {
		(Get-Interface $item "EnvDTE.ProjectItem").Delete()
	}
}

function Add-ProjectItem($item, $src, $itemtype = "None") {
	$newitem = (Get-Interface $item.ProjectItems "EnvDTE.ProjectItems").AddFromFileCopy($src)
	$newitem.Properties.Item("ItemType").Value = $itemtype
}

Export-ModuleMember -function Add-FilesToDirectory, Remove-FilesFromDirectory, Get-ProjectRoot, Get-ChildProjectItem, Ensure-Folder, Remove-Child, Remove-EmptyFolder, Add-ProjectItem
