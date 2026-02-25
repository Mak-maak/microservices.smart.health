#!/bin/bash
set -e

# Create additional databases for services that share this PostgreSQL instance
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" <<-EOSQL
    SELECT 'CREATE DATABASE smarthealth_audit'
    WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'smarthealth_audit')\gexec
EOSQL
