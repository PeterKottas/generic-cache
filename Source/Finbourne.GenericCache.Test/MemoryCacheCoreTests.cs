using Finbourne.GenericCache.Core.Model.Enum;
using Finbourne.GenericCache.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace Finbourne.GenericCache.Test
{
    [TestClass]
    public class MemoryCacheCoreTests
    {
        private ILogger<MemoryCacheCore> GetMockLogger()
        {
            return new Mock<ILogger<MemoryCacheCore>>().Object;
        }

        private MemoryCacheConfig GetTestCacheConfig()
        {
            return new MemoryCacheConfig { MaxSize = 3 };
        }

        [TestMethod]
        public void SetAsync_ShouldAddItemToCache()
        {
            // Arrange
            var logger = GetMockLogger();
            var cacheConfig = GetTestCacheConfig();
            var cache = new MemoryCacheCore(cacheConfig, logger);

            // Act
            cache.SetAsync("Key1", 100).Wait();

            // Assert
            Assert.IsTrue(cache.TryGetAsync<int>("Key1", out var value).Result);
            Assert.AreEqual(100, value);
        }

        [TestMethod]
        public void SetAsync_ShouldRemoveOldestItemWhenCacheFull()
        {
            // Arrange
            var logger = GetMockLogger();
            var cacheConfig = GetTestCacheConfig();
            var cache = new MemoryCacheCore(cacheConfig, logger);

            // Act
            cache.SetAsync("Key1", 100).Wait();
            cache.SetAsync("Key2", 200).Wait();
            cache.SetAsync("Key3", 300).Wait();
            cache.SetAsync("Key4", 400).Wait();

            // Assert
            Assert.IsFalse(cache.TryGetAsync<int>("Key1", out _).Result);
            Assert.IsTrue(cache.TryGetAsync<int>("Key2", out var value).Result);
            Assert.AreEqual(200, value);
        }


        [TestMethod]
        public void SetAsync_ShouldRemoveOldestUsedItemWhenCacheFull()
        {
            // Arrange
            var logger = GetMockLogger();
            var cacheConfig = GetTestCacheConfig();
            var cache = new MemoryCacheCore(cacheConfig, logger);

            // Act
            cache.SetAsync("Key1", 100).Wait();
            cache.SetAsync("Key2", 200).Wait();
            cache.SetAsync("Key3", 300).Wait();
            cache.TryGetAsync<int>("Key1", out _).Wait();
            cache.SetAsync("Key4", 400).Wait();

            // Assert
            Assert.IsFalse(cache.TryGetAsync<int>("Key2", out _).Result);
            Assert.IsTrue(cache.TryGetAsync<int>("Key1", out var value).Result);
            Assert.AreEqual(100, value);
        }

        [TestMethod]
        public void DeleteAsync_ShouldRemoveItemFromCache()
        {
            // Arrange
            var logger = GetMockLogger();
            var cacheConfig = GetTestCacheConfig();
            var cache = new MemoryCacheCore(cacheConfig, logger);
            cache.SetAsync("Key1", 100).Wait();

            // Act
            cache.DeleteAsync<int>("Key1").Wait();

            // Assert
            Assert.IsFalse(cache.TryGetAsync<int>("Key1", out _).Result);
        }

        [TestMethod]
        public void PurgeAsync_ShouldClearCache()
        {
            // Arrange
            var logger = GetMockLogger();
            var cacheConfig = GetTestCacheConfig();
            var cache = new MemoryCacheCore(cacheConfig, logger);
            cache.SetAsync("Key1", 100).Wait();
            cache.SetAsync("Key2", 200).Wait();

            // Act
            cache.PurgeAsync().Wait();

            // Assert
            Assert.IsFalse(cache.TryGetAsync<int>("Key1", out _).Result);
            Assert.IsFalse(cache.TryGetAsync<int>("Key2", out _).Result);
        }

        [TestMethod]
        public void SubscribeDeleteAsync_ShouldReceiveCacheDeletionNotifications()
        {
            // Arrange
            var logger = GetMockLogger();
            var cacheConfig = GetTestCacheConfig();
            var cache = new MemoryCacheCore(cacheConfig, logger);
            var mockAction = new Mock<Action<string, CacheDeletionReasonEnum, int>>();
            var guid = cache.SubscribeDeleteAsync<int>(mockAction.Object).Result;
            cache.SetAsync("Key1", 100).Wait();

            // Act
            cache.DeleteAsync<int>("Key1").Wait();

            // Assert
            mockAction.Verify(a => a("Key1", CacheDeletionReasonEnum.ManualDelete, 100), Times.Once);
        }

        [TestMethod]
        public void UnSubscribeDeleteAsync_ShouldUnsubscribeFromNotifications()
        {
            // Arrange
            var logger = GetMockLogger();
            var cacheConfig = GetTestCacheConfig();
            var cache = new MemoryCacheCore(cacheConfig, logger);
            var mockAction = new Mock<Action<string, CacheDeletionReasonEnum, int>>();
            var guid = cache.SubscribeDeleteAsync<int>(mockAction.Object).Result;

            // Act
            cache.UnSubscribeDeleteAsync<int>(guid).Wait();
            cache.SetAsync("Key1", 100).Wait();
            cache.DeleteAsync<int>("Key1").Wait();

            // Assert
            mockAction.Verify(a => a(It.IsAny<string>(), It.IsAny<CacheDeletionReasonEnum>(), It.IsAny<int>()), Times.Never);
        }
    }
}