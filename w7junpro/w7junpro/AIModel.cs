namespace w7junpro
{
    /// <summary>
    /// Hasil estimasi dampak lingkungan dari satu invocation model AI.
    /// </summary>
    public record ImpactResult(int Tokens, double EnergyKWh, double CarbonG, double WaterMl);

    /// <summary>
    /// Base class (Inheritance + Encapsulation + Polymorphism).
    /// Setiap model AI mewarisi class ini dan meng-override EnergyPerKTokenWh
    /// serta EstimateEnergyWh() sesuai karakteristiknya.
    /// </summary>
    public abstract class AIModel
    {
        private const double GridCarbonGPerKWh = 700.0; // faktor emisi grid
        private const double WaterMlPerKWh = 1800.0;    // WUE data-center

        // Encapsulation: nama model hanya bisa diisi lewat constructor.
        public string Name { get; }

        protected AIModel(string name)
        {
            Name = name;
        }

        // Energi (Wh) per 1000 token - dioverride tiap model.
        protected abstract double EnergyPerKTokenWh { get; }

        // Estimasi kasar token dari teks prompt (~1.3 token per kata).
        public static int EstimateTokens(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt)) return 0;
            int words = prompt.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
            return (int)Math.Ceiling(words * 1.3);
        }

        // Polymorphism: virtual, subclass boleh mengubah rumus.
        public virtual double EstimateEnergyWh(int tokens)
        {
            return tokens / 1000.0 * EnergyPerKTokenWh;
        }

        // Proses utama: hitung energi, karbon, dan air untuk sebuah prompt.
        public ImpactResult CalculateImpact(string prompt)
        {
            int tokens = EstimateTokens(prompt);
            double kwh = EstimateEnergyWh(tokens) / 1000.0;
            return new ImpactResult(tokens, kwh, kwh * GridCarbonGPerKWh, kwh * WaterMlPerKWh);
        }

        // Factory: pilih subclass berdasarkan nama di ComboBox.
        public static AIModel Create(string name) => name switch
        {
            "Gemini 2.5 Flash" => new GeminiFlashModel(),
            "GPT-4o Mini" => new Gpt4oMiniModel(),
            "Claude 3.5 Haiku" => new ClaudeHaikuModel(),
            _ => throw new ArgumentException("Model tidak dikenal: " + name)
        };
    }

    public class GeminiFlashModel : AIModel
    {
        public GeminiFlashModel() : base("Gemini 2.5 Flash") { }
        protected override double EnergyPerKTokenWh => 0.10;
    }

    public class Gpt4oMiniModel : AIModel
    {
        public Gpt4oMiniModel() : base("GPT-4o Mini") { }
        protected override double EnergyPerKTokenWh => 0.30;

        // Contoh override rumus: overhead tetap 0.02 Wh per request.
        public override double EstimateEnergyWh(int tokens) => base.EstimateEnergyWh(tokens) + 0.02;
    }

    public class ClaudeHaikuModel : AIModel
    {
        public ClaudeHaikuModel() : base("Claude 3.5 Haiku") { }
        protected override double EnergyPerKTokenWh => 0.20;
    }
}
