CREATE TABLE IF NOT EXISTS players (
    id UUID PRIMARY KEY,
    name VARCHAR(256) NOT NULL,
    level INT NOT NULL,
    _version BIGINT NOT NULL,
    _updated_at TIMESTAMP NOT NULL,
    _created_at TIMESTAMP NOT NULL
);

CREATE TABLE IF NOT EXISTS player_details (
    id UUID PRIMARY KEY,
    player_id UUID NOT NULL,
    meta_data JSONB NOT NULL,
    _version BIGINT NOT NULL,
    _updated_at TIMESTAMP NOT NULL,
    _created_at TIMESTAMP NOT NULL
);

CREATE TABLE IF NOT EXISTS player_groups (
    id UUID PRIMARY KEY,
    name VARCHAR(256) NOT NULL,
    _version BIGINT NOT NULL,
    _updated_at TIMESTAMP NOT NULL,
    _created_at TIMESTAMP NOT NULL
);

CREATE TABLE IF NOT EXISTS player_group_player (
    player_id UUID NOT NULL,
    group_id UUID NOT NULL
);

CREATE TABLE IF NOT EXISTS player_items (
    id UUID PRIMARY KEY,
    name VARCHAR(256) NOT NULL,
    player_id UUID NULL,
    _version BIGINT NOT NULL,
    _updated_at TIMESTAMP NOT NULL,
    _created_at TIMESTAMP NOT NULL
);

CREATE TABLE IF NOT EXISTS player_powerups (
    id UUID PRIMARY KEY,
    _type VARCHAR(64) NOT NULL,
    player_id UUID NOT NULL,
    energy_boost_power REAL NULL,
    health_boost_power REAL NULL,
    _version BIGINT NOT NULL,
    _updated_at TIMESTAMP NOT NULL,
    _created_at TIMESTAMP NOT NULL
);