using Npgsql;

namespace w7junpro
{
    public record UsageSummary(long Invocations, long Tokens, double EnergyKWh, double CarbonG, double WaterMl);

    /// <summary>
    /// Akses PostgreSQL. Connection string dibaca dari environment variable
    /// W7_PG_CONN, default ke PostgreSQL lokal.
    /// </summary>
    public static class Database
    {
        private static readonly string ConnectionString =
            Environment.GetEnvironmentVariable("W7_PG_CONN")
            ?? "Host=localhost;Port=5432;Database=w7junpro;Username=postgres;Password=postgres";

        // Buat tabel jika belum ada.
        public static void Initialize()
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(@"
                CREATE TABLE IF NOT EXISTS ai_usage_log (
                    id           SERIAL PRIMARY KEY,
                    model_name   VARCHAR(100)     NOT NULL,
                    prompt       TEXT             NOT NULL,
                    tokens       INT              NOT NULL,
                    energy_kwh   DOUBLE PRECISION NOT NULL,
                    carbon_g     DOUBLE PRECISION NOT NULL,
                    water_ml     DOUBLE PRECISION NOT NULL,
                    created_at   TIMESTAMP        NOT NULL DEFAULT NOW()
                );", conn);
            cmd.ExecuteNonQuery();
        }

        public static void SaveUsage(AIModel model, string prompt, ImpactResult r)
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(@"
                INSERT INTO ai_usage_log (model_name, prompt, tokens, energy_kwh, carbon_g, water_ml)
                VALUES (@m, @p, @t, @e, @c, @w);", conn);
            cmd.Parameters.AddWithValue("m", model.Name);
            cmd.Parameters.AddWithValue("p", prompt);
            cmd.Parameters.AddWithValue("t", r.Tokens);
            cmd.Parameters.AddWithValue("e", r.EnergyKWh);
            cmd.Parameters.AddWithValue("c", r.CarbonG);
            cmd.Parameters.AddWithValue("w", r.WaterMl);
            cmd.ExecuteNonQuery();
        }

        public static UsageSummary GetSummary()
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(@"
                SELECT COUNT(*), COALESCE(SUM(tokens),0), COALESCE(SUM(energy_kwh),0),
                       COALESCE(SUM(carbon_g),0), COALESCE(SUM(water_ml),0)
                FROM ai_usage_log;", conn);
            using var rd = cmd.ExecuteReader();
            rd.Read();
            return new UsageSummary(rd.GetInt64(0), rd.GetInt64(1), rd.GetDouble(2), rd.GetDouble(3), rd.GetDouble(4));
        }
    }
}
