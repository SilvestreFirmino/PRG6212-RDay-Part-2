using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;

namespace RaceDayApi.Tests;

internal static class TestHelpers
{
    public static RaceDayDbContext Database()
    {
        var options = new DbContextOptionsBuilder<RaceDayDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new RaceDayDbContext(options);
    }

    public static void SetUser(ControllerBase controller, int userId, string role)
    {
        var context = new DefaultHttpContext();
        context.Features.Set<ISessionFeature>(new TestSessionFeature());
        context.Session.SetInt32("UserId", userId);
        context.Session.SetString("Role", role);
        controller.ControllerContext = new ControllerContext { HttpContext = context };
    }

    private class TestSessionFeature : ISessionFeature { public ISession Session { get; set; } = new TestSession(); }

    private class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _data = new();
        public IEnumerable<string> Keys => _data.Keys;
        public string Id => "test-session";
        public bool IsAvailable => true;
        public void Clear() => _data.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _data.Remove(key);
        public void Set(string key, byte[] value) => _data[key] = value;
        public bool TryGetValue(string key, out byte[] value) => _data.TryGetValue(key, out value!);
    }
}
