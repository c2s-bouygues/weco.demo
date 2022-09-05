mkdir -p ${DATAPATH?Variable DATAPATH not set}/{traefik,portainer,grafana,prometheus,alertmanager,loki,promtail}/{data,entrypoint,configs,certificates,logs}

yes | cp -rf configs/prometheus/* ${DATAPATH?Variable DATAPATH not set}/prometheus/provisioning
yes | cp -rf configs/alertmanager/* ${DATAPATH?Variable DATAPATH not set}/alertmanager/provisioning
yes | cp -rf configs/grafana/* ${DATAPATH?Variable DATAPATH not set}/grafana/provisioning
yes | cp -rf configs/loki/* ${DATAPATH?Variable DATAPATH not set}/loki/provisioning
yes | cp -rf configs/promtail/* ${DATAPATH?Variable DATAPATH not set}/promtail/provisioning
