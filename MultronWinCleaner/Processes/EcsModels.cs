using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace MultronWinCleaner.Processes
{
    public sealed class EcsScanResult
    {
        [JsonPropertyName("type")]
        [JsonProperty("type")]
        public string Type { get; set; } = "result";

        [JsonPropertyName("id")]
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonPropertyName("@timestamp")]
        [JsonProperty("@timestamp")]
        public string? Timestamp { get; set; }

        [JsonPropertyName("ecs")]
        [JsonProperty("ecs")]
        public EcsVersionInfo? Ecs { get; set; }

        [JsonPropertyName("event")]
        [JsonProperty("event")]
        public EcsEventInfo? Event { get; set; }

        [JsonPropertyName("file")]
        [JsonProperty("file")]
        public EcsFileInfo File { get; set; } = new();

        [JsonPropertyName("antivirus")]
        [JsonProperty("antivirus")]
        public EcsAntivirusInfo Antivirus { get; set; } = new();

        [JsonPropertyName("threat")]
        [JsonProperty("threat")]
        public EcsThreatInfo? Threat { get; set; }

        [JsonPropertyName("rule")]
        [JsonProperty("rule")]
        public EcsRuleInfo? Rule { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public string EffectiveVerdict => !string.IsNullOrEmpty(Antivirus.Verdict) ? Antivirus.Verdict : "unknown";

        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public bool IsThreat => EffectiveVerdict.Equals("malicious", StringComparison.OrdinalIgnoreCase)
                             || EffectiveVerdict.Equals("suspicious", StringComparison.OrdinalIgnoreCase);

        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public string ThreatName => Threat?.Indicator?.Name ?? Rule?.Name ?? Antivirus.Detail ?? Loc.T("Threat Detected");

        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public string Sha256Hash => File.Hash?.Sha256 ?? string.Empty;

        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public double ThreatScore => Antivirus.Score;
    }

    public sealed class EcsVersionInfo
    {
        [JsonPropertyName("version")]
        [JsonProperty("version")]
        public string Version { get; set; } = "9.5.4";
    }

    public sealed class EcsEventInfo
    {
        [JsonPropertyName("action")]
        [JsonProperty("action")]
        public string Action { get; set; } = "static_analysis";

        [JsonPropertyName("kind")]
        [JsonProperty("kind")]
        public string Kind { get; set; } = "event";

        [JsonPropertyName("category")]
        [JsonProperty("category")]
        public List<string> Category { get; set; } = new() { "malware", "file" };

        [JsonPropertyName("duration")]
        [JsonProperty("duration")]
        public long Duration { get; set; }

        [JsonPropertyName("outcome")]
        [JsonProperty("outcome")]
        public string Outcome { get; set; } = "success";
    }

    public sealed class EcsFileInfo
    {
        [JsonPropertyName("name")]
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonPropertyName("path")]
        [JsonProperty("path")]
        public string? Path { get; set; }

        [JsonPropertyName("extension")]
        [JsonProperty("extension")]
        public string? Extension { get; set; }

        [JsonPropertyName("size")]
        [JsonProperty("size")]
        public long Size { get; set; }

        [JsonPropertyName("hash")]
        [JsonProperty("hash")]
        public EcsHashInfo? Hash { get; set; }

        [JsonPropertyName("code_signature")]
        [JsonProperty("code_signature")]
        public EcsCodeSignatureInfo? CodeSignature { get; set; }
    }

    public sealed class EcsHashInfo
    {
        [JsonPropertyName("sha256")]
        [JsonProperty("sha256")]
        public string? Sha256 { get; set; }
    }

    public sealed class EcsCodeSignatureInfo
    {
        [JsonPropertyName("exists")]
        [JsonProperty("exists")]
        public bool Exists { get; set; }

        [JsonPropertyName("signed")]
        [JsonProperty("signed")]
        public bool Signed { get; set; }

        [JsonPropertyName("trusted")]
        [JsonProperty("trusted")]
        public bool Trusted { get; set; }

        [JsonPropertyName("subject_name")]
        [JsonProperty("subject_name")]
        public string? SubjectName { get; set; }

        [JsonPropertyName("status")]
        [JsonProperty("status")]
        public string? Status { get; set; }

        [JsonPropertyName("catalog_signed")]
        [JsonProperty("catalog_signed")]
        public bool CatalogSigned { get; set; }
    }

    public sealed class EcsAntivirusInfo
    {
        [JsonPropertyName("engine")]
        [JsonProperty("engine")]
        public string Engine { get; set; } = "VirusKov";

        [JsonPropertyName("verdict")]
        [JsonProperty("verdict")]
        public string Verdict { get; set; } = "clean";

        [JsonPropertyName("score")]
        [JsonProperty("score")]
        public double Score { get; set; }

        [JsonPropertyName("scan_time_ms")]
        [JsonProperty("scan_time_ms")]
        public long ScanTimeMs { get; set; }

        [JsonPropertyName("source")]
        [JsonProperty("source")]
        public string? Source { get; set; }

        [JsonPropertyName("detail")]
        [JsonProperty("detail")]
        public string? Detail { get; set; }

        [JsonPropertyName("detections_count")]
        [JsonProperty("detections_count")]
        public int DetectionsCount { get; set; }

        [JsonPropertyName("detections")]
        [JsonProperty("detections")]
        public List<EcsDetectionItem>? Detections { get; set; }
    }

    public sealed class EcsDetectionItem
    {
        [JsonPropertyName("layer")]
        [JsonProperty("layer")]
        public string Layer { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("score")]
        [JsonProperty("score")]
        public float? Score { get; set; }

        [JsonPropertyName("details")]
        [JsonProperty("details")]
        public string? Details { get; set; }
    }

    public sealed class EcsThreatInfo
    {
        [JsonPropertyName("indicator")]
        [JsonProperty("indicator")]
        public EcsThreatIndicator? Indicator { get; set; }
    }

    public sealed class EcsThreatIndicator
    {
        [JsonPropertyName("type")]
        [JsonProperty("type")]
        public string Type { get; set; } = "file";

        [JsonPropertyName("name")]
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("confidence")]
        [JsonProperty("confidence")]
        public float? Confidence { get; set; }
    }

    public sealed class EcsRuleInfo
    {
        [JsonPropertyName("name")]
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("category")]
        [JsonProperty("category")]
        public string? Category { get; set; }

        [JsonPropertyName("verdict")]
        [JsonProperty("verdict")]
        public string? Verdict { get; set; }
    }
}
