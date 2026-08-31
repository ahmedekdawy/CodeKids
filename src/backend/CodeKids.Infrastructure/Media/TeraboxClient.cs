using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Media;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CodeKids.Infrastructure.Media;

public sealed class TeraboxClient
{
    private static readonly TimeSpan TokenRefreshInterval = TimeSpan.FromHours(6);

    private readonly TeraboxOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TeraboxClient> _logger;
    private readonly TeraboxOAuthTokenManager _oauthTokenManager;
    private readonly string _cookieHeader;
    private readonly string _dpLogId;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private string _jsToken;
    private string _bdsToken;
    private DateTimeOffset _tokensRefreshedAt;

    public TeraboxClient(
        IOptions<TeraboxOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<TeraboxClient> logger,
        TeraboxOAuthTokenManager oauthTokenManager)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _oauthTokenManager = oauthTokenManager;
        _dpLogId = Random.Shared.NextInt64(100000000000L, 999999999999L).ToString();
        _cookieHeader = BuildCookieHeader(_options);
        _jsToken = _options.JsToken;
        _bdsToken = _options.BdsToken;
    }

    public bool IsConfigured =>
        _oauthTokenManager.IsEnabled
        || (!string.IsNullOrWhiteSpace(_options.Ndus) && !string.IsNullOrWhiteSpace(_options.JsToken));

    public async Task<TeraboxUploadResult> UploadAsync(
        string localFilePath,
        string remoteDirectory,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        if (_oauthTokenManager.IsEnabled)
        {
            try
            {
                return await UploadCoreAsync(localFilePath, remoteDirectory, fileName, cancellationToken);
            }
            catch (TeraboxApiException ex) when (IsAccessTokenExpired(ex.Errno))
            {
                _logger.LogWarning("Terabox OAuth access token expired (errno {Errno}); refreshing and retrying upload.", ex.Errno);
                await _oauthTokenManager.RefreshAsync(cancellationToken);
                return await UploadCoreAsync(localFilePath, remoteDirectory, fileName, cancellationToken);
            }
        }

        await EnsureFreshSessionAsync(cancellationToken);

        try
        {
            return await UploadCoreAsync(localFilePath, remoteDirectory, fileName, cancellationToken);
        }
        catch (TeraboxApiException ex) when (IsVerificationRequired(ex.Errno, ex.Errmsg))
        {
            _logger.LogWarning(
                "Terabox {Operation} requires verification (errno {Errno}); refreshing session tokens and retrying upload.",
                ex.Operation,
                ex.Errno);
            await RefreshSessionTokensAsync(force: true, cancellationToken);
            return await UploadCoreAsync(localFilePath, remoteDirectory, fileName, cancellationToken);
        }
    }

    private async Task<TeraboxUploadResult> UploadCoreAsync(
        string localFilePath,
        string remoteDirectory,
        string fileName,
        CancellationToken cancellationToken)
    {
        if (_oauthTokenManager.IsEnabled)
        {
            return await UploadOAuthAsync(localFilePath, remoteDirectory, fileName, cancellationToken);
        }

        return await UploadSessionAsync(localFilePath, remoteDirectory, fileName, cancellationToken);
    }

    private async Task<TeraboxUploadResult> UploadOAuthAsync(
        string localFilePath,
        string remoteDirectory,
        string fileName,
        CancellationToken cancellationToken)
    {
        var session = await _oauthTokenManager.GetSessionAsync(cancellationToken);
        var directory = NormalizeDirectory(remoteDirectory);
        await EnsureDirectoryAsync(directory, cancellationToken);

        var remotePath = $"{directory}/{fileName}";
        var fileInfo = new FileInfo(localFilePath);
        var fileSize = fileInfo.Length;
        var fileMd5 = await ComputeMd5HexAsync(localFilePath, cancellationToken);
        var modifiedUnix = new DateTimeOffset(fileInfo.LastWriteTimeUtc).ToUnixTimeSeconds();

        var precreateUrl = BuildOAuthApiUrl(session, "/openapi/api/precreate");
        var precreateBody = new Dictionary<string, string>
        {
            ["path"] = remotePath,
            ["autoinit"] = "1",
            ["target_path"] = directory,
            ["block_list"] = JsonSerializer.Serialize(new[] { fileMd5 }),
            ["size"] = fileSize.ToString(),
            ["local_mtime"] = modifiedUnix.ToString()
        };

        var precreate = await PostOAuthFormAsync<TeraboxPrecreateResponse>(precreateUrl, precreateBody, cancellationToken);
        EnsureSuccess(precreate?.Errno, precreate?.Errmsg, "precreate");
        if (string.IsNullOrWhiteSpace(precreate!.UploadId))
        {
            throw new InvalidOperationException("Terabox precreate did not return an upload id.");
        }

        var uploadUrl =
            $"{session.UploadDomain}/rest/2.0/pcs/superfile2?method=upload&app_id={Uri.EscapeDataString(_options.AppId)}&path=%2F{Uri.EscapeDataString(fileName)}&uploadid={Uri.EscapeDataString(precreate.UploadId)}&partseq=0&access_tokens={Uri.EscapeDataString(session.AccessToken)}";

        await UploadFileContentOAuthAsync(uploadUrl, localFilePath, cancellationToken);

        var createUrl = BuildOAuthApiUrl(session, "/openapi/api/create");
        var createBody = new Dictionary<string, string>
        {
            ["path"] = remotePath,
            ["size"] = fileSize.ToString(),
            ["uploadid"] = precreate.UploadId,
            ["target_path"] = directory,
            ["block_list"] = JsonSerializer.Serialize(new[] { fileMd5 }),
            ["local_mtime"] = modifiedUnix.ToString(),
            ["isdir"] = "0",
            ["rtype"] = "1"
        };

        var create = await PostOAuthFormAsync<TeraboxCreateResponse>(createUrl, createBody, cancellationToken);
        EnsureSuccess(create?.Errno, create?.Errmsg, "create");
        if (create!.FsId <= 0)
        {
            throw new InvalidOperationException("Terabox create did not return a file id.");
        }

        return new TeraboxUploadResult(create.FsId, remotePath);
    }

    private async Task<TeraboxUploadResult> UploadSessionAsync(
        string localFilePath,
        string remoteDirectory,
        string fileName,
        CancellationToken cancellationToken)
    {
        var directory = NormalizeDirectory(remoteDirectory);
        await EnsureDirectoryAsync(directory, cancellationToken);

        var remotePath = $"{directory}/{fileName}";
        var fileInfo = new FileInfo(localFilePath);
        var fileSize = fileInfo.Length;
        var fileMd5 = await ComputeMd5HexAsync(localFilePath, cancellationToken);
        var modifiedUnix = new DateTimeOffset(fileInfo.LastWriteTimeUtc).ToUnixTimeSeconds();

        var precreateUrl = BuildApiUrl("/api/precreate");
        var precreateBody = new Dictionary<string, string>
        {
            ["path"] = remotePath,
            ["autoinit"] = "1",
            ["target_path"] = directory,
            ["block_list"] = JsonSerializer.Serialize(new[] { fileMd5 }),
            ["size"] = fileSize.ToString(),
            ["local_mtime"] = modifiedUnix.ToString()
        };

        var precreate = await PostFormAsync<TeraboxPrecreateResponse>(precreateUrl, precreateBody, cancellationToken);
        EnsureSuccess(precreate?.Errno, precreate?.Errmsg, "precreate");
        if (string.IsNullOrWhiteSpace(precreate!.UploadId))
        {
            throw new InvalidOperationException("Terabox precreate did not return an upload id.");
        }

        var uploadUrl =
            $"https://c-jp.1024terabox.com/rest/2.0/pcs/superfile2?method=upload&app_id={Uri.EscapeDataString(_options.AppId)}&channel=dubox&clienttype=0&web=1&path=%2F{Uri.EscapeDataString(fileName)}&uploadid={Uri.EscapeDataString(precreate.UploadId)}&uploadsign=0&partseq=0";

        await UploadFileContentAsync(uploadUrl, localFilePath, cancellationToken);

        var createUrl = BuildApiUrl("/api/create");
        var createBody = new Dictionary<string, string>
        {
            ["path"] = remotePath,
            ["size"] = fileSize.ToString(),
            ["uploadid"] = precreate.UploadId,
            ["target_path"] = directory,
            ["block_list"] = JsonSerializer.Serialize(new[] { fileMd5 }),
            ["local_mtime"] = modifiedUnix.ToString(),
            ["isdir"] = "0",
            ["rtype"] = "1"
        };
        AppendBdsToken(createBody);

        var create = await PostFormAsync<TeraboxCreateResponse>(createUrl, createBody, cancellationToken);
        EnsureSuccess(create?.Errno, create?.Errmsg, "create");
        if (create!.FsId <= 0)
        {
            throw new InvalidOperationException("Terabox create did not return a file id.");
        }

        return new TeraboxUploadResult(create.FsId, remotePath);
    }

    public async Task<string> GetDirectLinkAsync(long fsId, CancellationToken cancellationToken = default)
    {
        var link = await GetDirectLinkByFsIdAsync(fsId, cancellationToken);
        return TeraboxDisplayUrl.NormalizePlaybackUrl(link, _options.BaseUrl);
    }

    private async Task<string> GetRawDirectLinkAsync(long fsId, string? remotePath, CancellationToken cancellationToken = default)
    {
        try
        {
            return await GetDirectLinkByFsIdAsync(fsId, cancellationToken);
        }
        catch (Exception ex) when (!string.IsNullOrWhiteSpace(remotePath))
        {
            _logger.LogWarning(ex, "Terabox download by fs_id {FsId} failed, trying file path {RemotePath}", fsId, remotePath);
            return await GetDirectLinkByPathAsync(remotePath, cancellationToken);
        }
    }

    private async Task<string> GetDirectLinkByFsIdAsync(long fsId, CancellationToken cancellationToken)
    {
        EnsureConfigured();
        if (_oauthTokenManager.IsEnabled)
        {
            return await GetDirectLinkByFsIdOAuthAsync(fsId, cancellationToken);
        }

        var home = await GetHomeInfoAsync(cancellationToken);
        var sign = GenerateSign(home.Sign3, home.Sign1);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var downloadUrl = BuildApiUrl("/api/download") +
                          $"&type=dlink&fidlist=[{fsId}]&sign={Uri.EscapeDataString(sign)}&vip=2&timestamp={timestamp}";

        using var client = CreateClient();
        using var response = await client.GetAsync(downloadUrl, cancellationToken);
        var json = await ReadResponseBodyAsync(response.Content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Terabox download lookup failed ({(int)response.StatusCode}): {json}");
        }

        var payload = JsonSerializer.Deserialize<TeraboxDownloadResponse>(json);
        EnsureSuccess(payload?.Errno, payload?.Errmsg, "download");
        return ExtractDirectLink(payload, json);
    }

    private async Task<string> GetDirectLinkByPathAsync(string remotePath, CancellationToken cancellationToken)
    {
        EnsureConfigured();
        if (_oauthTokenManager.IsEnabled)
        {
            return await GetDirectLinkByPathOAuthAsync(remotePath, cancellationToken);
        }

        var target = JsonSerializer.Serialize(new[] { remotePath });
        var url = BuildApiUrl("/api/filemetas") +
                  $"&target={Uri.EscapeDataString(target)}&dlink=1&origin=dlna";

        using var client = CreateClient();
        using var response = await client.GetAsync(url, cancellationToken);
        var json = await ReadResponseBodyAsync(response.Content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Terabox filemetas lookup failed ({(int)response.StatusCode}): {json}");
        }

        var payload = JsonSerializer.Deserialize<TeraboxFileMetasResponse>(json);
        EnsureSuccess(payload?.Errno, payload?.Errmsg, "filemetas");
        var link = payload?.Info?.FirstOrDefault()?.Dlink;
        if (string.IsNullOrWhiteSpace(link))
        {
            throw new InvalidOperationException($"Terabox filemetas did not return a download link: {json}");
        }

        return NormalizeRawLink(link);
    }

    private static string ExtractDirectLink(TeraboxDownloadResponse? payload, string json)
    {
        var link = payload?.Dlink?.FirstOrDefault()?.Dlink
            ?? payload?.List?.FirstOrDefault()?.Dlink;
        if (string.IsNullOrWhiteSpace(link))
        {
            throw new InvalidOperationException($"Terabox did not return a download link: {json}");
        }

        return NormalizeRawLink(link);
    }

    private static string NormalizeRawLink(string link)
    {
        if (link.StartsWith("//", StringComparison.Ordinal))
        {
            return $"https:{link}";
        }

        return link;
    }

    public async Task<Stream> OpenReadAsync(long fsId, string? remotePath = null, CancellationToken cancellationToken = default)
    {
        var link = await GetRawDirectLinkAsync(fsId, remotePath, cancellationToken);
        var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, link);
        PrepareDownloadRequest(request);
        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (response.StatusCode is System.Net.HttpStatusCode.Redirect or System.Net.HttpStatusCode.MovedPermanently or System.Net.HttpStatusCode.Found or System.Net.HttpStatusCode.SeeOther or System.Net.HttpStatusCode.TemporaryRedirect)
        {
            var redirectUrl = response.Headers.Location?.ToString();
            response.Dispose();
            if (string.IsNullOrWhiteSpace(redirectUrl))
            {
                client.Dispose();
                throw new InvalidOperationException("Terabox file download redirect had no location.");
            }

            if (redirectUrl.StartsWith("//", StringComparison.Ordinal))
            {
                redirectUrl = $"https:{redirectUrl}";
            }

            using var redirectRequest = new HttpRequestMessage(HttpMethod.Get, redirectUrl);
            PrepareDownloadRequest(redirectRequest);
            response = await client.SendAsync(redirectRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }

        if (!response.IsSuccessStatusCode)
        {
            response.Dispose();
            client.Dispose();
            throw new InvalidOperationException($"Terabox file download failed ({(int)response.StatusCode}).");
        }

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return new HttpDependencyStream(stream, response, client);
    }

    public async Task DeleteAsync(string remotePath, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        if (_oauthTokenManager.IsEnabled)
        {
            await DeleteOAuthAsync(remotePath, cancellationToken);
            return;
        }

        var url = BuildApiUrl("/api/filemanager") + "&opera=delete&async=2&onnest=fail";
        var body = new Dictionary<string, string>
        {
            ["filelist"] = JsonSerializer.Serialize(new[] { remotePath })
        };
        AppendBdsToken(body);

        var result = await PostFormAsync<TeraboxSimpleResponse>(url, body, cancellationToken);
        EnsureSuccess(result?.Errno, result?.Errmsg, "delete");
    }

    public Task<bool> ExistsAsync(string remotePath, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var directory = NormalizeDirectory(Path.GetDirectoryName(remotePath)?.Replace('\\', '/') ?? "/");
        var fileName = Path.GetFileName(remotePath);
        return FileExistsInDirectoryAsync(directory, fileName, cancellationToken);
    }

    private async Task EnsureDirectoryAsync(string directoryPath, CancellationToken cancellationToken)
    {
        if (directoryPath == "/")
        {
            return;
        }

        var segments = directoryPath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        var current = string.Empty;
        foreach (var segment in segments)
        {
            current += $"/{segment}";
            if (await DirectoryExistsAsync(current, cancellationToken))
            {
                continue;
            }

            var createUrl = _oauthTokenManager.IsEnabled
                ? BuildOAuthApiUrl(await _oauthTokenManager.GetSessionAsync(cancellationToken), "/openapi/api/create")
                : BuildApiUrl("/api/create");
            var body = new Dictionary<string, string>
            {
                ["path"] = current,
                ["isdir"] = "1",
                ["size"] = "0",
                ["block_list"] = "[]",
                ["local_mtime"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()
            };
            if (!_oauthTokenManager.IsEnabled)
            {
                AppendBdsToken(body);
            }

            var result = _oauthTokenManager.IsEnabled
                ? await PostOAuthFormAsync<TeraboxSimpleResponse>(createUrl, body, cancellationToken)
                : await PostFormAsync<TeraboxSimpleResponse>(createUrl, body, cancellationToken);
            if (result?.Errno is not (0 or -8))
            {
                EnsureSuccess(result?.Errno, result?.Errmsg, $"create directory {current}");
            }
        }
    }

    private async Task<bool> DirectoryExistsAsync(string directoryPath, CancellationToken cancellationToken)
    {
        var list = await ListDirectoryAsync(directoryPath, cancellationToken);
        return list?.Errno == 0;
    }

    private async Task<bool> FileExistsInDirectoryAsync(
        string directoryPath,
        string fileName,
        CancellationToken cancellationToken)
    {
        var list = await ListDirectoryAsync(directoryPath, cancellationToken);
        return list?.List?.Any(x => string.Equals(x.ServerFilename, fileName, StringComparison.OrdinalIgnoreCase)) == true;
    }

    private async Task<TeraboxListResponse?> ListDirectoryAsync(string directoryPath, CancellationToken cancellationToken)
    {
        if (_oauthTokenManager.IsEnabled)
        {
            return await ListDirectoryOAuthAsync(directoryPath, cancellationToken);
        }

        var url = BuildApiUrl("/api/list") +
                  $"&order=time&desc=1&dir={Uri.EscapeDataString(NormalizeDirectory(directoryPath))}&num=100&page=1&showempty=0";
        using var client = CreateClient();
        using var response = await client.GetAsync(url, cancellationToken);
        var json = await ReadResponseBodyAsync(response.Content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Terabox list failed for {Directory}: {Status} {Body}", directoryPath, (int)response.StatusCode, json);
            return null;
        }

        return JsonSerializer.Deserialize<TeraboxListResponse>(json);
    }

    private async Task<TeraboxHomeInfo> GetHomeInfoAsync(CancellationToken cancellationToken)
    {
        var url = BuildApiUrl("/api/home/info");
        using var client = CreateClient();
        using var response = await client.GetAsync(url, cancellationToken);
        var json = await ReadResponseBodyAsync(response.Content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Terabox home info failed ({(int)response.StatusCode}): {json}");
        }

        var payload = JsonSerializer.Deserialize<TeraboxHomeInfoResponse>(json);
        EnsureSuccess(payload?.Errno, payload?.Errmsg, "home info");
        if (payload?.Data is null
            || string.IsNullOrWhiteSpace(payload.Data.Sign1)
            || string.IsNullOrWhiteSpace(payload.Data.Sign3)
            || payload.Data.Timestamp <= 0)
        {
            throw new InvalidOperationException("Terabox home info response was incomplete.");
        }

        return payload.Data;
    }

    private async Task UploadFileContentAsync(string uploadUrl, string localFilePath, CancellationToken cancellationToken)
    {
        await using var fileStream = File.OpenRead(localFilePath);
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent, "file", Path.GetFileName(localFilePath));

        using var client = CreateClient();
        using var response = await client.PostAsync(uploadUrl, content, cancellationToken);
        var body = await ReadResponseBodyAsync(response.Content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Terabox upload failed ({(int)response.StatusCode}): {body}");
        }
    }

    private async Task<T?> PostFormAsync<T>(string url, Dictionary<string, string> body, CancellationToken cancellationToken)
    {
        using var client = CreateClient();
        using var content = new FormUrlEncodedContent(body);
        using var response = await client.PostAsync(url, content, cancellationToken);
        var json = await ReadResponseBodyAsync(response.Content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Terabox request failed ({(int)response.StatusCode}): {json}");
        }

        return JsonSerializer.Deserialize<T>(json);
    }

    private static async Task<string> ReadResponseBodyAsync(HttpContent content, CancellationToken cancellationToken)
    {
        var bytes = await content.ReadAsByteArrayAsync(cancellationToken);
        if (bytes.Length == 0)
        {
            return string.Empty;
        }

        // Terabox often returns JSON with an invalid/missing charset in Content-Type.
        return Encoding.UTF8.GetString(bytes);
    }

    private HttpClient CreateClient()
    {
        var client = _httpClientFactory.CreateClient(nameof(TeraboxClient));
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json, text/plain, */*");
        client.DefaultRequestHeaders.Remove("Cookie");
        client.DefaultRequestHeaders.Add("Cookie", _cookieHeader);
        client.DefaultRequestHeaders.Remove("Referer");
        client.DefaultRequestHeaders.Add("Referer", $"{_options.BaseUrl.TrimEnd('/')}/");
        client.DefaultRequestHeaders.Remove("X-Requested-With");
        client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        return client;
    }

    private void PrepareDownloadRequest(HttpRequestMessage request)
    {
        request.Headers.Remove("Cookie");
        request.Headers.Add("Cookie", _cookieHeader);
        request.Headers.Remove("Referer");
        request.Headers.Add("Referer", $"{_options.BaseUrl.TrimEnd('/')}/");
        request.Headers.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    }

    private string BuildApiUrl(string path)
    {
        var baseUrl = _options.BaseUrl.TrimEnd('/');
        return $"{baseUrl}{path}?app_id={Uri.EscapeDataString(_options.AppId)}&web=1&channel=dubox&clienttype=0&jsToken={Uri.EscapeDataString(_jsToken)}&dp-logid={_dpLogId}";
    }

    private void AppendBdsToken(Dictionary<string, string> body)
    {
        if (!string.IsNullOrWhiteSpace(_bdsToken))
        {
            body["bdstoken"] = _bdsToken;
        }
    }

    private static string BuildOAuthApiUrl(TeraboxOAuthSession session, string path) =>
        $"{session.ApiDomain}{path}?access_tokens={Uri.EscapeDataString(session.AccessToken)}";

    private async Task<T?> PostOAuthFormAsync<T>(string url, Dictionary<string, string> body, CancellationToken cancellationToken)
    {
        using var client = CreateOAuthClient();
        using var content = new FormUrlEncodedContent(body);
        using var response = await client.PostAsync(url, content, cancellationToken);
        var json = await ReadResponseBodyAsync(response.Content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Terabox request failed ({(int)response.StatusCode}): {json}");
        }

        return JsonSerializer.Deserialize<T>(json);
    }

    private async Task UploadFileContentOAuthAsync(string uploadUrl, string localFilePath, CancellationToken cancellationToken)
    {
        await using var fileStream = File.OpenRead(localFilePath);
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent, "file", Path.GetFileName(localFilePath));

        using var client = CreateOAuthClient();
        using var response = await client.PostAsync(uploadUrl, content, cancellationToken);
        var body = await ReadResponseBodyAsync(response.Content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Terabox upload failed ({(int)response.StatusCode}): {body}");
        }
    }

    private HttpClient CreateOAuthClient()
    {
        var client = _httpClientFactory.CreateClient(nameof(TeraboxClient));
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json, text/plain, */*");
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        return client;
    }

    private async Task<string> GetDirectLinkByFsIdOAuthAsync(long fsId, CancellationToken cancellationToken)
    {
        var session = await _oauthTokenManager.GetSessionAsync(cancellationToken);
        var downloadUrl = BuildOAuthApiUrl(session, "/openapi/api/download") +
                          $"&fidlist=[{fsId}]&type=dlink";

        using var client = CreateOAuthClient();
        using var response = await client.GetAsync(downloadUrl, cancellationToken);
        var json = await ReadResponseBodyAsync(response.Content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Terabox download lookup failed ({(int)response.StatusCode}): {json}");
        }

        var payload = JsonSerializer.Deserialize<TeraboxDownloadResponse>(json);
        EnsureSuccess(payload?.Errno, payload?.Errmsg, "download");
        return ExtractDirectLink(payload, json);
    }

    private async Task<string> GetDirectLinkByPathOAuthAsync(string remotePath, CancellationToken cancellationToken)
    {
        var session = await _oauthTokenManager.GetSessionAsync(cancellationToken);
        var target = JsonSerializer.Serialize(new[] { remotePath });
        var url = BuildOAuthApiUrl(session, "/openapi/api/filemetas") +
                  $"&target={Uri.EscapeDataString(target)}&dlink=1";

        using var client = CreateOAuthClient();
        using var response = await client.GetAsync(url, cancellationToken);
        var json = await ReadResponseBodyAsync(response.Content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Terabox filemetas lookup failed ({(int)response.StatusCode}): {json}");
        }

        var payload = JsonSerializer.Deserialize<TeraboxFileMetasResponse>(json);
        EnsureSuccess(payload?.Errno, payload?.Errmsg, "filemetas");
        var link = payload?.Info?.FirstOrDefault()?.Dlink;
        if (string.IsNullOrWhiteSpace(link))
        {
            throw new InvalidOperationException($"Terabox filemetas did not return a download link: {json}");
        }

        return NormalizeRawLink(link);
    }

    private async Task DeleteOAuthAsync(string remotePath, CancellationToken cancellationToken)
    {
        var session = await _oauthTokenManager.GetSessionAsync(cancellationToken);
        var url = BuildOAuthApiUrl(session, "/openapi/api/filemanager") + "&opera=delete&async=2&onnest=fail";
        var body = new Dictionary<string, string>
        {
            ["filelist"] = JsonSerializer.Serialize(new[] { remotePath })
        };

        var result = await PostOAuthFormAsync<TeraboxSimpleResponse>(url, body, cancellationToken);
        EnsureSuccess(result?.Errno, result?.Errmsg, "delete");
    }

    private async Task<TeraboxListResponse?> ListDirectoryOAuthAsync(string directoryPath, CancellationToken cancellationToken)
    {
        var session = await _oauthTokenManager.GetSessionAsync(cancellationToken);
        var url = BuildOAuthApiUrl(session, "/openapi/api/list") +
                  $"&order=time&desc=1&dir={Uri.EscapeDataString(NormalizeDirectory(directoryPath))}&num=100&page=1&showempty=0";
        using var client = CreateOAuthClient();
        using var response = await client.GetAsync(url, cancellationToken);
        var json = await ReadResponseBodyAsync(response.Content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Terabox list failed for {Directory}: {Status} {Body}", directoryPath, (int)response.StatusCode, json);
            return null;
        }

        return JsonSerializer.Deserialize<TeraboxListResponse>(json);
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Terabox credentials are not configured.");
        }
    }

    private static void EnsureSuccess(int? errno, string? errmsg, string operation)
    {
        if (errno is 0)
        {
            return;
        }

        throw new TeraboxApiException(errno ?? 0, errmsg, operation);
    }

    private async Task EnsureFreshSessionAsync(CancellationToken cancellationToken)
    {
        if (_tokensRefreshedAt != default &&
            DateTimeOffset.UtcNow - _tokensRefreshedAt < TokenRefreshInterval)
        {
            return;
        }

        await RefreshSessionTokensAsync(force: false, cancellationToken);
    }

    private async Task RefreshSessionTokensAsync(bool force, CancellationToken cancellationToken)
    {
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (!force &&
                _tokensRefreshedAt != default &&
                DateTimeOffset.UtcNow - _tokensRefreshedAt < TokenRefreshInterval)
            {
                return;
            }

            var baseUrl = _options.BaseUrl.TrimEnd('/');
            var mainUrl = $"{baseUrl}/main?category=all";
            using var client = CreateClient();
            using var response = await client.GetAsync(mainUrl, cancellationToken);
            var html = await ReadResponseBodyAsync(response.Content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Terabox token refresh failed ({Status}): {Body}",
                    (int)response.StatusCode,
                    html);
                if (force)
                {
                    throw new InvalidOperationException(
                        "Terabox session expired and could not be refreshed automatically. Log in at terabox.com, complete verification, and update Ndus/JsToken in server config.");
                }

                return;
            }

            var jsToken = ExtractJsToken(html);
            var bdsToken = ExtractBdsToken(html);
            if (string.IsNullOrWhiteSpace(jsToken))
            {
                _logger.LogWarning("Terabox token refresh did not find jsToken in main page HTML.");
                if (force)
                {
                    throw new InvalidOperationException(
                        "Terabox session expired and could not be refreshed automatically. Log in at terabox.com, complete verification, and update Ndus/JsToken in server config.");
                }

                return;
            }

            _jsToken = jsToken;
            if (!string.IsNullOrWhiteSpace(bdsToken))
            {
                _bdsToken = bdsToken;
            }

            _tokensRefreshedAt = DateTimeOffset.UtcNow;
            _logger.LogInformation("Terabox session tokens refreshed successfully.");
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private static bool IsVerificationRequired(int errno, string? errmsg) =>
        errno == 4000023 ||
        string.Equals(errmsg, "need verify", StringComparison.OrdinalIgnoreCase);

    private static bool IsAccessTokenExpired(int errno) => errno is 200002 or 200003;

    private static string? ExtractJsToken(string html)
    {
        const string encodedMarker = "fn%28%22";
        var start = html.IndexOf(encodedMarker, StringComparison.Ordinal);
        if (start >= 0)
        {
            start += encodedMarker.Length;
            var end = html.IndexOf("%22%29", start, StringComparison.Ordinal);
            if (end > start)
            {
                return html[start..end];
            }
        }

        var match = Regex.Match(html, @"fn\(""([A-Fa-f0-9]+)""\)");
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string? ExtractBdsToken(string html)
    {
        var match = Regex.Match(html, @"bdstoken""\s*:\s*""([^""]+)""");
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        match = Regex.Match(html, @"bdstoken%22%3A%22([^%""']+)%22");
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string BuildCookieHeader(TeraboxOptions options)
    {
        var parts = new List<string> { "lang=en", $"ndus={options.Ndus}" };
        if (!string.IsNullOrWhiteSpace(options.BrowserId))
        {
            parts.Add($"browserid={options.BrowserId}");
        }

        return string.Join("; ", parts);
    }

    private static string NormalizeDirectory(string directory)
    {
        var normalized = (directory ?? "/").Replace('\\', '/').Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "/";
        }

        if (!normalized.StartsWith('/'))
        {
            normalized = $"/{normalized}";
        }

        return normalized.TrimEnd('/');
    }

    private static async Task<string> ComputeMd5HexAsync(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await MD5.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string GenerateSign(string sign3, string sign1)
    {
        var a = new int[256];
        var p = new int[256];
        var keyLength = sign3.Length;
        if (keyLength == 0 || sign1.Length == 0)
        {
            throw new InvalidOperationException("Terabox sign payload was invalid.");
        }

        for (var i = 0; i < 256; i++)
        {
            a[i] = sign3[i % keyLength];
            p[i] = i;
        }

        for (int u = 0, i = 0; i < 256; i++)
        {
            u = (u + p[i] + a[i]) % 256;
            (p[i], p[u]) = (p[u], p[i]);
        }

        var output = new byte[sign1.Length];
        for (int u = 0, i = 0, q = 0; q < sign1.Length; q++)
        {
            i = (i + 1) % 256;
            u = (u + p[i]) % 256;
            (p[i], p[u]) = (p[u], p[i]);
            var k = p[(p[i] + p[u]) % 256];
            output[q] = (byte)(sign1[q] ^ k);
        }

        return Convert.ToBase64String(output);
    }

    private sealed class TeraboxApiException : InvalidOperationException
    {
        public TeraboxApiException(int errno, string? errmsg, string operation)
            : base($"Terabox {operation} failed ({errno}): {errmsg ?? "Unknown error"}")
        {
            Errno = errno;
            Errmsg = errmsg;
            Operation = operation;
        }

        public int Errno { get; }
        public string? Errmsg { get; }
        public string Operation { get; }
    }

    private class TeraboxSimpleResponse
    {
        [JsonPropertyName("errno")]
        public int Errno { get; set; }

        [JsonPropertyName("errmsg")]
        public string? Errmsg { get; set; }
    }

    private sealed class TeraboxPrecreateResponse : TeraboxSimpleResponse
    {
        [JsonPropertyName("uploadid")]
        public string? UploadId { get; set; }
    }

    private sealed class TeraboxCreateResponse : TeraboxSimpleResponse
    {
        [JsonPropertyName("fs_id")]
        public long FsId { get; set; }
    }

    private sealed class TeraboxDownloadResponse : TeraboxSimpleResponse
    {
        [JsonPropertyName("dlink")]
        public List<TeraboxDownloadItem>? Dlink { get; set; }

        [JsonPropertyName("list")]
        public List<TeraboxDownloadItem>? List { get; set; }
    }

    private sealed class TeraboxFileMetasResponse : TeraboxSimpleResponse
    {
        [JsonPropertyName("info")]
        public List<TeraboxDownloadItem>? Info { get; set; }
    }

    private sealed class TeraboxDownloadItem
    {
        [JsonPropertyName("dlink")]
        public string? Dlink { get; set; }
    }

    private sealed class TeraboxHomeInfoResponse : TeraboxSimpleResponse
    {
        [JsonPropertyName("data")]
        public TeraboxHomeInfo? Data { get; set; }
    }

    private sealed class TeraboxHomeInfo
    {
        [JsonPropertyName("sign1")]
        public string Sign1 { get; set; } = string.Empty;

        [JsonPropertyName("sign3")]
        public string Sign3 { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }
    }

    private sealed class TeraboxListResponse : TeraboxSimpleResponse
    {
        [JsonPropertyName("list")]
        public List<TeraboxListItem>? List { get; set; }
    }

    private sealed class TeraboxListItem
    {
        [JsonPropertyName("server_filename")]
        public string? ServerFilename { get; set; }
    }

    private sealed class HttpDependencyStream : Stream
    {
        private readonly Stream _inner;
        private readonly HttpResponseMessage _response;
        private readonly HttpClient _client;
        private bool _disposed;

        public HttpDependencyStream(Stream inner, HttpResponseMessage response, HttpClient client)
        {
            _inner = inner;
            _response = response;
            _client = client;
        }

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => false;
        public override long Length => _inner.Length;
        public override long Position
        {
            get => _inner.Position;
            set => _inner.Position = value;
        }

        public override void Flush() => _inner.Flush();

        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

        public override void SetLength(long value) => _inner.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _inner.Dispose();
                _response.Dispose();
                _client.Dispose();
                _disposed = true;
            }

            base.Dispose(disposing);
        }
    }
}

public sealed record TeraboxUploadResult(long FsId, string RemotePath);
