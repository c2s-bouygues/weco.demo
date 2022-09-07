$d = [System.DateTime]::Now.ToString("yyMMdd-HHmm")
$d

docker build -f .\WeCo.API.Noises\Dockerfile -t xrevo/useless-soft-noises-api:$d .
docker tag xrevo/useless-soft-noises-api:$d xrevo/useless-soft-noises-api:latest
docker push xrevo/useless-soft-noises-api:$d
docker push xrevo/useless-soft-noises-api:latest

docker build -f .\WeCo.API.Smokes\Dockerfile -t xrevo/useless-soft-smokes-api:$d .
docker tag xrevo/useless-soft-smokes-api:$d xrevo/useless-soft-smokes-api:latest
docker push xrevo/useless-soft-smokes-api:$d
docker push xrevo/useless-soft-smokes-api:latest

docker build -f .\WeCo.SensorsAPI\Dockerfile -t xrevo/useless-soft-sensors:$d .
docker tag xrevo/useless-soft-sensors:$d xrevo/useless-soft-sensors:latest
docker push xrevo/useless-soft-sensors:$d
docker push xrevo/useless-soft-sensors:latest

docker build -f .\WeCo.AlertsAPI\Dockerfile -t xrevo/useless-soft-alerts:$d .
docker tag xrevo/useless-soft-alerts:$d xrevo/useless-soft-alerts:latest
docker push xrevo/useless-soft-alerts:$d
docker push xrevo/useless-soft-alerts:latest
