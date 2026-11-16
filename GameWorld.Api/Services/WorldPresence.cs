namespace GameWorld.Api.Services;

/// <summary>
/// One-server presence and short-lived regional chat. Economic state remains in the database.
/// Positions are presentation state and expire instead of writing the database every frame.
/// </summary>
public sealed class WorldPresence
{
    private static readonly TimeSpan PresenceLifetime = TimeSpan.FromSeconds(12);
    private static readonly TimeSpan ChatLifetime = TimeSpan.FromMinutes(15);
    private readonly object _sync = new();
    private readonly Dictionary<long, PresenceEntry> _players = [];
    private readonly Dictionary<long, List<MapChatMessage>> _messages = [];
    private readonly Dictionary<long, DateTime> _lastChat = [];
    private long _nextMessageId;

    public PresencePosition Update(PresenceActor actor, long sessionId, double requestedX, double requestedZ, decimal movementModifier)
    {
        if (!double.IsFinite(requestedX) || !double.IsFinite(requestedZ) || Math.Abs(requestedX) > 42 || Math.Abs(requestedZ) > 42)
            throw new GameRuleException("Vị trí phải nằm trong phạm vi bản đồ (-42 đến 42).");
        var now = DateTime.UtcNow;
        var timestamp = TimeProvider.System.GetTimestamp();
        lock (_sync)
        {
            Clean(now);
            // An account controls a single visible character, even when several tabs are open.
            foreach (var other in _players.Values.Where(x => x.AccountId == actor.AccountId && x.CharacterId != actor.CharacterId).Select(x => x.CharacterId).ToArray())
                _players.Remove(other);
            if (!_players.TryGetValue(actor.CharacterId, out var previous) || previous.MapId != actor.MapId || previous.SessionId != sessionId)
            {
                if (_players.Count >= 10000 && previous is null) throw new GameRuleException("Thế giới đang đông người. Vui lòng thử lại sau.");
                var spawnX = float.IsFinite(actor.SpawnX) ? Math.Clamp(actor.SpawnX, -42, 42) : 0;
                var spawnZ = float.IsFinite(actor.SpawnZ) ? Math.Clamp(actor.SpawnZ, -42, 42) : 0;
                previous = new(actor.CharacterId, actor.AccountId, sessionId, actor.Name, actor.Level, actor.MapId, spawnX, spawnZ, now, timestamp);
                _players[actor.CharacterId] = previous;
                return new(actor.CharacterId, actor.MapId, spawnX, spawnZ, requestedX != spawnX || requestedZ != spawnZ, now);
            }
            var elapsed = Math.Clamp(TimeProvider.System.GetElapsedTime(previous.Timestamp, timestamp).TotalSeconds, 0, 2);
            var distanceX = requestedX - previous.X;
            var distanceZ = requestedZ - previous.Z;
            var distance = Math.Sqrt(distanceX * distanceX + distanceZ * distanceZ);
            var maxDistance = actor.CurrentHP > 0 ? 5 * (double)Math.Clamp(movementModifier, 0.3m, 1.5m) * elapsed : 0;
            var ratio = distance > maxDistance && distance > 0 ? maxDistance / distance : 1;
            var x = previous.X + distanceX * ratio;
            var z = previous.Z + distanceZ * ratio;
            _players[actor.CharacterId] = previous with { Name = actor.Name, Level = actor.Level, X = x, Z = z, TouchedAt = now, Timestamp = timestamp };
            return new(actor.CharacterId, actor.MapId, x, z, ratio < 1, now);
        }
    }

    public PresenceCandidate[] Candidates(long mapId, long exceptCharacterId)
    {
        lock (_sync)
        {
            Clean(DateTime.UtcNow);
            return _players.Values.Where(x => x.MapId == mapId && x.CharacterId != exceptCharacterId)
                .Select(x => new PresenceCandidate(x.CharacterId, x.AccountId, x.SessionId, x.Name, x.Level, x.MapId, x.X, x.Z)).ToArray();
        }
    }

    public MapChatMessage[] Messages(long mapId)
    {
        lock (_sync)
        {
            Clean(DateTime.UtcNow);
            return _messages.TryGetValue(mapId, out var messages) ? messages.ToArray() : [];
        }
    }

    public MapChatMessage Chat(PresenceActor actor, long sessionId, string text)
    {
        text = new string(text.Trim().Where(c => !char.IsControl(c)).ToArray());
        if (text.Length is < 1 or > 200) throw new GameRuleException("Tin nhắn phải có từ 1 đến 200 ký tự.");
        var now = DateTime.UtcNow;
        lock (_sync)
        {
            Clean(now);
            if (!_players.TryGetValue(actor.CharacterId, out var player) || player.AccountId != actor.AccountId || player.MapId != actor.MapId || player.SessionId != sessionId)
                throw new GameRuleException("Hãy kết nối nhân vật vào vùng hiện tại trước khi trò chuyện.");
            if (_lastChat.TryGetValue(actor.AccountId, out var sentAt) && now - sentAt < TimeSpan.FromSeconds(2))
                throw new GameRuleException("Hãy chờ 2 giây giữa các tin nhắn.");
            var message = new MapChatMessage(++_nextMessageId, actor.CharacterId, actor.Name, text, now);
            if (!_messages.TryGetValue(actor.MapId, out var messages)) _messages[actor.MapId] = messages = [];
            messages.Add(message);
            if (messages.Count > 30) messages.RemoveRange(0, messages.Count - 30);
            _lastChat[actor.AccountId] = now;
            return message;
        }
    }

    private void Clean(DateTime now)
    {
        foreach (var id in _players.Where(x => now - x.Value.TouchedAt > PresenceLifetime).Select(x => x.Key).ToArray()) _players.Remove(id);
        foreach (var pair in _messages.ToArray())
        {
            pair.Value.RemoveAll(x => now - x.SentAt > ChatLifetime);
            if (pair.Value.Count == 0) _messages.Remove(pair.Key);
        }
        foreach (var id in _lastChat.Where(x => now - x.Value > ChatLifetime).Select(x => x.Key).ToArray()) _lastChat.Remove(id);
    }

    private sealed record PresenceEntry(long CharacterId, long AccountId, long SessionId, string Name, int Level, long MapId,
        double X, double Z, DateTime TouchedAt, long Timestamp);
}

public sealed record PresenceActor(long CharacterId, long AccountId, string Name, int Level, long MapId, int CurrentHP, float SpawnX, float SpawnZ);
public sealed record PresenceCandidate(long CharacterId, long AccountId, long SessionId, string Name, int Level, long MapId, double X, double Z);
public sealed record PresencePosition(long CharacterId, long MapId, double X, double Z, bool Corrected, DateTime ServerTime);
public sealed record MapChatMessage(long Id, long CharacterId, string Name, string Text, DateTime SentAt);
