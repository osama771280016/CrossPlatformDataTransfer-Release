using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using CrossPlatformDataTransfer.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace CrossPlatformDataTransfer.Application.Transfer;

// ─────────────────────────────────────────────────────────────
//  Wire DTOs — self-describing payload layout
//
//  Every payload sent by this engine:
//    [4 bytes LE : metaLen][metaLen bytes : UTF-8 JSON][remaining : binary chunk]
//
//  This layout avoids dependence on any framer MetaLen field and keeps all
//  meta/payload splitting inside this engine with no transport changes.
// ─────────────────────────────────────────────────────────────

file sealed class StartTransferMeta
{
    public string CommandType { get; init; } = "START_TRANSFER";
    public string TransferId  { get; init; } = string.Empty;
    public long   TotalBytes  { get; init; }
    public int    TotalChunks { get; init; }
    public int    ChunkSize   { get; init; }
}

file sealed class ChunkMeta
{
    public string CommandType { get; init; } = "CHUNK";
    public string TransferId  { get; init; } = string.Empty;
    public int    ChunkIndex  { get; init; }
    public string Sha256      { get; init; } = string.Empty;
}

file sealed class CompleteMeta
{
    public string CommandType { get; init; } = "COMPLETE";
    public string TransferId  { get; init; } = string.Empty;
    public string FileSha256  { get; init; } = string.Empty;
}

file sealed class ErrorMeta
{
    public string CommandType { get; init; } = "ERROR";
    public string TransferId  { get; init; } = string.Empty;
    public string Reason      { get; init; } = string.Empty;
}

file sealed class AckMeta
{
    public string  CommandType { get; init; } = string.Empty;
    public string  TransferId  { get; init; } = string.Empty;
    public int     ChunkIndex  { get; init; }
    public bool    Success     { get; init; }
    public string? Reason      { get; init; }
}

file sealed class ResumeResponseMeta
{
    public string CommandType        { get; init; } = string.Empty;
    public string TransferId         { get; init; } = string.Empty;
    public int    LastConfirmedChunk { get; init; }
}

// ─────────────────────────────────────────────────────────────
//  Per-chunk state: TCS + per-chunk timeout CTS
// ─────────────────────────────────────────────────────────────

file sealed class PendingChunk : IDisposable
{
    public TaskCompletionSource<bool> Tcs { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public CancellationTokenSource TimeoutCts { get; } = new();

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        TimeoutCts.Dispose();
    }
}

// ─────────────────────────────────────────────────────────────
//  Progress record
// ─────────────────────────────────────────────────────────────

public sealed record TransferProgress(
    string TransferId,
    int    ConfirmedChunks,
    int    TotalChunks,
    long   ConfirmedBytes,
    long   TotalBytes,
    bool   IsComplete);

// ─────────────────────────────────────────────────────────────
//  Engine
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Reliable, resumable, integrity-verified chunked file transfer.
///
/// Integration points
/// ──────────────────
/// • IAgentCommunicationService  — transport (send/receive raw byte frames).
/// • IHashService                — per-chunk SHA-256 verification + final file hash.
/// • ILogger&lt;ChunkedTransferEngine&gt; — structured diagnostics; no UI dependency.
///
/// Payload wire layout (self-describing, transport-agnostic)
/// ──────────────────────────────────────────────────────────
/// [4 bytes LE : metaLen][metaLen bytes : UTF-8 JSON][remaining : binary data]
///
/// Per-chunk ACK timeout (30 s)
/// ────────────────────────────
/// Armed immediately after the chunk frame is written. Timeout removes the
/// entry from the pending table, faults the TCS, and cancels the transfer.
/// The timeout CTS is disposed in a ContinueWith on the TCS — safe in both
/// the normal-ACK path and the timeout-fired path.
///
/// SHA-256 integrity
/// ─────────────────
/// IHashService.ComputeHash() is used for the per-chunk hash (embedded in
/// ChunkMeta and verified by the Android agent). The incremental file hash
/// (TransformBlock / TransformFinalBlock) produces the whole-file digest
/// delivered in the COMPLETE frame — no second read, no seek.
/// </summary>
public sealed class ChunkedTransferEngine
{
    private const int    ChunkSize     = 256 * 1024;
    private const int    MaxInFlight   = 4;
    private const int    MetaPrefixLen = 4;
    private static readonly TimeSpan AckTimeout = TimeSpan.FromSeconds(30);

    private static readonly JsonSerializerOptions JsonOpts =
        new(JsonSerializerDefaults.Web);

    private readonly IAgentCommunicationService   _agent;
    private readonly IHashService                 _hash;
    private readonly ILogger<ChunkedTransferEngine> _log;

    public ChunkedTransferEngine(
        IAgentCommunicationService    agent,
        IHashService                  hash,
        ILogger<ChunkedTransferEngine> log)
    {
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
        _hash  = hash  ?? throw new ArgumentNullException(nameof(hash));
        _log   = log   ?? throw new ArgumentNullException(nameof(log));
    }

    /// <summary>
    /// Sends <paramref name="sourceStream"/> to the connected Android agent.
    /// Seekability is not required; the stream is read exactly once.
    /// </summary>
    public async Task SendAsync(
        Stream                       sourceStream,
        long                         totalBytes,
        string                       transferId,
        IProgress<TransferProgress>? progress = null,
        CancellationToken            ct       = default)
    {
        if (sourceStream is null) throw new ArgumentNullException(nameof(sourceStream));
        if (totalBytes < 0)       throw new ArgumentOutOfRangeException(nameof(totalBytes));
        if (string.IsNullOrWhiteSpace(transferId))
            throw new ArgumentException("transferId required.", nameof(transferId));

        int totalChunks = totalBytes == 0
            ? 0
            : (int)Math.Ceiling((double)totalBytes / ChunkSize);

        int resumeFrom = await NegotiateStartAsync(transferId, totalBytes, totalChunks, ct)
            .ConfigureAwait(false);

        _log.LogInformation("[{Id}] Sending from chunk {From}/{Total}.",
            transferId, resumeFrom, totalChunks);

        var pendingAcks = new ConcurrentDictionary<int, PendingChunk>();
        using var sha   = SHA256.Create();
        int  confirmedCount = resumeFrom;
        long confirmedBytes = (long)resumeFrom * ChunkSize;

        using var cts   = CancellationTokenSource.CreateLinkedTokenSource(ct);
        Exception? readerFault = null;
        int acksRemaining = totalChunks - resumeFrom;

        using var inFlightGate = new SemaphoreSlim(MaxInFlight, MaxInFlight);

        // ── ACK reader ───────────────────────────────────────────
        Task ackReader = Task.Run(async () =>
        {
            try
            {
                while (acksRemaining > 0)
                {
                    cts.Token.ThrowIfCancellationRequested();

                    byte[] raw = await ReceiveFrameAsync(cts.Token).ConfigureAwait(false);
                    if (raw.Length == 0) throw new TransferException("Connection closed by agent.");

                    var ack = DeserializeMeta<AckMeta>(raw)
                              ?? throw new TransferException("Malformed ACK (null metadata).");

                    if (ack.CommandType == "ERROR")
                    {
                        var err = DeserializeMeta<ErrorMeta>(raw);
                        throw new TransferException($"Agent ERROR: {err?.Reason}");
                    }

                    if (!string.Equals(ack.TransferId, transferId, StringComparison.Ordinal))
                        throw new TransferException(
                            $"ACK transferId mismatch: '{ack.TransferId}' ≠ '{transferId}'.");

                    if (!pendingAcks.TryRemove(ack.ChunkIndex, out var pending))
                        throw new TransferException(
                            $"ACK for unknown chunkIndex {ack.ChunkIndex}.");

                    if (ack.Success)
                        pending.Tcs.TrySetResult(true);
                    else
                        pending.Tcs.TrySetException(new TransferException(
                            $"Chunk {ack.ChunkIndex} rejected: {ack.Reason}"));

                    Interlocked.Decrement(ref acksRemaining);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                readerFault = ex;
                foreach (var kv in pendingAcks)
                    kv.Value.Tcs.TrySetException(ex);
                await cts.CancelAsync().ConfigureAwait(false);
            }
        }, CancellationToken.None);

        // ── Send loop ────────────────────────────────────────────
        try
        {
            for (int idx = resumeFrom; idx < totalChunks; idx++)
            {
                cts.Token.ThrowIfCancellationRequested();

                int  len    = (int)ChunkLength(idx, totalChunks, totalBytes);
                bool isLast = idx == totalChunks - 1;

                byte[] buf = ArrayPool<byte>.Shared.Rent(len);
                try
                {
                    int read = await ReadExactAsync(sourceStream, buf, len, cts.Token)
                        .ConfigureAwait(false);
                    if (read != len)
                        throw new TransferException(
                            $"Stream ended prematurely at chunk {idx} (got {read}/{len} B).");

                    // Incremental file SHA-256 (IHashService used for per-chunk hash below)
                    if (!isLast)
                        sha.TransformBlock(buf, 0, len, null, 0);
                    else
                        sha.TransformFinalBlock(buf, 0, len);

                    // Per-chunk hash via IHashService (matches Android VerifyChunkHash)
                    string chunkHash = _hash.ComputeHash(buf[..len]);

                    var pending = new PendingChunk();
                    pendingAcks[idx] = pending;

                    await inFlightGate.WaitAsync(cts.Token).ConfigureAwait(false);
                    cts.Token.ThrowIfCancellationRequested();

                    byte[] frame = BuildFrame(
                        new ChunkMeta
                        {
                            TransferId = transferId,
                            ChunkIndex = idx,
                            Sha256     = chunkHash,
                        },
                        buf.AsSpan(0, len),
                        isLast);

                    await SendFrameAsync(frame, cts.Token).ConfigureAwait(false);

                    _log.LogDebug("[{Id}] Chunk {Idx}/{Last} sent ({Bytes} B).",
                        transferId, idx, totalChunks - 1, len);

                    // Arm per-chunk timeout AFTER write
                    int  capturedIdx   = idx;
                    long capturedBytes = len;

                    pending.TimeoutCts.Token.Register(() =>
                    {
                        if (!pendingAcks.TryRemove(capturedIdx, out var t)) return;
                        _log.LogError("[{Id}] ACK timeout chunk {Idx} ({Secs}s).",
                            transferId, capturedIdx, AckTimeout.TotalSeconds);
                        t.Tcs.TrySetException(new TransferException(
                            $"ACK timeout chunk {capturedIdx}."));
                        cts.Cancel();
                    }, useSynchronizationContext: false);

                    pending.TimeoutCts.CancelAfter(AckTimeout);

                    _ = pending.Tcs.Task.ContinueWith(t =>
                    {
                        pending.Dispose();
                        inFlightGate.Release();

                        if (t.IsCompletedSuccessfully)
                        {
                            int  nc = Interlocked.Increment(ref confirmedCount);
                            long nb = Interlocked.Add(ref confirmedBytes, capturedBytes);
                            progress?.Report(new TransferProgress(
                                transferId, nc, totalChunks,
                                Math.Min(nb, totalBytes), totalBytes, false));
                        }
                    },
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
                }
                finally { ArrayPool<byte>.Shared.Return(buf); }
            }

            await ackReader.ConfigureAwait(false);
        }
        catch
        {
            await cts.CancelAsync().ConfigureAwait(false);
            try { await ackReader.ConfigureAwait(false); } catch { }
            string reason = readerFault?.Message ?? "Send loop failure.";
            await SendErrorFrameAsync(transferId, reason).ConfigureAwait(false);
            if (readerFault is not null)
                throw new TransferException("Transfer failed (reader fault).", readerFault);
            throw;
        }

        if (readerFault is not null)
        {
            await SendErrorFrameAsync(transferId, readerFault.Message).ConfigureAwait(false);
            throw new TransferException("Transfer failed (reader fault).", readerFault);
        }

        // ── COMPLETE with whole-file SHA-256 ─────────────────────
        string fileSha = totalChunks > 0
            ? Convert.ToHexString(sha.Hash!).ToLowerInvariant()
            : _hash.ComputeHash(Array.Empty<byte>());

        _log.LogInformation("[{Id}] Sending COMPLETE (sha256={Hash}).", transferId, fileSha);

        byte[] completeFrame = BuildFrame(new CompleteMeta
        {
            TransferId = transferId,
            FileSha256 = fileSha,
        });
        await SendFrameAsync(completeFrame, ct).ConfigureAwait(false);

        progress?.Report(new TransferProgress(
            transferId, totalChunks, totalChunks, totalBytes, totalBytes, true));

        _log.LogInformation("[{Id}] Transfer complete — {N} chunks, {B} bytes.",
            transferId, totalChunks, totalBytes);
    }

    // ─────────────────────────────────────────────────────────────
    //  Handshake
    // ─────────────────────────────────────────────────────────────

    private async Task<int> NegotiateStartAsync(
        string transferId, long totalBytes, int totalChunks, CancellationToken ct)
    {
        byte[] frame = BuildFrame(new StartTransferMeta
        {
            TransferId  = transferId,
            TotalBytes  = totalBytes,
            TotalChunks = totalChunks,
            ChunkSize   = ChunkSize,
        });
        await SendFrameAsync(frame, ct).ConfigureAwait(false);

        _log.LogDebug("[{Id}] START_TRANSFER sent ({N} chunks, {B} bytes).",
            transferId, totalChunks, totalBytes);

        while (true)
        {
            byte[] raw = await ReceiveFrameAsync(ct).ConfigureAwait(false);

            var resumeResp = TryDeserializeMeta<ResumeResponseMeta>(raw);
            if (resumeResp is { CommandType: "RESUME_RESPONSE" })
            {
                _log.LogInformation("[{Id}] Resuming from chunk {N}.",
                    transferId, resumeResp.LastConfirmedChunk);
                return resumeResp.LastConfirmedChunk;
            }

            var errMeta = TryDeserializeMeta<ErrorMeta>(raw);
            if (errMeta?.CommandType == "ERROR")
                throw new TransferException($"Agent refused START_TRANSFER: {errMeta.Reason}");

            // Any other response (generic OK) means fresh start
            var generic = TryDeserializeMeta<GenericResponseMeta>(raw);
            if (generic?.Status == "OK") return 0;

            _log.LogWarning("[{Id}] Unexpected response during negotiation — assuming fresh start.", transferId);
            return 0;
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Transport — delegates to IAgentCommunicationService
    // ─────────────────────────────────────────────────────────────

    private async Task SendFrameAsync(byte[] frame, CancellationToken ct)
    {
        // IAgentCommunicationService uses its own length-prefix framing.
        // We send the engine frame as the payload of an AgentCommand.
        await _agent.SendEncryptedPayloadAsync(frame).ConfigureAwait(false);
    }

    private async Task<byte[]> ReceiveFrameAsync(CancellationToken ct)
    {
        // Stream a single response frame via the agent's command channel.
        await foreach (var chunk in _agent.StreamDataAsync(
            new AgentCommand { CommandType = "RECEIVE_FRAME" })
            .WithCancellation(ct))
        {
            return chunk; // first (and only) yielded item
        }
        return Array.Empty<byte>();
    }

    private async Task SendErrorFrameAsync(string transferId, string reason)
    {
        try
        {
            byte[] frame = BuildFrame(new ErrorMeta { TransferId = transferId, Reason = reason });
            await _agent.SendEncryptedPayloadAsync(frame).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "[{Id}] Could not deliver ERROR frame to agent.", transferId);
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Self-describing payload codec
    // ─────────────────────────────────────────────────────────────

    private static byte[] BuildFrame<T>(T meta, ReadOnlySpan<byte> data = default, bool isLast = false)
    {
        var metaJson  = JsonSerializer.SerializeToUtf8Bytes(meta, JsonOpts);
        var result    = new byte[MetaPrefixLen + metaJson.Length + data.Length];
        BinaryPrimitives.WriteInt32LittleEndian(result, metaJson.Length);
        metaJson.AsSpan().CopyTo(result.AsSpan(MetaPrefixLen));
        if (!data.IsEmpty)
            data.CopyTo(result.AsSpan(MetaPrefixLen + metaJson.Length));
        return result;
    }

    private static T? DeserializeMeta<T>(byte[] payload)
    {
        if (payload.Length < MetaPrefixLen) return default;
        int metaLen = BinaryPrimitives.ReadInt32LittleEndian(payload);
        if (metaLen <= 0 || MetaPrefixLen + metaLen > payload.Length) return default;
        return JsonSerializer.Deserialize<T>(
            payload.AsSpan(MetaPrefixLen, metaLen), JsonOpts);
    }

    private static T? TryDeserializeMeta<T>(byte[] payload)
    {
        try   { return DeserializeMeta<T>(payload); }
        catch { return default; }
    }

    // ─────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────

    private static long ChunkLength(int idx, int total, long totalBytes)
    {
        if (idx < total - 1) return ChunkSize;
        long rem = totalBytes % ChunkSize;
        return rem == 0 ? ChunkSize : rem;
    }

    private static async Task<int> ReadExactAsync(
        Stream s, byte[] buf, int count, CancellationToken ct)
    {
        int total = 0;
        while (total < count)
        {
            int r = await s.ReadAsync(buf, total, count - total, ct).ConfigureAwait(false);
            if (r == 0) break;
            total += r;
        }
        return total;
    }
}

// Lightweight response shim for negotiation
file sealed class GenericResponseMeta
{
    public string Status { get; init; } = string.Empty;
}
