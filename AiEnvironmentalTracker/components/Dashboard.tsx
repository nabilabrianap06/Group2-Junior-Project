"use client";

import React, { useState, useEffect, useMemo } from "react";
import {
  Activity,
  Zap,
  CloudRain,
  Droplets,
  Server,
  FileSpreadsheet,
  FileText,
  Clock,
  ShieldCheck,
  RefreshCw,
  ExternalLink,
  BookOpen,
  Code2,
  Filter,
  CheckCircle2,
  ChevronDown,
  Smartphone,
  Lightbulb,
  Search,
  ArrowUpRight,
  Database
} from "lucide-react";
import {
  AreaChart,
  Area,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell
} from "recharts";

// ── Types ─────────────────────────────────────────────────────────────

interface TelemetrySummary {
  totalRequests: number;
  totalTokens: number;
  totalEnergyKWh: number;
  totalCarbonGrams: number;
  totalWaterML: number;
  avgLatencyMs: number;
  smartphoneChargesEquivalent: number;
  waterBottlesEquivalent: number;
  ledBulbHoursEquivalent: number;
}

interface TelemetryLog {
  id: string;
  provider: string;
  modelName: string;
  promptTokens: number;
  completionTokens: number;
  totalTokens: number;
  energyKWh: number;
  carbonGrams: number;
  waterML: number;
  analogyString: string;
  latencyMs: number;
  isStreaming: boolean;
  statusCode: number;
  createdAt: string;
}

interface ProviderBreakdown {
  provider: string;
  requestCount: number;
  totalTokens: number;
  energyKWh: number;
  carbonGrams: number;
  waterML: number;
  percentage: number;
}

// ── Scientific Formatting Helpers ─────────────────────────────────────

function formatElectricity(kwh: number): { value: string; unit: string } {
  if (!kwh || isNaN(kwh)) return { value: "0.00", unit: "kWh" };
  if (kwh < 0.001) {
    return { value: (kwh * 1000).toFixed(3), unit: "mWh" };
  }
  return { value: kwh.toFixed(5), unit: "kWh" };
}

function formatCarbon(grams: number): { value: string; unit: string } {
  if (!grams || isNaN(grams)) return { value: "0.00", unit: "g CO₂eq" };
  if (grams >= 1000) {
    return { value: (grams / 1000).toFixed(3), unit: "kg CO₂eq" };
  }
  return { value: grams.toFixed(4), unit: "g CO₂eq" };
}

function formatWater(ml: number): { value: string; unit: string } {
  if (!ml || isNaN(ml)) return { value: "0.00", unit: "mL" };
  if (ml >= 1000) {
    return { value: (ml / 1000).toFixed(3), unit: "L" };
  }
  return { value: ml.toFixed(4), unit: "mL" };
}

function formatInteger(num: number): string {
  if (!num || isNaN(num)) return "0";
  return new Intl.NumberFormat("en-US").format(num);
}

// ── Palette Definition ────────────────────────────────────────────────

const COLORS = {
  emerald: "#059669",
  navy: "#1E3A8A",
  sky: "#0284C7",
  amber: "#D97706",
  slate: "#475569",
  charcoal: "#0F172A",
  border: "#E2E8F0",
  bgSubtle: "#F8FAFC",
};

const PROVIDER_COLORS: Record<string, string> = {
  Gemini: "#0284C7",
  OpenAI: "#059669",
  DeepSeek: "#7C3AED",
  Claude: "#D97706",
  Groq: "#EA580C",
  Ollama: "#475569",
};

// ── Main Dashboard Component ──────────────────────────────────────────

export default function Dashboard() {
  // State
  const [summary, setSummary] = useState<TelemetrySummary | null>(null);
  const [logs, setLogs] = useState<TelemetryLog[]>([]);
  const [providers, setProviders] = useState<ProviderBreakdown[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [isRefreshing, setIsRefreshing] = useState<boolean>(false);
  const [searchQuery, setSearchQuery] = useState<string>("");
  const [selectedModelFilter, setSelectedModelFilter] = useState<string>("all");
  const [dateRange, setDateRange] = useState<string>("all");

  // Modals
  const [showMethodologyModal, setShowMethodologyModal] = useState<boolean>(false);
  const [showApiModal, setShowApiModal] = useState<boolean>(false);

  // Live Audit Console State
  const [testModel, setTestModel] = useState<string>("gemini-2.5-flash");
  const [testPrompt, setTestPrompt] = useState<string>("Evaluate the carbon intensity of distributed renewable energy systems.");
  const [isExecutingAudit, setIsExecutingAudit] = useState<boolean>(false);
  const [auditOutput, setAuditOutput] = useState<string>("");
  const [auditLatency, setAuditLatency] = useState<number | null>(null);

  // Fetch telemetry
  const fetchData = async () => {
    try {
      setIsRefreshing(true);
      const [sumRes, logsRes, provRes] = await Promise.all([
        fetch("/api/telemetry/summary"),
        fetch("/api/telemetry/logs?count=100"),
        fetch("/api/telemetry/providers"),
      ]);

      if (sumRes.ok) setSummary(await sumRes.json());
      if (logsRes.ok) setLogs(await logsRes.json());
      if (provRes.ok) setProviders(await provRes.json());
    } catch (err) {
      console.error("Telemetry fetch error:", err);
    } finally {
      setLoading(false);
      setIsRefreshing(false);
    }
  };

  useEffect(() => {
    fetchData();
    const interval = setInterval(fetchData, 4000);
    return () => clearInterval(interval);
  }, []);

  // Filtered Logs
  const filteredLogs = useMemo(() => {
    return logs.filter((log) => {
      const matchesSearch =
        log.modelName.toLowerCase().includes(searchQuery.toLowerCase()) ||
        log.provider.toLowerCase().includes(searchQuery.toLowerCase()) ||
        log.id.toLowerCase().includes(searchQuery.toLowerCase());

      const matchesModel =
        selectedModelFilter === "all" ||
        log.modelName.toLowerCase() === selectedModelFilter.toLowerCase();

      return matchesSearch && matchesModel;
    });
  }, [logs, searchQuery, selectedModelFilter]);

  // Chart Data: Cumulative Carbon & Energy
  const chartData = useMemo(() => {
    const sorted = [...logs].reverse();
    let accCarbon = 0;
    let accEnergy = 0;
    return sorted.map((l, index) => {
      accCarbon += l.carbonGrams;
      accEnergy += l.energyKWh * 1000; // in mWh
      return {
        name: `Q${index + 1}`,
        time: new Date(l.createdAt).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit", second: "2-digit" }),
        carbon: Number(accCarbon.toFixed(4)),
        energyMWh: Number(accEnergy.toFixed(3)),
        tokens: l.totalTokens,
        model: l.modelName,
      };
    });
  }, [logs]);

  // Handle Audit Prompt Submission
  const handleExecuteAudit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!testPrompt.trim()) return;

    setIsExecutingAudit(true);
    setAuditOutput("Initiating telemetry stream and parsing LLM usage metadata...\n");
    const startTime = performance.now();

    try {
      const response = await fetch("/v1/chat/completions", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          model: testModel,
          messages: [{ role: "user", content: testPrompt }],
          stream: true,
        }),
      });

      if (!response.ok) {
        const err = await response.text();
        setAuditOutput(`[Audit Exception ${response.status}]: ${err}`);
        setIsExecutingAudit(false);
        return;
      }

      const reader = response.body?.getReader();
      const decoder = new TextDecoder("utf-8");
      let buffer = "";
      let completeText = "";

      if (reader) {
        while (true) {
          const { done, value } = await reader.read();
          if (done) break;

          buffer += decoder.decode(value, { stream: true });
          const lines = buffer.split("\n");
          buffer = lines.pop() || "";

          for (const line of lines) {
            const trimmed = line.trim();
            if (trimmed.startsWith("data: ")) {
              const data = trimmed.substring(6);
              if (data === "[DONE]") break;
              try {
                const parsed = JSON.parse(data);
                const token = parsed.choices?.[0]?.delta?.content || "";
                completeText += token;
                setAuditOutput(completeText);
              } catch {
                // Ignore non-json chunks
              }
            }
          }
        }
      }

      setAuditLatency(Math.round(performance.now() - startTime));
      fetchData();
    } catch (err: any) {
      setAuditOutput(`[Audit Connection Error]: ${err.message}`);
    } finally {
      setIsExecutingAudit(false);
    }
  };

  // CSV Export Utility
  const handleExportCSV = () => {
    if (logs.length === 0) return;
    const headers = [
      "ID",
      "Timestamp_UTC",
      "Provider",
      "Model",
      "Prompt_Tokens",
      "Completion_Tokens",
      "Total_Tokens",
      "Electricity_kWh",
      "Carbon_gCO2eq",
      "Water_mL",
      "Latency_ms",
      "Status",
    ];

    const rows = logs.map((l) => [
      l.id,
      l.createdAt,
      l.provider,
      l.modelName,
      l.promptTokens,
      l.completionTokens,
      l.totalTokens,
      l.energyKWh,
      l.carbonGrams,
      l.waterML,
      l.latencyMs,
      l.statusCode,
    ]);

    const csvContent =
      "data:text/csv;charset=utf-8," +
      [headers.join(","), ...rows.map((e) => e.join(","))].join("\n");

    const encodedUri = encodeURI(csvContent);
    const link = document.createElement("a");
    link.setAttribute("href", encodedUri);
    link.setAttribute("download", `ai_environmental_telemetry_${Date.now()}.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  };

  const energyFormatted = formatElectricity(summary?.totalEnergyKWh || 0);
  const carbonFormatted = formatCarbon(summary?.totalCarbonGrams || 0);
  const waterFormatted = formatWater(summary?.totalWaterML || 0);

  return (
    <div className="min-h-screen bg-[#F8FAFC] text-[#0F172A] font-sans antialiased selection:bg-slate-200">
      
      {/* ── Global Institutional Header ───────────────────────────────── */}
      <header className="border-b border-[#E2E8F0] bg-white sticky top-0 z-30 shadow-sm">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex items-center justify-between h-16">
            
            {/* Title & Institutional Badge */}
            <div className="flex items-center space-x-3">
              <div className="w-9 h-9 rounded bg-[#1E3A8A] flex items-center justify-center text-white shadow-sm">
                <Database className="w-5 h-5 text-sky-200" />
              </div>
              <div>
                <div className="flex items-center space-x-2">
                  <h1 className="text-base font-semibold tracking-tight text-[#0F172A]">
                    AI Environmental Telemetry & Impact Portal
                  </h1>
                  <span className="inline-flex items-center px-2 py-0.5 rounded text-[11px] font-medium bg-slate-100 text-slate-700 border border-slate-200">
                    ISO 14064 Compliance Framework
                  </span>
                </div>
                <p className="text-xs text-slate-500 font-normal">
                  Department of Electrical Engineering & Information Technology · FT UGM
                </p>
              </div>
            </div>

            {/* Actions & Utilities */}
            <div className="flex items-center space-x-2">
              <button
                onClick={() => setShowApiModal(true)}
                className="inline-flex items-center px-3 py-1.5 text-xs font-medium text-slate-700 bg-white border border-[#E2E8F0] rounded hover:bg-slate-50 transition-colors shadow-xs"
              >
                <Code2 className="w-3.5 h-3.5 mr-1.5 text-slate-500" />
                API & Proxy Access
              </button>

              <button
                onClick={() => setShowMethodologyModal(true)}
                className="inline-flex items-center px-3 py-1.5 text-xs font-medium text-[#1E3A8A] bg-blue-50 border border-blue-200 rounded hover:bg-blue-100/70 transition-colors shadow-xs"
              >
                <BookOpen className="w-3.5 h-3.5 mr-1.5 text-[#1E3A8A]" />
                Methodology & Sources
              </button>

              <div className="h-4 w-px bg-slate-200 mx-1" />

              <button
                onClick={fetchData}
                disabled={isRefreshing}
                title="Refresh telemetry feed"
                className="p-1.5 text-slate-500 hover:text-slate-800 rounded border border-transparent hover:border-slate-200 transition-colors"
              >
                <RefreshCw className={`w-4 h-4 ${isRefreshing ? "animate-spin text-emerald-600" : ""}`} />
              </button>
            </div>
          </div>
        </div>

        {/* Global Filter Bar */}
        <div className="border-t border-[#E2E8F0] bg-[#F8FAFC]">
          <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-2 flex flex-wrap items-center justify-between gap-3 text-xs">
            
            <div className="flex items-center space-x-3">
              <span className="text-slate-500 font-medium flex items-center">
                <Filter className="w-3.5 h-3.5 mr-1 text-slate-400" />
                Filters:
              </span>

              <select
                value={selectedModelFilter}
                onChange={(e) => setSelectedModelFilter(e.target.value)}
                className="bg-white border border-[#E2E8F0] text-slate-800 text-xs rounded px-2.5 py-1 outline-none focus:border-slate-400 shadow-2xs cursor-pointer font-medium"
              >
                <option value="all">All Models (Aggregate)</option>
                <option value="gemini-2.5-flash">Gemini 2.5 Flash</option>
                <option value="deepseek-chat">DeepSeek Chat</option>
                <option value="gpt-4o">GPT-4o</option>
                <option value="gpt-4o-mini">GPT-4o Mini</option>
                <option value="claude-3-5-sonnet-20241022">Claude 3.5 Sonnet</option>
              </select>

              <select
                value={dateRange}
                onChange={(e) => setDateRange(e.target.value)}
                className="bg-white border border-[#E2E8F0] text-slate-800 text-xs rounded px-2.5 py-1 outline-none focus:border-slate-400 shadow-2xs cursor-pointer font-medium"
              >
                <option value="all">Full Registry (All Time)</option>
                <option value="today">Today (Past 24 Hours)</option>
                <option value="week">Trailing 7 Days</option>
                <option value="month">Current Month</option>
              </select>
            </div>

            <div className="flex items-center space-x-2">
              <span className="text-[11px] text-slate-500">
                Gateway Host: <code className="bg-slate-200/70 text-slate-700 px-1 rounded font-mono">http://localhost:5000/v1</code>
              </span>
              <button
                onClick={handleExportCSV}
                className="inline-flex items-center px-2.5 py-1 text-xs font-medium text-slate-700 bg-white border border-slate-300 rounded hover:bg-slate-50 transition-colors shadow-2xs"
              >
                <FileSpreadsheet className="w-3.5 h-3.5 mr-1 text-emerald-600" />
                Export CSV
              </button>
            </div>

          </div>
        </div>
      </header>

      {/* ── Main Dashboard Content ────────────────────────────────────── */}
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6 space-y-6">
        
        {/* ── Section A: Key Telemetry Summary Cards ──────────────────── */}
        <section>
          <div className="flex items-center justify-between mb-3">
            <h2 className="text-xs font-semibold uppercase tracking-wider text-slate-500">
              Primary Environmental Telemetry
            </h2>
            <span className="text-[11px] text-slate-400">
              Continuous live integration with Supabase Registry
            </span>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-4">
            
            {/* Card 1: Total Queries */}
            <div className="bg-white border border-[#E2E8F0] rounded-lg p-4 shadow-xs">
              <div className="flex items-center justify-between text-slate-500 mb-1">
                <span className="text-xs font-medium uppercase tracking-wider">Total Invocations</span>
                <Activity className="w-4 h-4 text-slate-400" />
              </div>
              <div className="text-2xl font-bold tracking-tight text-[#0F172A] tabular-nums font-mono">
                {formatInteger(summary?.totalRequests || 0)}
              </div>
              <div className="mt-2 text-[11px] text-slate-500 flex items-center">
                <span className="inline-block w-1.5 h-1.5 rounded-full bg-emerald-500 mr-1.5" />
                Real-time proxy requests
              </div>
            </div>

            {/* Card 2: Total Tokens */}
            <div className="bg-white border border-[#E2E8F0] rounded-lg p-4 shadow-xs">
              <div className="flex items-center justify-between text-slate-500 mb-1">
                <span className="text-xs font-medium uppercase tracking-wider">Tokens Processed</span>
                <Server className="w-4 h-4 text-slate-400" />
              </div>
              <div className="text-2xl font-bold tracking-tight text-[#0F172A] tabular-nums font-mono">
                {formatInteger(summary?.totalTokens || 0)}
              </div>
              <div className="mt-2 text-[11px] text-slate-500">
                Prompt & completion payload
              </div>
            </div>

            {/* Card 3: Electricity Consumed */}
            <div className="bg-white border border-[#E2E8F0] rounded-lg p-4 shadow-xs">
              <div className="flex items-center justify-between text-slate-500 mb-1">
                <span className="text-xs font-medium uppercase tracking-wider">Grid Electricity</span>
                <Zap className="w-4 h-4 text-amber-500" />
              </div>
              <div className="flex items-baseline space-x-1.5">
                <span className="text-2xl font-bold tracking-tight text-[#0F172A] tabular-nums font-mono">
                  {energyFormatted.value}
                </span>
                <span className="text-xs font-semibold text-slate-500">{energyFormatted.unit}</span>
              </div>
              <div className="mt-2 text-[11px] text-slate-500">
                Baseline: ~0.0003 kWh / 1k tok
              </div>
            </div>

            {/* Card 4: Carbon Emissions */}
            <div className="bg-white border border-[#E2E8F0] rounded-lg p-4 shadow-xs">
              <div className="flex items-center justify-between text-slate-500 mb-1">
                <span className="text-xs font-medium uppercase tracking-wider">Carbon Footprint</span>
                <CloudRain className="w-4 h-4 text-emerald-600" />
              </div>
              <div className="flex items-baseline space-x-1.5">
                <span className="text-2xl font-bold tracking-tight text-[#0F172A] tabular-nums font-mono">
                  {carbonFormatted.value}
                </span>
                <span className="text-xs font-semibold text-slate-500">{carbonFormatted.unit}</span>
              </div>
              <div className="mt-2 text-[11px] text-slate-500">
                Intensity factor: 400 g/kWh
              </div>
            </div>

            {/* Card 5: Cooling Water Footprint */}
            <div className="bg-white border border-[#E2E8F0] rounded-lg p-4 shadow-xs">
              <div className="flex items-center justify-between text-slate-500 mb-1">
                <span className="text-xs font-medium uppercase tracking-wider">Direct Water Usage</span>
                <Droplets className="w-4 h-4 text-sky-600" />
              </div>
              <div className="flex items-baseline space-x-1.5">
                <span className="text-2xl font-bold tracking-tight text-[#0F172A] tabular-nums font-mono">
                  {waterFormatted.value}
                </span>
                <span className="text-xs font-semibold text-slate-500">{waterFormatted.unit}</span>
              </div>
              <div className="mt-2 text-[11px] text-slate-500">
                Data-center WUE: 1.5 mL/kWh
              </div>
            </div>

          </div>
        </section>

        {/* ── Section B: Human Impact Equivalencies ───────────────────── */}
        <section className="bg-white border border-[#E2E8F0] rounded-lg p-4 shadow-xs">
          <h2 className="text-xs font-semibold uppercase tracking-wider text-slate-500 mb-3">
            Physical Impact Equivalencies (Standard Reference Baseline)
          </h2>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            
            <div className="border border-slate-100 bg-[#F8FAFC] rounded-md p-3 flex items-start space-x-3">
              <div className="p-2 rounded bg-slate-200/70 text-slate-700">
                <Smartphone className="w-4 h-4" />
              </div>
              <div>
                <div className="text-xs text-slate-500 font-medium">Smartphone Full Charge Equivalent</div>
                <div className="text-lg font-bold text-[#0F172A] font-mono tabular-nums mt-0.5">
                  {summary?.smartphoneChargesEquivalent?.toFixed(2) || "0.00"} <span className="text-xs font-normal text-slate-500">charges</span>
                </div>
                <div className="text-[11px] text-slate-500 mt-0.5">Standard 15 Wh (0.015 kWh) Li-ion battery</div>
              </div>
            </div>

            <div className="border border-slate-100 bg-[#F8FAFC] rounded-md p-3 flex items-start space-x-3">
              <div className="p-2 rounded bg-blue-100/70 text-sky-700">
                <Droplets className="w-4 h-4" />
              </div>
              <div>
                <div className="text-xs text-slate-500 font-medium">Freshwater Bottle Unit Ratio</div>
                <div className="text-lg font-bold text-[#0F172A] font-mono tabular-nums mt-0.5">
                  {summary?.waterBottlesEquivalent?.toFixed(4) || "0.0000"} <span className="text-xs font-normal text-slate-500">bottles</span>
                </div>
                <div className="text-[11px] text-slate-500 mt-0.5">Fraction of standard 600 mL potable unit</div>
              </div>
            </div>

            <div className="border border-slate-100 bg-[#F8FAFC] rounded-md p-3 flex items-start space-x-3">
              <div className="p-2 rounded bg-amber-100/70 text-amber-700">
                <Lightbulb className="w-4 h-4" />
              </div>
              <div>
                <div className="text-xs text-slate-500 font-medium">10W LED Grid Load Duration</div>
                <div className="text-lg font-bold text-[#0F172A] font-mono tabular-nums mt-0.5">
                  {summary?.ledBulbHoursEquivalent?.toFixed(2) || "0.00"} <span className="text-xs font-normal text-slate-500">hours</span>
                </div>
                <div className="text-[11px] text-slate-500 mt-0.5">Continuous domestic LED bulb operation</div>
              </div>
            </div>

          </div>
        </section>

        {/* ── Section C: Split View (Console & Analytics) ──────────────── */}
        <section className="grid grid-cols-1 lg:grid-cols-12 gap-6">
          
          {/* Left Column: Live Audit Query Console (5 cols) */}
          <div className="lg:col-span-5 bg-white border border-[#E2E8F0] rounded-lg p-4 shadow-xs flex flex-col">
            <div className="flex items-center justify-between pb-3 border-b border-[#E2E8F0] mb-3">
              <div>
                <h3 className="text-sm font-semibold text-[#0F172A]">Live Telemetry Audit Console</h3>
                <p className="text-[11px] text-slate-500">Simulate client query to trigger real-time telemetry capture</p>
              </div>
              <span className="inline-flex items-center px-1.5 py-0.5 rounded text-[10px] font-mono bg-emerald-50 text-emerald-700 border border-emerald-200">
                POST /v1/chat/completions
              </span>
            </div>

            <form onSubmit={handleExecuteAudit} className="space-y-3 flex-1 flex flex-col">
              <div>
                <label className="block text-xs font-medium text-slate-700 mb-1">
                  Target Upstream Model
                </label>
                <select
                  value={testModel}
                  onChange={(e) => setTestModel(e.target.value)}
                  className="w-full bg-[#F8FAFC] border border-[#E2E8F0] text-slate-800 text-xs rounded px-3 py-1.5 outline-none focus:border-slate-400 font-medium"
                >
                  <option value="gemini-2.5-flash">Gemini 2.5 Flash (Google AI)</option>
                  <option value="deepseek-chat">DeepSeek Chat (DeepSeek-V3)</option>
                  <option value="gpt-4o">GPT-4o (OpenAI)</option>
                  <option value="gpt-4o-mini">GPT-4o Mini (OpenAI)</option>
                  <option value="claude-3-5-sonnet-20241022">Claude 3.5 Sonnet (Anthropic)</option>
                </select>
              </div>

              <div className="flex-1 flex flex-col">
                <label className="block text-xs font-medium text-slate-700 mb-1">
                  Test Prompt Payload
                </label>
                <textarea
                  rows={3}
                  value={testPrompt}
                  onChange={(e) => setTestPrompt(e.target.value)}
                  placeholder="Enter test audit prompt..."
                  className="w-full bg-[#F8FAFC] border border-[#E2E8F0] text-slate-800 text-xs rounded p-2.5 outline-none focus:border-slate-400 resize-none font-sans"
                  required
                />
              </div>

              <div className="flex items-center justify-between pt-1">
                <button
                  type="submit"
                  disabled={isExecutingAudit}
                  className="inline-flex items-center px-3.5 py-1.5 text-xs font-semibold text-white bg-[#1E3A8A] rounded hover:bg-blue-900 transition-colors disabled:opacity-50 shadow-xs cursor-pointer"
                >
                  {isExecutingAudit ? (
                    <>
                      <RefreshCw className="w-3 h-3 mr-1.5 animate-spin" />
                      Streaming Telemetry...
                    </>
                  ) : (
                    <>
                      <Activity className="w-3 h-3 mr-1.5" />
                      Execute Audit & Stream
                    </>
                  )}
                </button>

                {auditLatency !== null && (
                  <span className="text-[11px] text-slate-500 font-mono">
                    Latency: <strong className="text-slate-800">{auditLatency} ms</strong>
                  </span>
                )}
              </div>
            </form>

            {/* Audit Output Box */}
            {auditOutput && (
              <div className="mt-3 pt-3 border-t border-[#E2E8F0]">
                <div className="text-[11px] font-medium text-slate-500 mb-1">Streamed Output & Decoded Metadata:</div>
                <div className="bg-[#F8FAFC] border border-[#E2E8F0] rounded p-2 text-xs font-mono text-slate-700 max-h-36 overflow-y-auto whitespace-pre-wrap leading-relaxed">
                  {auditOutput}
                </div>
              </div>
            )}
          </div>

          {/* Right Column: Analytics & Charts (7 cols) */}
          <div className="lg:col-span-7 bg-white border border-[#E2E8F0] rounded-lg p-4 shadow-xs flex flex-col justify-between">
            <div className="flex items-center justify-between pb-2 border-b border-[#E2E8F0] mb-3">
              <div>
                <h3 className="text-sm font-semibold text-[#0F172A]">Cumulative Environmental Impact Trend</h3>
                <p className="text-[11px] text-slate-500">Cumulative carbon equivalent ($\text{g CO}_2\text{eq}$) and energy load ($\text{mWh}$)</p>
              </div>
              <div className="flex items-center space-x-3 text-xs font-medium">
                <span className="flex items-center text-emerald-700">
                  <span className="w-2.5 h-2.5 rounded-sm bg-emerald-600 mr-1.5" /> Carbon ($\text{g}$)
                </span>
                <span className="flex items-center text-sky-700">
                  <span className="w-2.5 h-2.5 rounded-sm bg-sky-500 mr-1.5" /> Energy ($\text{mWh}$)
                </span>
              </div>
            </div>

            <div className="h-56 w-full">
              {chartData.length > 0 ? (
                <ResponsiveContainer width="100%" height="100%">
                  <AreaChart data={chartData} margin={{ top: 10, right: 10, left: -20, bottom: 0 }}>
                    <defs>
                      <linearGradient id="colorCarbon" x1="0" y1="0" x2="0" y2="1">
                        <stop offset="5%" stopColor="#059669" stopOpacity={0.2} />
                        <stop offset="95%" stopColor="#059669" stopOpacity={0} />
                      </linearGradient>
                      <linearGradient id="colorEnergy" x1="0" y1="0" x2="0" y2="1">
                        <stop offset="5%" stopColor="#0284C7" stopOpacity={0.2} />
                        <stop offset="95%" stopColor="#0284C7" stopOpacity={0} />
                      </linearGradient>
                    </defs>
                    <CartesianGrid strokeDasharray="3 3" stroke="#E2E8F0" vertical={false} />
                    <XAxis dataKey="name" stroke="#94A3B8" fontSize={10} tickLine={false} />
                    <YAxis stroke="#94A3B8" fontSize={10} tickLine={false} />
                    <Tooltip
                      contentStyle={{
                        backgroundColor: "#FFFFFF",
                        border: "1px solid #CBD5E1",
                        borderRadius: "4px",
                        fontSize: "11px",
                        boxShadow: "0 2px 4px rgba(0,0,0,0.05)",
                      }}
                    />
                    <Area type="monotone" dataKey="carbon" stroke="#059669" strokeWidth={1.5} fillOpacity={1} fill="url(#colorCarbon)" name="Carbon (g CO₂eq)" />
                    <Area type="monotone" dataKey="energyMWh" stroke="#0284C7" strokeWidth={1.5} fillOpacity={1} fill="url(#colorEnergy)" name="Energy (mWh)" />
                  </AreaChart>
                </ResponsiveContainer>
              ) : (
                <div className="h-full flex items-center justify-center text-xs text-slate-400">
                  Insufficient data points to plot continuous trajectory.
                </div>
              )}
            </div>

            {/* Provider Breakdown Mini Bars */}
            <div className="mt-3 pt-3 border-t border-[#E2E8F0]">
              <div className="text-[11px] font-semibold text-slate-500 uppercase tracking-wider mb-2">
                Provider Workload Distribution
              </div>
              <div className="space-y-1.5">
                {providers.length > 0 ? (
                  providers.map((p) => (
                    <div key={p.provider} className="flex items-center text-xs">
                      <span className="w-20 font-medium text-slate-700 truncate">{p.provider}</span>
                      <div className="flex-1 bg-slate-100 rounded-full h-2 mx-2 overflow-hidden">
                        <div
                          className="h-full rounded-full transition-all duration-500"
                          style={{
                            width: `${p.percentage}%`,
                            backgroundColor: PROVIDER_COLORS[p.provider] || COLORS.navy,
                          }}
                        />
                      </div>
                      <span className="w-24 text-right font-mono text-[11px] text-slate-600">
                        {p.percentage}% ({p.totalTokens.toLocaleString()} tok)
                      </span>
                    </div>
                  ))
                ) : (
                  <div className="text-xs text-slate-400">No multi-provider data recorded yet.</div>
                )}
              </div>
            </div>

          </div>

        </section>

        {/* ── Section D: Telemetry Log Data Table ──────────────────────── */}
        <section className="bg-white border border-[#E2E8F0] rounded-lg shadow-xs overflow-hidden">
          
          {/* Table Header Controls */}
          <div className="p-4 border-b border-[#E2E8F0] flex flex-wrap items-center justify-between gap-3">
            <div>
              <h3 className="text-sm font-semibold text-[#0F172A]">Statistical Telemetry Registry</h3>
              <p className="text-[11px] text-slate-500">Granular record of all proxied LLM inference operations</p>
            </div>

            <div className="flex items-center space-x-2">
              <div className="relative">
                <Search className="w-3.5 h-3.5 absolute left-2.5 top-2 text-slate-400" />
                <input
                  type="text"
                  placeholder="Search model, provider, ID..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="bg-[#F8FAFC] border border-[#E2E8F0] text-slate-800 text-xs rounded pl-8 pr-2.5 py-1 outline-none focus:border-slate-400 w-48 shadow-2xs"
                />
              </div>
            </div>
          </div>

          {/* Table Element */}
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse text-xs">
              <thead>
                <tr className="bg-[#F8FAFC] border-b border-[#E2E8F0] text-slate-500 font-semibold text-[11px] uppercase tracking-wider">
                  <th className="py-2.5 px-3">Timestamp (UTC)</th>
                  <th className="py-2.5 px-3">Provider</th>
                  <th className="py-2.5 px-3">Model Specification</th>
                  <th className="py-2.5 px-3 text-right">Tokens (In / Out)</th>
                  <th className="py-2.5 px-3 text-right">Electricity</th>
                  <th className="py-2.5 px-3 text-right">Carbon ($\text{CO}_2\text{eq}$)</th>
                  <th className="py-2.5 px-3 text-right">Water</th>
                  <th className="py-2.5 px-3 text-right">Latency</th>
                  <th className="py-2.5 px-3 text-center">Status</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[#E2E8F0] font-sans">
                {filteredLogs.length > 0 ? (
                  filteredLogs.map((log) => {
                    const electricity = formatElectricity(log.energyKWh);
                    const carbon = formatCarbon(log.carbonGrams);
                    const water = formatWater(log.waterML);

                    return (
                      <tr key={log.id} className="hover:bg-slate-50/80 transition-colors">
                        
                        {/* Timestamp */}
                        <td className="py-2.5 px-3 text-slate-600 font-mono text-[11px]">
                          {new Date(log.createdAt).toISOString().replace("T", " ").substring(0, 19)}
                        </td>

                        {/* Provider */}
                        <td className="py-2.5 px-3 font-medium text-slate-900">
                          <span
                            className="inline-block w-2 h-2 rounded-full mr-1.5"
                            style={{ backgroundColor: PROVIDER_COLORS[log.provider] || COLORS.slate }}
                          />
                          {log.provider}
                        </td>

                        {/* Model */}
                        <td className="py-2.5 px-3 font-mono text-slate-700 text-[11px]">
                          {log.modelName}
                          {log.isStreaming && (
                            <span className="ml-1.5 px-1 py-0.2 rounded text-[9px] bg-slate-100 text-slate-600 border border-slate-200">
                              SSE
                            </span>
                          )}
                        </td>

                        {/* Tokens */}
                        <td className="py-2.5 px-3 text-right font-mono tabular-nums text-slate-800">
                          <span className="text-slate-400">{log.promptTokens}</span> /{" "}
                          <span className="text-slate-600">{log.completionTokens}</span> (
                          <strong>{log.totalTokens}</strong>)
                        </td>

                        {/* Electricity */}
                        <td className="py-2.5 px-3 text-right font-mono tabular-nums text-slate-800">
                          {electricity.value} <span className="text-slate-400 text-[10px]">{electricity.unit}</span>
                        </td>

                        {/* Carbon */}
                        <td className="py-2.5 px-3 text-right font-mono tabular-nums text-slate-800">
                          {carbon.value} <span className="text-slate-400 text-[10px]">{carbon.unit}</span>
                        </td>

                        {/* Water */}
                        <td className="py-2.5 px-3 text-right font-mono tabular-nums text-slate-800">
                          {water.value} <span className="text-slate-400 text-[10px]">{water.unit}</span>
                        </td>

                        {/* Latency */}
                        <td className="py-2.5 px-3 text-right font-mono tabular-nums text-slate-600">
                          {log.latencyMs} ms
                        </td>

                        {/* Status */}
                        <td className="py-2.5 px-3 text-center">
                          <span className="inline-flex items-center px-1.5 py-0.5 rounded text-[10px] font-medium bg-emerald-50 text-emerald-700 border border-emerald-200">
                            <CheckCircle2 className="w-2.5 h-2.5 mr-1" />
                            {log.statusCode}
                          </span>
                        </td>

                      </tr>
                    );
                  })
                ) : (
                  <tr>
                    <td colSpan={9} className="py-8 text-center text-slate-400 text-xs">
                      No telemetry logs match the selected filter criteria.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>

          <div className="p-3 border-t border-[#E2E8F0] bg-[#F8FAFC] flex items-center justify-between text-xs text-slate-500">
            <span>Showing {filteredLogs.length} recorded entries</span>
            <span>Database: PostgreSQL (Supabase Connected)</span>
          </div>

        </section>

      </main>

      {/* ── Section E: Methodology & Footnote Footer ──────────────────── */}
      <footer className="border-t border-[#E2E8F0] bg-white mt-12 py-6 text-xs text-slate-500">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 flex flex-col md:flex-row items-center justify-between gap-4">
          <div>
            <p className="font-medium text-slate-700">
              Department of Electrical Engineering & Information Technology · Faculty of Engineering · Universitas Gadjah Mada
            </p>
            <p className="text-slate-400 text-[11px] mt-0.5">
              Research Prototype for Climate Action & Sustainable Computing. Standard baseline coefficients referenced from IEA and peer-reviewed ML carbon accounting standards.
            </p>
          </div>

          <div className="flex items-center space-x-4 text-slate-600 font-medium">
            <button onClick={() => setShowMethodologyModal(true)} className="hover:text-[#1E3A8A] underline cursor-pointer">
              Methodology Reference
            </button>
            <span>·</span>
            <button onClick={() => setShowApiModal(true)} className="hover:text-[#1E3A8A] underline cursor-pointer">
              Proxy Gateway Specs
            </button>
            <span>·</span>
            <span>Version 2.4.0-Release</span>
          </div>
        </div>
      </footer>

      {/* ── Modal 1: Methodology & Calculation Reference ──────────────── */}
      {showMethodologyModal && (
        <div className="fixed inset-0 z-50 bg-slate-900/40 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-white border border-[#E2E8F0] rounded-lg max-w-2xl w-full p-6 shadow-xl space-y-4">
            <div className="flex items-center justify-between pb-3 border-b border-[#E2E8F0]">
              <h3 className="text-base font-semibold text-[#0F172A] flex items-center">
                <BookOpen className="w-4 h-4 mr-2 text-[#1E3A8A]" />
                Environmental Accounting Methodology & Constants
              </h3>
              <button
                onClick={() => setShowMethodologyModal(false)}
                className="text-slate-400 hover:text-slate-700 text-lg leading-none cursor-pointer"
              >
                ✕
              </button>
            </div>

            <div className="text-xs text-slate-600 space-y-3 leading-relaxed">
              <p>
                The AI Environmental Tracker applies peer-reviewed econometric formulas to translate token throughput into physical ecological impact indicators.
              </p>

              <div className="bg-slate-50 border border-slate-200 rounded p-3 font-mono text-[11px] space-y-1.5 text-slate-800">
                <div><strong>1. Energy Model:</strong> \(E_{\text{kWh}} = \text{TotalTokens} \times 3.0 \times 10^{-7}\text{ kWh/tok}\)</div>
                <div><strong>2. Carbon Model:</strong> \(\text{Carbon}_{\text{g}} = E_{\text{kWh}} \times 400.0\text{ g CO}_2\text{eq/kWh}\)</div>
                <div><strong>3. Water Model:</strong> \(\text{Water}_{\text{mL}} = E_{\text{kWh}} \times 1.50\text{ mL/kWh}\) (WUE)</div>
              </div>

              <h4 className="font-semibold text-slate-800 text-xs mt-3">Primary References:</h4>
              <ul className="list-disc pl-5 space-y-1 text-[11px] text-slate-500">
                <li>International Energy Agency (IEA) — <em>Data Centres and Data Transmission Networks (2023)</em>.</li>
                <li>Luccioni, A. S. et al. — <em>Estimating the Carbon Footprint of Large Language Models (FAccT 2023)</em>.</li>
                <li>Google Sustainability Reports — <em>Data Center Water Usage Effectiveness (WUE) Benchmarks</em>.</li>
              </ul>
            </div>

            <div className="pt-3 border-t border-[#E2E8F0] text-right">
              <button
                onClick={() => setShowMethodologyModal(false)}
                className="px-4 py-1.5 bg-slate-800 text-white text-xs font-semibold rounded hover:bg-slate-900 cursor-pointer"
              >
                Close Reference Panel
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ── Modal 2: API & Gateway Access Guide ────────────────────────── */}
      {showApiModal && (
        <div className="fixed inset-0 z-50 bg-slate-900/40 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-white border border-[#E2E8F0] rounded-lg max-w-2xl w-full p-6 shadow-xl space-y-4">
            <div className="flex items-center justify-between pb-3 border-b border-[#E2E8F0]">
              <h3 className="text-base font-semibold text-[#0F172A] flex items-center">
                <Code2 className="w-4 h-4 mr-2 text-[#1E3A8A]" />
                LLM Observability Gateway Integration
              </h3>
              <button
                onClick={() => setShowApiModal(false)}
                className="text-slate-400 hover:text-slate-700 text-lg leading-none cursor-pointer"
              >
                ✕
              </button>
            </div>

            <div className="text-xs text-slate-600 space-y-3 leading-relaxed">
              <p>
                To enable continuous environmental telemetry in your IDE or application, redirect your OpenAI-compatible base URL to this gateway:
              </p>

              <div className="bg-slate-900 text-slate-100 rounded-md p-3 font-mono text-[11px] space-y-2 overflow-x-auto">
                <div className="text-slate-400"># OpenAI Python SDK Configuration</div>
                <div>
                  <span className="text-pink-400">from</span> openai <span className="text-pink-400">import</span> OpenAI
                </div>
                <div>
                  client = OpenAI(
                </div>
                <div className="pl-4">
                  base_url=<span className="text-emerald-400">&quot;http://localhost:5000/v1&quot;</span>,
                </div>
                <div className="pl-4">
                  api_key=<span className="text-emerald-400">&quot;YOUR_UPSTREAM_KEY&quot;</span>
                </div>
                <div>)</div>
              </div>

              <div className="bg-slate-50 border border-slate-200 rounded p-2.5 text-[11px] text-slate-700">
                <strong>VS Code Cline / Continue:</strong> Set <code>Base URL</code> to <code>http://localhost:5000/v1</code>. The gateway will intercept token metadata transparently without adding latency.
              </div>
            </div>

            <div className="pt-3 border-t border-[#E2E8F0] text-right">
              <button
                onClick={() => setShowApiModal(false)}
                className="px-4 py-1.5 bg-slate-800 text-white text-xs font-semibold rounded hover:bg-slate-900 cursor-pointer"
              >
                Close Integration Panel
              </button>
            </div>
          </div>
        </div>
      )}

    </div>
  );
}
