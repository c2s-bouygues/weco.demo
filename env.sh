
export ADMIN_SUB_DOMAIN=weco.
export USERNAME=admin
export DATAPATH=/data

export HASHED_PASSWORD=$(openssl passwd -apr1 $PASSWORD)
export PORTAINER_HASHED_PASSWORD=$(echo $HASHED_PASSWORD|sed 's/\$/$$/g')
export HOSTNAME=$(hostname)
export MASTER_IP=$(hostname -I | awk '{print $1}')
export MASTER_IP_MASK=$(ip -o -f inet addr show $(ip route list | awk '/^default/ {print $5}') | awk '{print $4}' | sed -e 's|/|\\/|g')