docker exec -it pulsar bash ./bin/pulsar-admin clusters list
docker exec -it pulsar bash ./bin/pulsar-admin tenants create iot
docker exec -it pulsar bash ./bin/pulsar-admin namespaces create iot/incoming

docker exec -it pulsar bash ./bin/pulsar-admin topics create persistent://iot/gas/azote
docker exec -it pulsar bash ./bin/pulsar-admin topics create persistent://iot/gas/oxygen
docker exec -it pulsar bash ./bin/pulsar-admin topics create persistent://iot/gas/co2

docker exec -it pulsar bash ./bin/pulsar-admin topics create-subscription -s GasInfluxWriter persistent://iot/gas/azote
docker exec -it pulsar bash ./bin/pulsar-admin topics create-subscription -s GasInfluxWriter persistent://iot/gas/oxygen
docker exec -it pulsar bash ./bin/pulsar-admin topics create-subscription -s GasInfluxWriter persistent://iot/gas/co2