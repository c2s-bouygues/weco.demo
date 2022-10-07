sudo mkdir -p ${DATAPATH?Variable DATAPATH not set}/{traefik,portainer,postgres,influxdb,grafana,prometheus,alertmanager,loki,promtail,otel-collector,postgres,pulsar,pulsar-manager}/{data,entrypoint,configs,certificates,logs,provisioning}
sudo chown -R $(whoami) ${DATAPATH?Variable DATAPATH not set}
sudo chmod -R 777 ${DATAPATH?Variable DATAPATH not set}

envsubst < configs/postgres.sql > ${DATAPATH?Variable DATAPATH not set}/postgres/entrypoint/init.sql
yes | cp -rf configs/influxdb.sh ${DATAPATH?Variable DATAPATH not set}/influxdb/entrypoint/influxdb.sh
yes | cp -rf configs/prometheus/* ${DATAPATH?Variable DATAPATH not set}/prometheus/provisioning
yes | cp -rf configs/alertmanager/* ${DATAPATH?Variable DATAPATH not set}/alertmanager/provisioning
yes | cp -rf configs/grafana/* ${DATAPATH?Variable DATAPATH not set}/grafana/provisioning
yes | cp -rf configs/loki/* ${DATAPATH?Variable DATAPATH not set}/loki/provisioning
yes | cp -rf configs/promtail/* ${DATAPATH?Variable DATAPATH not set}/promtail/provisioning
yes | cp -rf configs/otel-collector/* ${DATAPATH?Variable DATAPATH not set}/otel-collector/provisioning
