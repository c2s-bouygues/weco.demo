# Définition de variables pour instancier le conteneur
$ACI_RESOURCE_GROUP = "my-rg-name"

$ACI_STORAGE_ACCOUNT_NAME = "mystorageaccountname"
$ACI_SHARE_NAME = "locust"
$ACI_SHARE_LOCATION = "/home/locust/"

$ACI_IMAGE_NAME = "locustio/locust"

$ACI_WORKERS_COUNT = 4;
$ACI_MASTER_NAME = $ACI_RESOURCE_GROUP + "-container-locust-master"
$ACI_MASTER_COMMAND = "locust --locustfile /home/locust/load-front-api.py --master --expect-workers=$ACI_WORKERS_COUNT --csv=master-results"

# Récupération de la clé de stockage
$STORAGE_KEY = $(az storage account keys list --resource-group $ACI_RESOURCE_GROUP --account-name $ACI_STORAGE_ACCOUNT_NAME --query "[0].value" --output tsv)

# Création du conteneur docker locust master
az container create `
    --resource-group $ACI_RESOURCE_GROUP `
    --name $ACI_MASTER_NAME `
    --dns-name-label $ACI_MASTER_NAME `
    --image $ACI_IMAGE_NAME `
    --protocol TCP `
    --ports 5557 8089 `
    --azure-file-volume-account-name $ACI_STORAGE_ACCOUNT_NAME `
    --azure-file-volume-account-key $STORAGE_KEY `
    --azure-file-volume-share-name $ACI_SHARE_NAME `
    --azure-file-volume-mount-path $ACI_SHARE_LOCATION `
    --command-line $ACI_MASTER_COMMAND `
    --os-type Linux `
    --restart-policy OnFailure `
    --ip-address Public

$ACI_MASTER_IP = az container show --resource-group $ACI_RESOURCE_GROUP --name $ACI_MASTER_NAME --query 'ipAddress.ip'
$ACI_MASTER_IP = $ACI_MASTER_IP.Substring(1).Substring(0, $ACI_MASTER_IP.Length - 2)
$ACI_WORKER_NAME_TEMPLATE = $ACI_RESOURCE_GROUP + "-container-locust-worker-"
$ACI_WORKER_COMMAND = "locust --locustfile /home/locust/load-front-api.py --worker --master-host=$ACI_MASTER_IP"

For ($i = 0; $i -lt $ACI_WORKERS_COUNT; $i++) { 
    $ACI_WORKER_NAME = $ACI_WORKER_NAME_TEMPLATE + $("{0:d2}" -f ($i + 1))
    # Création du conteneur docker locust worker 01
    az container create `
        --resource-group $ACI_RESOURCE_GROUP `
        --name $ACI_WORKER_NAME `
        --dns-name-label $ACI_WORKER_NAME `
        --image $ACI_IMAGE_NAME `
        --protocol TCP `
        --ports 5557 8089 `
        --azure-file-volume-account-name $ACI_STORAGE_ACCOUNT_NAME `
        --azure-file-volume-account-key $STORAGE_KEY `
        --azure-file-volume-share-name $ACI_SHARE_NAME `
        --azure-file-volume-mount-path $ACI_SHARE_LOCATION `
        --command-line $ACI_WORKER_COMMAND `
        --os-type Linux `
        --restart-policy OnFailure `
        --ip-address Public    
}
