-- SolarScope database schema (PostgreSQL 15+ / Supabase)
-- Run once in the Supabase SQL Editor of a new project.
-- The desktop app connects with the database role from the connection string (default: postgres),
-- which owns these tables. Row Level Security blocks access through Supabase's public REST API.

CREATE TABLE IF NOT EXISTS simulations (
    simulation_id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    scenario_name text NOT NULL CHECK (btrim(scenario_name) <> ''),
    created_at timestamptz NOT NULL DEFAULT now(),
    latitude double precision NOT NULL CHECK (latitude BETWEEN -90 AND 90),
    longitude double precision NOT NULL CHECK (longitude BETWEEN -180 AND 180),
    location_name text,
    system_capacity_kwp double precision NOT NULL CHECK (system_capacity_kwp > 0),
    performance_ratio double precision NOT NULL DEFAULT 0.75 CHECK (performance_ratio BETWEEN 0.01 AND 1),
    data_source text NOT NULL DEFAULT 'NASA POWER Climatology',
    annual_average_radiation double precision NOT NULL CHECK (annual_average_radiation >= 0),
    total_annual_energy_kwh double precision NOT NULL CHECK (total_annual_energy_kwh >= 0),
    peak_production_month text NOT NULL,
    lowest_production_month text NOT NULL
);

CREATE TABLE IF NOT EXISTS monthly_productions (
    simulation_id integer NOT NULL REFERENCES simulations(simulation_id) ON DELETE CASCADE,
    month_index integer NOT NULL CHECK (month_index BETWEEN 1 AND 12),
    month_name text NOT NULL,
    solar_radiation_daily_average double precision NOT NULL CHECK (solar_radiation_daily_average >= 0),
    estimated_energy_kwh double precision NOT NULL CHECK (estimated_energy_kwh >= 0),
    PRIMARY KEY (simulation_id, month_index)
);

ALTER TABLE simulations ENABLE ROW LEVEL SECURITY;
ALTER TABLE monthly_productions ENABLE ROW LEVEL SECURITY;
