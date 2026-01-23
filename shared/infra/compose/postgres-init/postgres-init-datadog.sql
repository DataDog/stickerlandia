-- Enable pg_stat_statements extension
CREATE EXTENSION IF NOT EXISTS pg_stat_statements;

-- Create datadog user with monitoring permissions
CREATE USER datadog WITH PASSWORD 'datadog_password';
GRANT pg_monitor TO datadog;
GRANT SELECT ON pg_stat_database TO datadog;

-- Create schema for execution plans
CREATE SCHEMA IF NOT EXISTS datadog;
GRANT USAGE ON SCHEMA datadog TO datadog;
GRANT SELECT ON ALL TABLES IN SCHEMA datadog TO datadog;

-- Create explain function for execution plans
CREATE OR REPLACE FUNCTION datadog.explain_statement(
   l_query TEXT,
   OUT explain JSON
)
RETURNS SETOF JSON AS
$$
DECLARE
curs REFCURSOR;
plan JSON;
BEGIN
   OPEN curs FOR EXECUTE 'EXPLAIN (FORMAT JSON) ' || l_query;
   LOOP
       FETCH curs INTO plan;
       EXIT WHEN NOT FOUND;
       explain := plan;
       RETURN NEXT;
   END LOOP;
   CLOSE curs;
END;
$$
LANGUAGE plpgsql
SECURITY DEFINER;