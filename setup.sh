mkdir -p ${DATAPATH?Variable DATAPATH not set}/{traefik,portainer,grafana}/{data,entrypoint,configs,certificates,logs}

cp -r configs/grafana ${DATAPATH?Variable DATAPATH not set}/grafana/provisioning
