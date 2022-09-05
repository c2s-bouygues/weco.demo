mkdir -p ${DATAPATH?Variable DATAPATH not set}/{traefik,postgres,portainer,grafana}/{data,entrypoint,configs,certificates,logs}

envsubst < configs/postgres.sql > ${DATAPATH?Variable DATAPATH not set}/postgres/entrypoint/init.sql
cp -r configs/grafana ${DATAPATH?Variable DATAPATH not set}/grafana/provisioning
