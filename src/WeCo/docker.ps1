$d = [System.DateTime]::Now.ToString("yyMMdd-HHmm")
$d

docker build -f .\WeCo.API\Dockerfile -t xrevo/useless-soft-api:$d .
docker tag xrevo/useless-soft-api:$d xrevo/useless-soft-api:latest
docker push xrevo/useless-soft-api:$d
docker push xrevo/useless-soft-api:latest

docker build -f .\WeCo.IsAliveAPI\Dockerfile -t xrevo/useless-soft-is-alive:$d .
docker tag xrevo/useless-soft-is-alive:$d xrevo/useless-soft-is-alive:latest
docker push xrevo/useless-soft-is-alive:$d
docker push xrevo/useless-soft-is-alive:latest

docker build -f .\WeCo.BureaucracyAPI\Dockerfile -t xrevo/useless-soft-bureaucracy:$d .
docker tag xrevo/useless-soft-bureaucracy:$d xrevo/useless-soft-bureaucracy:latest
docker push xrevo/useless-soft-bureaucracy:$d
docker push xrevo/useless-soft-bureaucracy:latest
