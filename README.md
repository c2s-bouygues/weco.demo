# WeCo - Démo

Ce dépôt vous donne accès à un setup basique permettant de monter rapidement l'environnement de démo, issu de l'audit WeCo, via [docker-compose](https://docs.docker.com/compose/.

_<u>Attention :</u> Ce setup n'inclus pas de haut niveau en terme de sécurité ou [haute disponibilité](https://www.digitalocean.com/community/tutorials/what-is-high-availability). Une révision, auprès de personnes compétente, devra être effectuée après validation de l'usage des utilitaires qu'il référence, afin de définitivement clore ces sujets._

## Composants disponibles et plannifiés

### Platforme
* [DONE] [Docker Compose](https://docs.docker.com/compose/)
* [DONE] [Traefik](https://traefik.io) : Serveur web avec autodiscovery
* [DONE] [letsencrypt](https://letsencrypt.org) : Fournisseur de certificats SSL
* [DONE] [Portainer](https://www.portainer.io/) : Panneau d'administration des conteneurs Docker
* [DONE] [Influxdb 2](https://www.influxdata.com/blog/influxdb-2-0-open-source-is-generally-available/) et [Telegraf](https://www.influxdata.com/time-series-platform/telegraf/) : Monitoring services
* [DONE] [Grafana](https://grafana.com/) et [Victoriametrics](https://victoriametrics.com) : Monitoring des services

### Base de données
* [DONE] [Postgresql](https://www.postgresql.org/) : Base de données SQL
* [DONE] [SQL Adminer](https://www.adminer.org/) : Outil d'administration pour les base de données

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

Après avoir saisi toutes les commandes ci-dessous, vous pourrez vous connecter à vos nouveaux services via les URLs suivantes :
* https://traefik.devops.your_domain user `votre $USERNAME` password `votre $PASSWORD`
* https://portainer.devops.your_domain
* https://influxdb.devops.your_domain user `votre $USERNAME` password `votre $PASSWORD`
* https://grafana.devops.your_domain user `votre $USERNAME` password `votre $PASSWORD`
* https://victoriametrics.devops.your_domain user `votre $USERNAME` password `votre $PASSWORD`
* https://adminer.devops.your_domain user `votre $USERNAME` password `votre $PASSWORD`

Étapes obligatoires :
```
docker-compose -f setup-compose.yml up -d
docker-compose -f databases.yml up -d
```

A partir de maintenant, vous pouvez choisir les services dont vous avez besoin :
```
docker-compose -f monitoring.yml up -d
```

Après avoir activé portainer, vous devez immédiatement vous rendre sur https://portainer.votre_domaine et définir le mot de passe administrateur :
```
docker-compose -f portainer.yml up -d
```

### 6) Configurez les sauvegardes de votre serveur
Au moins le dossier $DATAPATH
