using System.Text.Json;
using SmartHospital.Api.Services.Agent4;
using Xunit;

namespace SmartHospital.Tests.Unit.Services;

public class Agent4RecommendationsTests
{
    private static JsonElement Stored(JsonElement? raw) =>
        JsonSerializer.SerializeToElement(Agent4MedicalReportService.BuildRecommendations(raw), new JsonSerializerOptions(JsonSerializerDefaults.Web));

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement.Clone();

    [Fact]
    public void MissingRecommendations_StoreTheNotAvailableMarker()
    {
        var stored = Stored(null);

        Assert.False(stored.GetProperty("available").GetBoolean());
        Assert.Equal(Agent4MedicalReportService.RecommendationsUnavailableMessage, stored.GetProperty("message").GetString());
    }

    [Theory]
    [InlineData("[\"eat well\"]")]
    [InlineData("\"eat well\"")]
    [InlineData("{\"lifestyle\": [1, null, \"\"], \"diet\": \"salad\"}")]
    public void InvalidOrEmptyRecommendations_StoreTheNotAvailableMarker(string json)
    {
        Assert.False(Stored(Parse(json)).GetProperty("available").GetBoolean());
    }

    [Fact]
    public void ValidRecommendations_AreStoredWithOnlyKnownFields()
    {
        var stored = Stored(Parse("""
            {
              "basis": "condition_specific",
              "diet": { "prefer": ["Vegetables."], "avoid": ["Salty snacks."] },
              "exercise": { "suitable": ["Walking."], "avoid": [] },
              "lifestyle": ["Sleep well."],
              "warningSigns": ["Chest pain: seek emergency care."],
              "followUp": { "timeframe": "about 1 month", "monitoring": ["Blood pressure."] },
              "note": "",
              "unexpected": "dropped"
            }
            """));

        Assert.True(stored.GetProperty("available").GetBoolean());
        Assert.Equal("condition_specific", stored.GetProperty("basis").GetString());
        Assert.Equal("Salty snacks.", stored.GetProperty("diet").GetProperty("avoid")[0].GetString());
        Assert.Equal("about 1 month", stored.GetProperty("followUp").GetProperty("timeframe").GetString());
        Assert.False(stored.TryGetProperty("unexpected", out _));
    }

    [Fact]
    public void UnsafeOrOversizedItems_AreDroppedAndListsCapped()
    {
        var items = string.Join(",", Enumerable.Range(1, 9).Select(i => $"\"Tip {i}.\""));
        var stored = Stored(Parse($$"""
            { "basis": "<b>x</b>", "lifestyle": ["<script>x</script>", "{{new string('a', 300)}}", {{items}}] }
            """));

        var lifestyle = stored.GetProperty("lifestyle").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Equal(5, lifestyle.Count);
        Assert.Equal("Tip 1.", lifestyle[0]);
        Assert.Equal("general", stored.GetProperty("basis").GetString());
    }
}
