using System;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace EbayClone.Infrastructure.Services
{
    /// <summary>
    /// [Performance Phase 2] Redis-based Distributed Lock — dùng SET NX EX pattern.
    /// 
    /// Cách hoạt động:
    /// 1. TryAcquireLock → Redis SET key value NX EX ttl
    ///    - NX = chỉ set nếu key chưa tồn tại (Non-eXists)
    ///    - EX = auto-expire sau ttl giây (phòng crash)
    /// 2. ReleaseLock → DEL key (chỉ khi value khớp instance hiện tại)
    /// 
    /// Lưu ý Performance:
    /// - Redis SET/DEL là O(1) — rất nhanh, không ảnh hưởng latency
    /// - Lock value = Machine name + Process ID → tránh release nhầm lock của instance khác
    /// - Auto-expire → nếu instance crash, lock tự giải phóng sau TTL
    /// </summary>
    public class RedisDistributedLockService : IDistributedLockService, IDisposable
    {
        private readonly ConnectionMultiplexer _redis;
        private readonly ILogger<RedisDistributedLockService> _logger;
        private readonly string _lockValue;

        public RedisDistributedLockService(
            IConfiguration configuration,
            ILogger<RedisDistributedLockService> logger)
        {
            _logger = logger;
            
            var redisConn = configuration.GetConnectionString("Redis") ?? "localhost:6379";
            
            // [Performance] Cấu hình ConnectionMultiplexer với timeout hợp lý
            var options = ConfigurationOptions.Parse(redisConn);
            options.ConnectTimeout = 5000;     // 5s connect timeout
            options.SyncTimeout = 3000;        // 3s sync operation timeout
            options.AbortOnConnectFail = false; // Không throw khi Redis down → graceful degradation
            
            _redis = ConnectionMultiplexer.Connect(options);
            
            // Lock value = Machine + PID → unique per instance
            _lockValue = $"{Environment.MachineName}:{Environment.ProcessId}";
            
            _logger.LogInformation("RedisDistributedLockService initialized. Instance: {Instance}", _lockValue);
        }

        public async Task<bool> TryAcquireLockAsync(string lockKey, TimeSpan expiry, CancellationToken cancellationToken = default)
        {
            try
            {
                var db = _redis.GetDatabase();
                
                // SET key value NX EX ttl
                // NX = only set if Not eXists → atomic lock acquire
                // EX = auto-expire → self-healing if instance crashes
                var acquired = await db.StringSetAsync(
                    $"lock:{lockKey}", 
                    _lockValue, 
                    expiry, 
                    When.NotExists);

                if (acquired)
                {
                    _logger.LogDebug("Lock acquired: {Key} by {Instance}, TTL: {TTL}s", lockKey, _lockValue, expiry.TotalSeconds);
                }
                else
                {
                    _logger.LogDebug("Lock NOT acquired: {Key} — held by another instance", lockKey);
                }

                return acquired;
            }
            catch (RedisConnectionException ex)
            {
                // Redis down → fallback: cho phép execute (single instance mode)
                // Tốt hơn là block tất cả instances khi Redis tạm mất
                _logger.LogWarning(ex, "Redis unavailable for lock {Key}. Falling back to allow execution.", lockKey);
                return true;
            }
        }

        // [Performance] Lua script cho atomic Compare-And-Delete
        // GET + CHECK + DELETE trong 1 atomic operation — chống race condition
        private static readonly LuaScript _releaseLockScript = LuaScript.Prepare(
            "if redis.call('get', @key) == @value then return redis.call('del', @key) else return 0 end");

        public async Task ReleaseLockAsync(string lockKey, CancellationToken cancellationToken = default)
        {
            try
            {
                var db = _redis.GetDatabase();
                var key = $"lock:{lockKey}";
                
                // [Security] Atomic release: chỉ xóa nếu value khớp instance hiện tại
                // Trước đây: GET → CHECK → DELETE (3 steps, race condition)
                // Bây giờ: Lua script CAS (1 atomic step)
                var result = (int)await db.ScriptEvaluateAsync(
                    _releaseLockScript, 
                    new { key = (RedisKey)key, value = (RedisValue)_lockValue });

                if (result == 1)
                {
                    _logger.LogDebug("Lock released: {Key} by {Instance}", lockKey, _lockValue);
                }
                else
                {
                    _logger.LogDebug("Lock {Key} not owned by this instance or already expired. Skip release.", lockKey);
                }
            }
            catch (RedisConnectionException ex)
            {
                // Redis down → lock sẽ tự expire qua TTL
                _logger.LogWarning(ex, "Redis unavailable for releasing lock {Key}. Lock will auto-expire.", lockKey);
            }
        }

        public void Dispose()
        {
            _redis?.Dispose();
        }
    }
}
