mkdir -p ${DATAPATH?Variable DATAPATH not set}/{traefik,portainer,grafana,prometheus,alertmanager}/{data,entrypoint,configs,certificates,logs}

cp -r configs/prometheus ${DATAPATH?Variable DATAPATH not set}/prometheus/provisioning
cp -r configs/alertmanager ${DATAPATH?Variable DATAPATH not set}/alertmanager/provisioning
cp -r configs/grafana ${DATAPATH?Variable DATAPATH not set}/grafana/provisioning
