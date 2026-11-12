
using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";

using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

try
{
    // NAC 1: Liet ke bai tap
    Console.WriteLine("Danh sach bai tap dang mo:");

    var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>(
        "/api/assignments");

    foreach (var a in assignments ?? [])
    {
        Console.WriteLine($"- {a.Id}: {a.Title}");
    }

    var selectedAssignmentId = assignments?.FirstOrDefault()?.Id ?? 1;

    // NAC 2: Gui yeu cau nop bai
    var createResponse = await http.PostAsJsonAsync(
        $"/api/assignments/{selectedAssignmentId}/submissions",
        new CreateSubmissionRequest(
            "SV001",
            "https://github.com/SV001/rest-api"));

    Console.WriteLine(
        $"\nTrang thai nop bai: {(int)createResponse.StatusCode} {createResponse.StatusCode}");

    if (createResponse.StatusCode != HttpStatusCode.Created)
    {
        await PrintApiErrorAsync(createResponse);
        return;
    }

    var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();

    Console.WriteLine($"Da tao luot nop: {created?.Id}");
    Console.WriteLine($"Trang thai ban dau: {created?.Status}");

    // NAC 3: Doc lai theo Location
    var location = createResponse.Headers.Location;

    if (location is null)
    {
        Console.WriteLine("Khong tim thay Location trong response.");
        return;
    }

    var readResponse = await http.GetAsync(location);

    if (readResponse.IsSuccessStatusCode)
    {
        var submission = await readResponse.Content.ReadFromJsonAsync<SubmissionDto>();

        Console.WriteLine(
            $"Doc lai luot nop: {submission?.StudentCode} - {submission?.RepositoryUrl}");
    }
    else
    {
        await PrintApiErrorAsync(readResponse);
    }

    // NAC 4: Gui yeu cau sai
    var invalidResponse = await http.PostAsJsonAsync(
        $"/api/assignments/{selectedAssignmentId}/submissions",
        new CreateSubmissionRequest("", ""));

    Console.WriteLine(
        $"\nThu gui yeu cau loi: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");

    await PrintApiErrorAsync(invalidResponse);
}
catch (HttpRequestException ex)
{
    Console.WriteLine("Khong the ket noi den API.");
    Console.WriteLine(ex.Message);
}

static async Task PrintApiErrorAsync(HttpResponseMessage response)
{
    if (response.IsSuccessStatusCode)
        return;

    var error = await response.Content.ReadFromJsonAsync<ApiError>();

    Console.WriteLine($"{error?.Code}: {error?.Message}");
}

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);

record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);

record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt);

record ApiError(string Code, string Message);
