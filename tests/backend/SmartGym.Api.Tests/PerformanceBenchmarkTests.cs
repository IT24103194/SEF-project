using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartGym.Api.Data;
using SmartGym.Api.DTOs.Auth;
using Xunit;
using Xunit.Abstractions;

namespace SmartGym.Api.Tests;

public class PerformanceBenchmarkTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public PerformanceBenchmarkTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _output = output;
    }

    private async Task<string> GetAdminTokenAsync()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = "admin@smartgym.com",
            Password = "Admin123!"
        });
        loginResponse.EnsureSuccessStatusCode();
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);
        return auth!.AccessToken;
    }

    [Fact]
    public async Task Benchmark_ConcurrentHealthRequests_MeasuresResponseTimeAndSuccessRate()
    {
        const int concurrentRequests = 50;
        var tasks = new List<Task<(long ElapsedMs, HttpStatusCode StatusCode)>>();

        var swOverall = Stopwatch.StartNew();

        for (int i = 0; i < concurrentRequests; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var sw = Stopwatch.StartNew();
                var response = await _client.GetAsync("/health");
                sw.Stop();
                return (sw.ElapsedMilliseconds, response.StatusCode);
            }));
        }

        var results = await Task.WhenAll(tasks);
        swOverall.Stop();

        var successCount = results.Count(r => r.StatusCode == HttpStatusCode.OK);
        var failureCount = results.Length - successCount;
        var successRate = (double)successCount / results.Length * 100.0;
        var failureRate = (double)failureCount / results.Length * 100.0;

        var latencies = results.Select(r => (double)r.ElapsedMs).OrderBy(l => l).ToList();
        var minMs = latencies.Min();
        var maxMs = latencies.Max();
        var meanMs = latencies.Average();
        var p50Ms = latencies[(int)(latencies.Count * 0.50)];
        var p95Ms = latencies[(int)(latencies.Count * 0.95)];

        _output.WriteLine("=== HEALTH ENDPOINT CONCURRENT BENCHMARK ===");
        _output.WriteLine($"Concurrent Requests: {concurrentRequests}");
        _output.WriteLine($"Total Duration:      {swOverall.ElapsedMilliseconds} ms");
        _output.WriteLine($"Success Rate:        {successRate:F2}% ({successCount}/{results.Length})");
        _output.WriteLine($"Failure Rate:        {failureRate:F2}%");
        _output.WriteLine($"Latency Min:         {minMs} ms");
        _output.WriteLine($"Latency Mean:        {meanMs:F2} ms");
        _output.WriteLine($"Latency P50:         {p50Ms} ms");
        _output.WriteLine($"Latency P95:         {p95Ms} ms");
        _output.WriteLine($"Latency Max:         {maxMs} ms");

        Assert.Equal(100.0, successRate);
        Assert.Equal(0.0, failureRate);
        Assert.True(meanMs < 500, $"Average latency {meanMs}ms exceeded 500ms SLA.");
    }

    [Fact]
    public async Task Benchmark_PostgresDatabaseQueryLatency_MeasuresRoundtripTime()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartGymDbContext>();

        const int iterations = 30;
        var latencies = new List<long>();

        // Warmup
        await db.FacilityIssues.Take(5).ToListAsync();

        for (int i = 0; i < iterations; i++)
        {
            var sw = Stopwatch.StartNew();
            // Complex join query across FacilityIssues, Locations, and Equipment
            var count = await db.FacilityIssues
                .AsNoTracking()
                .Include(f => f.Location)
                .Include(f => f.Equipment)
                .CountAsync();
            sw.Stop();
            latencies.Add(sw.ElapsedMilliseconds);
        }

        latencies.Sort();
        var minMs = latencies.Min();
        var maxMs = latencies.Max();
        var meanMs = latencies.Average();
        var p50Ms = latencies[(int)(latencies.Count * 0.50)];
        var p95Ms = latencies[(int)(latencies.Count * 0.95)];

        _output.WriteLine("=== POSTGRESQL QUERY LATENCY BENCHMARK ===");
        _output.WriteLine($"Sample Iterations:   {iterations}");
        _output.WriteLine($"DB Query Latency Min:{minMs} ms");
        _output.WriteLine($"DB Query Latency Mean:{meanMs:F2} ms");
        _output.WriteLine($"DB Query Latency P50: {p50Ms} ms");
        _output.WriteLine($"DB Query Latency P95: {p95Ms} ms");
        _output.WriteLine($"DB Query Latency Max: {maxMs} ms");

        Assert.True(meanMs < 100, $"Database mean latency {meanMs}ms exceeded 100ms threshold.");
    }

    [Fact]
    public async Task Benchmark_AuthenticatedFacilityIssuesEndpoint_MeasuresConcurrentLoad()
    {
        var token = await GetAdminTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        const int concurrentRequests = 25;
        var tasks = new List<Task<(long ElapsedMs, HttpStatusCode StatusCode)>>();

        for (int i = 0; i < concurrentRequests; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var sw = Stopwatch.StartNew();
                var response = await _client.GetAsync("/api/facility-issues?pageNumber=1&pageSize=10");
                sw.Stop();
                return (sw.ElapsedMilliseconds, response.StatusCode);
            }));
        }

        var results = await Task.WhenAll(tasks);

        var successCount = results.Count(r => r.StatusCode == HttpStatusCode.OK);
        var successRate = (double)successCount / results.Length * 100.0;
        var latencies = results.Select(r => (double)r.ElapsedMs).OrderBy(l => l).ToList();

        _output.WriteLine("=== AUTHENTICATED FACILITY ISSUES BENCHMARK ===");
        _output.WriteLine($"Concurrent Requests: {concurrentRequests}");
        _output.WriteLine($"Success Rate:        {successRate:F2}% ({successCount}/{results.Length})");
        _output.WriteLine($"Latency P50:         {latencies[(int)(latencies.Count * 0.50)]} ms");
        _output.WriteLine($"Latency P95:         {latencies[(int)(latencies.Count * 0.95)]} ms");

        Assert.True(successRate >= 95.0, $"Success rate {successRate}% was below 95%.");
    }
}
