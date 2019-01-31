$labId = "8260108"
$rg = "WebAppslod8260108"
$container = "images"
$saName = "sa$($labId)"
$blobName="it-pro-challenges.gif"
$sourceUrl = "https://www.learnondemandsystems.com/wp-content/uploads/2018/05/it-pro-challenges.gif"
$key = (Get-AzureRmStorageAccountKey -ResourceGroupName $rg -Name $saName)[0].Value
$context = New-AzureStorageContext -StorageAccountName $saName -StorageAccountKey $key
New-AzureStorageContainer -Name $container -Permission Blob -Context $context
Start-AzureStorageBlobCopy -AbsoluteUri $sourceUrl -DestContainer $container -DestBlob $blobName -DestContext $context -Force