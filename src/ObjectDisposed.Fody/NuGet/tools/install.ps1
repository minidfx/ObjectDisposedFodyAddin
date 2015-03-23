param($installPath, $toolsPath, $package, $project)

function UpdateFodyConfig($package, $project)
{
	Write-Host "Update-FodyConfig"
	$addinName = $package.Id.Replace(".Fody", "")
  $fodyWeaversFolder = Split-Path $project.FullName
  $fodyWeaversPath = Join-Path $fodyWeaversFolder "FodyWeavers.xml"

  $xml = [xml]$(Get-Content $fodyWeaversPath)

  $weavers = $xml["Weavers"]
  $node = $weavers.SelectSingleNode($addinName)

  if (-not $node)
  {
    Write-Host "Appending node"
    $newNode = $xml.CreateElement($addinName)
    $weavers.AppendChild($newNode)
  }

  $xml.Save($fodyWeaversPath)
}

function Fix-ReferencesCopyLocal($package, $project)
{
  Write-Host "Fix-ReferencesCopyLocal $($package.Id)"
  $asms = $package.AssemblyReferences | %{$_.Name}

  foreach ($reference in $project.Object.References)
  {
    if ($asms -contains $reference.Name + ".dll")
    {
      if($reference.CopyLocal -eq $true)
      {
        $reference.CopyLocal = $false;
      }
    }
  }
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
Fix-ReferencesCopyLocal -package $package -project $project
