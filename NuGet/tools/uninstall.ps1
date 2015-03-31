param($installPath, $toolsPath, $package, $project)

function UpdateFodyConfig($package, $project)
{
    $addinName = $package.Id.Replace(".Fody", "")
    $fodyWeaversPath = [System.IO.Path]::Combine([System.IO.Path]::GetDirectoryName($project.FullName), "FodyWeavers.xml")

    if (!(Test-Path ($fodyWeaversPath)))
    {
        return
    }

    $xml = [xml]$(Get-Content $fodyWeaversPath)

    $weavers = $xml["Weavers"]
    $node = $weavers.SelectSingleNode($addinName)

    if ($node)
    {
        Write-Host "Removing node from FodyWeavers.xml"
        $weavers.RemoveChild($node)
    }

    $xml.Save($fodyWeaversPath)
}

function UnlockWeaversXml($project)
{
    $fodyWeaversProjectItem = $project.ProjectItems.Item("FodyWeavers.xml");
    if ($fodyWeaversProjectItem)
    {
        $fodyWeaversProjectItem.Open("{7651A701-06E5-11D1-8EBD-00A0C90F26EA}")
        $fodyWeaversProjectItem.Save()
	      $fodyWeaversProjectItem.Document.Close()
    }
}

UnlockWeaversXml -project $project
UpdateFodyConfig -package $package -project $project
