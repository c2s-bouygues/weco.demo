create database keycloak;
create user keycloak with password '${KEYCLOAK_SQL_PASSWORD}';
grant all privileges on database keycloak to keycloak;
