using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace OpenCodeSleepGuard;

public sealed record DbPollResult(
    bool IsWorking,
    string LastActivity,
    DateTime? LastActivityTime,
    string SessionTitle,
    string AgentName,
    string TaskInfo,
    string DbStatus);

public sealed class OpenCodeDbMonitor : IDisposable
{
    private const int InitializeRowLimit = 100;
    private const int PollRowLimit = 500;

    private readonly string _dbPath;
    private readonly string _connectionString;

    private string? _lastSeenId;
    private bool _isWorking;
    private string _lastActivity = "초기화 전";
    private DateTime? _lastActivityTime;
    private string _sessionTitle = "-";
    private string _agentName = "-";
    private string _taskInfo = "-";
    private string _dbStatus = "연결 대기";
    private bool _disposed;

    public OpenCodeDbMonitor(string dbPath)
    {
        _dbPath = string.IsNullOrWhiteSpace(dbPath)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "opencode", "opencode.db")
            : dbPath;

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = _dbPath,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = true
        };

        _connectionString = builder.ToString();
    }

    public DbPollResult Initialize()
    {
        ThrowIfDisposed();

        if (!File.Exists(_dbPath))
        {
            _lastSeenId = null;
            _isWorking = false;
            _lastActivity = "DB 없음";
            _lastActivityTime = null;
            _sessionTitle = "-";
            _agentName = "-";
            _taskInfo = "-";
            _dbStatus = "DB 없음";
            Console.WriteLine($"[OpenCodeDbMonitor] DB file not found: {_dbPath}");
            return CurrentResult();
        }

        try
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = $@"
SELECT p.id, p.time_created, p.data, m.data, s.title
FROM part p
LEFT JOIN message m ON m.id = p.message_id
LEFT JOIN session s ON s.id = p.session_id
ORDER BY p.id DESC
LIMIT {InitializeRowLimit};";

            using var reader = command.ExecuteReader();

            string? highestId = null;
            var latestRelevant = (Found: false, IsWorking: false, Activity: string.Empty, ActivityTime: (DateTime?)null);
            var latestMeta = false;

            while (reader.Read())
            {
                var id = reader.GetString(0);
                if (string.IsNullOrEmpty(highestId) || string.CompareOrdinal(id, highestId) > 0)
                {
                    highestId = id;
                }

                if (!latestMeta)
                {
                    ApplyMetadata(reader, 1, 2, 3, 4);
                    latestMeta = true;
                }

                if (latestRelevant.Found)
                {
                    continue;
                }

                var parsed = ParseEvent(reader, idColumnIndex: 0, timeColumnIndex: 1, dataColumnIndex: 2);
                if (!parsed.IsRelevant)
                {
                    continue;
                }

                latestRelevant = (true, parsed.IsWorking, parsed.Activity, parsed.ActivityTime);
            }

            _lastSeenId = highestId;

            if (latestRelevant.Found)
            {
                _isWorking = latestRelevant.IsWorking;
                _lastActivity = latestRelevant.Activity;
                _lastActivityTime = latestRelevant.ActivityTime;
            }
            else
            {
                _isWorking = false;
                _lastActivity = !string.IsNullOrEmpty(highestId) ? "관련 step 이벤트 없음" : "part 레코드 없음";
                _lastActivityTime = null;
            }

            _dbStatus = "연결 정상";

            Console.WriteLine($"[OpenCodeDbMonitor] Initialized. LastSeenId={_lastSeenId}, Working={_isWorking}, Activity={_lastActivity}");
            return CurrentResult();
        }
        catch (Exception ex)
        {
            _dbStatus = "읽기 실패";
            Console.WriteLine($"[OpenCodeDbMonitor] Initialize failed: {ex.Message}");
            return CurrentResult();
        }
    }

    public DbPollResult Poll()
    {
        ThrowIfDisposed();

        if (!File.Exists(_dbPath))
        {
            _isWorking = false;
            _lastActivity = "DB 없음";
            _lastActivityTime = null;
            _dbStatus = "DB 없음";
            return CurrentResult();
        }

        try
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = $@"
SELECT p.id, p.time_created, p.data, m.data, s.title
FROM part p
LEFT JOIN message m ON m.id = p.message_id
LEFT JOIN session s ON s.id = p.session_id
WHERE ($lastSeenId IS NULL OR p.id > $lastSeenId)
ORDER BY p.id
LIMIT {PollRowLimit};";
            command.Parameters.AddWithValue("$lastSeenId", (object?)_lastSeenId ?? DBNull.Value);

            using var reader = command.ExecuteReader();
            var sawRows = false;

            while (reader.Read())
            {
                sawRows = true;
                var id = reader.GetString(0);
                if (string.IsNullOrEmpty(_lastSeenId) || string.CompareOrdinal(id, _lastSeenId) > 0)
                {
                    _lastSeenId = id;
                }

                ApplyMetadata(reader, 1, 2, 3, 4);

                var parsed = ParseEvent(reader, idColumnIndex: 0, timeColumnIndex: 1, dataColumnIndex: 2);
                if (!parsed.IsRelevant)
                {
                    continue;
                }

                _isWorking = parsed.IsWorking;
                _lastActivity = parsed.Activity;
                _lastActivityTime = parsed.ActivityTime;
            }

            if (sawRows)
            {
                _dbStatus = "연결 정상";
            }

            return CurrentResult();
        }
        catch (Exception ex)
        {
            _dbStatus = "읽기 실패";
            Console.WriteLine($"[OpenCodeDbMonitor] Poll failed: {ex.Message}");
            return CurrentResult();
        }
    }

    public void Dispose()
    {
        _disposed = true;
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private DbPollResult CurrentResult()
    {
        return new DbPollResult(_isWorking, _lastActivity, _lastActivityTime, _sessionTitle, _agentName, _taskInfo, _dbStatus);
    }

    private void ApplyMetadata(SqliteDataReader reader, int timeColumnIndex, int dataColumnIndex, int messageDataColumnIndex, int sessionTitleColumnIndex)
    {
        _lastActivityTime = ParseTimestamp(reader, timeColumnIndex) ?? _lastActivityTime;
        _sessionTitle = ReadSessionTitle(reader, sessionTitleColumnIndex);
        _agentName = ReadAgentName(reader, messageDataColumnIndex);
        _taskInfo = ReadTaskInfo(reader, dataColumnIndex);
    }

    private static ParsedEvent ParseEvent(SqliteDataReader reader, int idColumnIndex, int timeColumnIndex, int dataColumnIndex)
    {
        if (reader.IsDBNull(dataColumnIndex))
        {
            return ParsedEvent.NotRelevant;
        }

        try
        {
            var json = reader.GetString(dataColumnIndex);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (!root.TryGetProperty("type", out var typeElement))
            {
                return ParsedEvent.NotRelevant;
            }

            var type = typeElement.GetString();
            if (string.Equals(type, "step-start", StringComparison.OrdinalIgnoreCase))
            {
                return new ParsedEvent(true, true, "step-start 감지", ParseTimestamp(reader, timeColumnIndex));
            }

            if (!string.Equals(type, "step-finish", StringComparison.OrdinalIgnoreCase))
            {
                return ParsedEvent.NotRelevant;
            }

            if (!root.TryGetProperty("reason", out var reasonElement))
            {
                return ParsedEvent.NotRelevant;
            }

            var reason = reasonElement.GetString();
            if (!string.Equals(reason, "stop", StringComparison.OrdinalIgnoreCase))
            {
                return ParsedEvent.NotRelevant;
            }

            return new ParsedEvent(true, false, "step-finish(stop) 감지", ParseTimestamp(reader, timeColumnIndex));
        }
        catch (Exception ex)
        {
            var id = reader.IsDBNull(idColumnIndex) ? "<null>" : reader.GetString(idColumnIndex);
            Console.WriteLine($"[OpenCodeDbMonitor] Failed to parse part row {id}: {ex.Message}");
            return ParsedEvent.NotRelevant;
        }
    }

    private static DateTime? ParseTimestamp(SqliteDataReader reader, int timeColumnIndex)
    {
        if (reader.IsDBNull(timeColumnIndex))
        {
            return null;
        }

        var raw = reader.GetValue(timeColumnIndex);

        if (raw is long longValue)
        {
            return FromUnix(longValue);
        }

        if (raw is int intValue)
        {
            return FromUnix(intValue);
        }

        if (raw is double doubleValue)
        {
            return FromUnix((long)doubleValue);
        }

        if (raw is string text)
        {
            if (DateTime.TryParse(text, out var parsedDateTime))
            {
                return parsedDateTime;
            }

            if (long.TryParse(text, out var parsedUnix))
            {
                return FromUnix(parsedUnix);
            }
        }

        return null;
    }

    private static DateTime? FromUnix(long value)
    {
        if (value <= 0)
        {
            return null;
        }

        const long maxUnixMilliseconds = 253_402_300_799_999;
        const long maxUnixSeconds = 253_402_300_799;

        if (value > 100_000_000_000)
        {
            return value <= maxUnixMilliseconds
                ? DateTimeOffset.FromUnixTimeMilliseconds(value).LocalDateTime
                : null;
        }

        return value <= maxUnixSeconds
            ? DateTimeOffset.FromUnixTimeSeconds(value).LocalDateTime
            : null;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(OpenCodeDbMonitor));
        }
    }

    private readonly record struct ParsedEvent(bool IsRelevant, bool IsWorking, string Activity, DateTime? ActivityTime)
    {
        public static ParsedEvent NotRelevant => new(false, false, string.Empty, null);
    }

    private static string ReadSessionTitle(SqliteDataReader reader, int sessionTitleColumnIndex)
    {
        if (reader.IsDBNull(sessionTitleColumnIndex))
        {
            return "-";
        }

        var value = reader.GetString(sessionTitleColumnIndex);
        return string.IsNullOrWhiteSpace(value) ? "-" : value;
    }

    private static string ReadAgentName(SqliteDataReader reader, int messageDataColumnIndex)
    {
        if (reader.IsDBNull(messageDataColumnIndex))
        {
            return "-";
        }

        try
        {
            using var document = JsonDocument.Parse(reader.GetString(messageDataColumnIndex));
            var root = document.RootElement;

            if (root.TryGetProperty("agent", out var agentElement))
            {
                var value = agentElement.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            if (root.TryGetProperty("mode", out var modeElement))
            {
                var value = modeElement.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
        }
        catch
        {
        }

        return "-";
    }

    private static string ReadTaskInfo(SqliteDataReader reader, int dataColumnIndex)
    {
        if (reader.IsDBNull(dataColumnIndex))
        {
            return "-";
        }

        try
        {
            using var document = JsonDocument.Parse(reader.GetString(dataColumnIndex));
            var root = document.RootElement;

            if (root.TryGetProperty("tool", out var toolElement))
            {
                var tool = toolElement.GetString();
                if (!string.IsNullOrWhiteSpace(tool))
                {
                    return tool;
                }
            }

            if (root.TryGetProperty("type", out var typeElement))
            {
                var type = typeElement.GetString();
                if (!string.IsNullOrWhiteSpace(type))
                {
                    return type;
                }
            }
        }
        catch
        {
        }

        return "-";
    }
}
