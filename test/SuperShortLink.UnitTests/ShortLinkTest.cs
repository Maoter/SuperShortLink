﻿using Microsoft.Extensions.DependencyInjection;
using SuperShortLink.Cache;
using SuperShortLink.Repository;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SuperShortLink.UnitTests
{
    public class ShortLinkTest
    {
        private readonly IMemoryCaching _memory;
        private readonly IShortLinkService _shortLinkService;
        private readonly IShortLinkRepository _repository;

        public ShortLinkTest()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddShortLink(option =>
            {
                option.ConnectionString = "Server=192.168.100.3;Port=5432;User Id=ApolloProgram_User;Password=qf0LUpK@;Database=zz_db002;";
                option.DbType = DatabaseType.PostgreSQL;
                option.Secrect = "s9LFkgy5RovixI1aOf8UhdY3r4DMplQZJXPqebE0WSjBn7wVzmN2Gc6THCAKut";
                option.CodeLength = 6;
            });
            IServiceProvider serviceProvider = services.BuildServiceProvider();

            _shortLinkService = serviceProvider.GetService<IShortLinkService>();
            _repository = serviceProvider.GetService<IShortLinkRepository>();
            _memory = serviceProvider.GetService<IMemoryCaching>();
        }

        [Fact]
        public async Task Generate_Should_Be_Valid()
        {
            //有效链接
            var shortLink = await _shortLinkService.GenerateAsync("http://www.baidu.com");
            Assert.NotNull(shortLink);

            //无效链接
            shortLink = await _shortLinkService.GenerateAsync("abcd");
            Assert.Null(shortLink);
        }

        [Fact]
        public void Confuse_Should_Be_Same()
        {
            var id = int.MaxValue;
            //混淆加密
            var shortLink = _shortLinkService.ConfusionConvert(id);
            Assert.NotNull(shortLink);

            //解密-恢复混淆
            var reId = _shortLinkService.ReConfusionConvert(shortLink);
            Assert.Equal(reId, id);
        }

        [Fact]
        public async Task Count_Should_Add_When_Aceess()
        {
            var shortLink = await _shortLinkService.GenerateAsync("http://www.baidu.com");
            Assert.NotNull(shortLink);

            var reId = _shortLinkService.ReConfusionConvert(shortLink);
            Assert.True(reId > 0);

            var model = await _repository.GetAsync(reId);
            Assert.NotNull(model);
            Assert.True(model.access_count == 0);

            var accessCount = 100;
            for (int i = 0; i < accessCount; i++)
            {
                await _shortLinkService.AccessAsync(shortLink);
            }

            model = await _repository.GetAsync(reId);
            Assert.NotNull(model);
            Assert.True(model.access_count == accessCount);
        }

        [Fact]
        public void Cache_Should_Succeed()
        {
            var key = "key";
            var value = "value";
            var result = _memory.Set(key, value);
            Assert.True(result);

            var cache = _memory.Get<string>(key);
            Assert.False(cache.IsNull);
            Assert.Equal(value, cache.Value);
        }
    }
}