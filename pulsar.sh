docker exec -it pulsar bash ./bin/pulsar-admin clusters list
docker exec -it pulsar bash ./bin/pulsar-admin tenants create iot
docker exec -it pulsar bash ./bin/pulsar-admin namespaces create iot/raw
docker exec -it pulsar bash ./bin/pulsar-admin namespaces create iot/gas

docker exec -it pulsar bash ./bin/pulsar-admin topics create persistent://iot/raw/gas
docker exec -it pulsar bash ./bin/pulsar-admin topics create persistent://iot/raw/planes
docker exec -it pulsar bash ./bin/pulsar-admin topics create persistent://iot/gas/azote
docker exec -it pulsar bash ./bin/pulsar-admin topics create persistent://iot/gas/oxygen
docker exec -it pulsar bash ./bin/pulsar-admin topics create persistent://iot/gas/co2

docker exec -it pulsar bash ./bin/pulsar-admin topics create-subscription -s GasInfluxWriter persistent://iot/gas/azote
docker exec -it pulsar bash ./bin/pulsar-admin topics create-subscription -s GasInfluxWriter persistent://iot/gas/oxygen
docker exec -it pulsar bash ./bin/pulsar-admin topics create-subscription -s GasInfluxWriter persistent://iot/gas/co2