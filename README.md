# WeCo - Démo

Ce dépôt vous donne accès à un setup basique permettant de monter rapidement l'environnement de démo, issu de l'audit WeCo, via [docker-compose](https://docs.docker.com/compose/.

Pour la partie lié à la plateforme de streaming (Ingesters et ETL) un plan App Service sur Azure sera nécessaire. Ces éléments n'tant pas hostés sur la VM que nous allons mettre en place

_<u>Attention :</u> Ce setup n'inclus pas de haut niveau en terme de sécurité ou [haute disponibilité](https://www.digitalocean.com/community/tutorials/what-is-high-availability). Une révision, auprès de personnes compétente, devra être effectuée après validation de l'usage des utilitaires qu'il référence, afin de définitivement clore ces sujets._

## Composants disponibles et plannifiés

### Platforme
* [DONE] [Docker Compose](https://docs.docker.com/compose/)
* [DONE] [Traefik](https://traefik.io) : Serveur web avec autodiscovery
* [DONE] [letsencrypt](https://letsencrypt.org) : Fournisseur de certificats SSL
* [DONE] [Portainer](https://www.portainer.io/) : Panneau d'administration des conteneurs Docker
* [DONE] [Grafana](https://grafana.com/) : Monitoring des services - Construction de dashboard
* [DONE] [Prometheus](https://prometheus.io/) et [AlertManager](https://prometheus.io/docs/alerting/latest/alertmanager/) : Monitoring des services - Collecte de métriques et paramétrage d'alertes
* [DONE] [Loki](https://grafana.com/oss/loki/) : Monitoring des services - Collecte de logs
* [DONE] [ZipKin](https://zipkin.io/) : Monitoring des services - Traces distribuées
* [DONE] [OpenTelemetry Collector](https://opentelemetry.io/docs/collector/) : Monitoring des services - Collecte et distribution des logs, métriques et traces distribuées
* [DONE] [KeyCloak](https://www.keycloak.org/) : Serveur d'authentification
* [DONE] [InfluxDB](https://www.influxdata.com/) et [PostGreSQL](https://www.postgresql.org/) : Serveurs de bases de données
* [DONE] [Adminer](https://www.adminer.org/) : Gestionnaire de base de données
* [DONE] [Pulsar](https://pulsar.apache.org/fr/) et [Pulsar Manager](https://github.com/apache/pulsar-manager) : Plateforme de streaming
* [DONE] [Locust](https://locust.io/) : Outil de test de charge

### Applications
* [DONE] [Noises API](https://hub.docker.com/repository/docker/xrevo/useless-soft-noises-api) et [Smokes API](https://hub.docker.com/repository/docker/xrevo/useless-soft-smokes-api) : API de démonstration envoyant des données dans la plateforme d'observabilité
* [DONE] [Speed Data Ingester](./src/WeCo/WeCo.Ingesters.SpeedDataIngestion/) et [Gas Data Ingester](./src/WeCo/WeCo.Ingesters.GasDataIngestion/) : Ingester de données issue de Pulsar
* [DONE] [Temperature to InfluxDB](./src/WeCo/WeCo.ETL.TemperaturesToInfluxDB/) : ETL chargé de récupérer les données rafinées et les insérer dans InfluxDB

## Mise en place

Toutes les opérations doivent être exécutés en tant que `root` sur la machine distante.
Vous devez utiliser un serveur fonctionnant sous Linux.
Pour exécuter tous les services, vous aurez besoin d'au moins 2 coeurs de processeur, 8 Go de mémoire et 20 Go d'espace disque libre.

Vous aurez également besoin d'un nom de domaine valide, pointant vers ce serveur, pour configurer automatiquement https avec [traefik](https://traefik.io) et [letsencrypt](https://letsencrypt.org).

Si vous souhaitez utiliser des enregistrements DNS de type "A", vous devrez en créer au moins deux :
1) "votre nom de domaine" pointe vers "l'adresse IP de votre serveur"
2) `*.votre nom de domaine` pointait vers `l'adresse IP de votre serveur`

Si vous souhaitez utiliser des enregistrements DNS de type "CNAME", vous devrez en créer autant que le nombre de services que vous allez mettre en place.

Puisque les services sont exécutés avec `docker-compose` (et non `docker swarm`), tous les services seront situés sur votre serveur uniquement.

### 1) Installer docker (s'il ne l'est pas déjà)
Installer docker
```
curl -fsSL https://get.docker.com -o get-docker.sh
DRY_RUN=1 sh ./get-docker.sh
sh ./get-docker.sh
```

Installer docker-compose
```
curl -L "https://github.com/docker/compose/releases/download/1.29.2/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
chmod +x /usr/local/bin/docker-compose
```

### 2) Clôner ce repo
```
git clone https://github.com/c2s-bouygues/weco.demo.git --depth=1 weco
cd weco
```

### 3) Remplisser les variables nécessaires
Note : Il s'agit de données telle que le nom de domaine de votre serveur, votre email, mot de passe pour les authentifications basiques et services SQL, ...

Générez des mots de passe aléatoirement :
```
echo -e "export KEYCLOAK_SQL_PASSWORD=$(echo $RANDOM `date`|md5sum|base64|head -c 25)\n$(cat env.sh)" > env.sh
echo -e "export KEYCLOAK_PASSWORD=$(echo $RANDOM `date`|md5sum|base64|head -c 25)\n$(cat env.sh)" > env.sh
echo -e "export SQL_ROOT_PASSWORD=$(echo $RANDOM `date`|md5sum|base64|head -c 25)\n$(cat env.sh)" > env.sh
echo -e "export PASSWORD=$(echo $RANDOM `date`|md5sum|base64|head -c 25)\n$(cat env.sh)" > env.sh
```

Puis exécutez les lignes suivantes, en y plaçant votre adresse email :
```
echo -e "export EMAIL='Saisissez votre email ici'\n$(cat env.sh)" > env.sh
echo -e "export DOMAIN='Saisissez votre domain ici'\n$(cat env.sh)" > env.sh
```

Il s'agit de vos identifiants. Pensez à les stocker dans un gestionnaire de mot de passe ^^
```
cat env.sh
```

### 4) Préparer votre environment
```
source env.sh
bash setup.sh
```

### 5) Lancez vos services
Avant d'exécuter les commandes qui vous permettront de lancer vos services, vous devez préalablement exécuter les commandes suivantes :
```
docker-compose -f setup-compose.yml up -d
docker-compose -f databases.yml up -d
```

#### La brique de monitoring
Exécuter la ligne de commande suivante pour déployer les conteneurs Docker associés à la brique de monitoring :
```
docker-compose -f monitoring.yml up -d
```

Puis les lignes de commande suivante pour déployer les APIs chargées de transmettre de la données à la brique de monitoring :
```
docker-compose -f apps.yml up -d
```

#### Portainer
Exécuter la ligne de commande suivante pour déployer les conteneurs Docker associés à Portainer :
```
docker-compose -f portainer.yml up -d
```

#### Keycloak
Exécuter la ligne de commande suivante pour déployer les conteneurs Docker associés au Keycloak :
```
docker-compose -f keycloak.yml up -d
```

#### Pulsar
Exécuter la ligne de commande suivante pour déployer les conteneurs Docker associés à Pulsar :
```
docker-compose -f pulsar.yml up -d
```

Puis les lignes de commande suivante pour créer un utilisateur sur Pulsar Manager et créer les tenants, namespaces et topics, nécessaires au bon fonctionnement des Ingester :
```
./pulsar-manager.sh
./pulsar.sh
```

#### Et après ?

Après avoir saisi toutes les commandes précédemment citées, vous pourrez vous connecter à vos nouveaux services via les URLs suivantes :
* https://traefik._`votre_domaine`_
    - Utilisateur : `votre $USERNAME`
    - Mot de passe `votre $PASSWORD`
* https://portainer._`votre_domaine`_
    - Utilisateur : `votre $USERNAME`
    - Mot de passe `votre $PASSWORD`
* https://grafana._`votre_domaine`_
    - Utilisateur : `votre $USERNAME`
    - Mot de passe `votre $PASSWORD`
* https://prometheus._`votre_domaine`_
    - Utilisateur : `votre $USERNAME`
    - Mot de passe `votre $PASSWORD`
* https://zipkin._`votre_domaine`_
    - Utilisateur : `votre $USERNAME`
    - Mot de passe `votre $PASSWORD`
* https://influxdb._`votre_domaine`_
    - Utilisateur : `votre $USERNAME`
    - Mot de passe `votre $PASSWORD`
* https://loki._`votre_domaine`_
    - Utilisateur : `votre $USERNAME`
    - Mot de passe `votre $PASSWORD`
* https://keycloak._`votre_domaine`_
    - Utilisateur : `votre $USERNAME`
    - Mot de passe `votre $KEYCLOAK_PASSWORD`
* https://pulsar-manager._`votre_domaine`_
    - Utilisateur : `votre $USERNAME`
    - Mot de passe `votre $KEYCLOAK_PASSWORD`

NzRlNWZjNDNlNzg3MjdlMjBiY
NzRlNWZjNDNlNzg3MjdlMjBiY

### 6) Locust
Concernant Locust, le mode opérateur est un peu différent.
En effet, Locust est prévu pour être hébergé dans un environnement distinct des autres ressources, ceci pour 2 raisons : 
- simuler un "véritable" trafic en provenance de l'extérieur
- ne pas biaiser les performances en séparant les émetteurs de données des récepteurs

Pour cela, nous avons choisi de monter l'environnement sur Azure, via des ACI (*Azure Container Instance*).
C'est une solution simple et rapide pour exécuter un ou plusieurs conteneurs dans Azure sans avoir à gérer de machines virtuelles.

Pour la création des ressources nécessaires, nous vous mettons à disposition un script PowerShell `locust/init-azure-env.ps1`. 
Celui-ci nécessite la réalisation de quelques opérations préalable à son exécution : 
- la création d'un groupe de ressource
- à l'intérieur de ce dernier, la création d'un compte de stockage 
- dans ce compte de stockage, la création d'un conteneur nommé `locust` et dans lequel vous viendrez déposer le script `load-front-api.py`

> Attention, avant de déposer le script `load-front-api.py`, il est nécessaire de le modifier pour configurer les bonnes URLs vers les Azure Functions (l.88 et 141).

Une fois ceci fait, vous allez pouvoir mettre à jour le script avec les bonnes valeurs : 
- l.2 `$ACI_RESOURCE_GROUP` : le nom de votre groupe de ressource
- l.4 `$ACI_STORAGE_ACCOUNT_NAME` : le nom du compte de stockage
- l.5 `$ACI_SHARE_NAME` : le nom du conteneur de stockage (si vous l'avez modifié)
- l.12 et 37 : le nom du script Python si vous l'avez modifié

La dernière opération a effectué est d'ouvrir une console et d'exécuter les commandes suivantes : 
```
# Connexion à votre compte Azure
az login
# Configuration de la souscription
az account set --subscription "subscriptionId"
# Exécution du script
./init-azure-env.ps1
```

Le script va créer 5 ACI : 1 *master* et 4 *workers*. 
Une fois l'ensemble des opérations terminées, via le portail Azure, vous pourrez récupérer l'ip de l'ACI *master* (`my-rg-name-container-locust-master`).

Rendez-vous sur cette IP, sur le port *8089* et vous aurez accès à l'interface de Locust.

A ce niveau, il ne nous reste plus qu'à configurer les paramètres de votre test de charge, de spécifier une url (celle de Google convient très bien, elle n'est pas utilisée dans le test...) et de démarrer !