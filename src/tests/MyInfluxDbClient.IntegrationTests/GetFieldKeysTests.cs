using System;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace MyInfluxDbClient.IntegrationTests
{
    [TestFixture]
    public class GetFieldKeysTests : IntegrationTests
    {
        private string _databaseName;
        private InfluxPoints _seededPoints;

        protected override void OnBeforeAllTests()
        {
            _databaseName = CreateUniqueTestDatabase();

            _seededPoints = new InfluxPoints()
                .Add(new InfluxPoint("orderCreated").AddTag("oid", "1").AddTag("mid", "1").AddField("amt", 112.5).AddField("fee", 10M))
                .Add(new InfluxPoint("orderCreated").AddTag("oid", "2").AddTag("mid", "1").AddField("amt", 90M).AddField("fee", 10M))
                .Add(new InfluxPoint("orderCreated").AddTag("oid", "3").AddTag("mid", "2").AddField("amt", 50M).AddField("fee", 10M))
                .Add(new InfluxPoint("paymentRecieved").AddTag("pid", "1").AddTag("oid", "1").AddTag("mid", "1").AddField("amt", 60M))
                .Add(new InfluxPoint("paymentRecieved").AddTag("pid", "2").AddTag("oid", "1").AddTag("mid", "1").AddField("amt", 52.5M));

            Client.WriteAsync(_databaseName, _seededPoints).Wait();
        }

        protected override void OnAfterAllTests()
        {
            DropTestDatabase(_databaseName);
        }

        [Test]
        public async Task Should_return_all_field_keys_When_no_measurement_is_specified()
        {
            var fieldKeys = await Client.GetFieldKeysAsync(_databaseName);

            fieldKeys.Should().ContainKey("orderCreated");
            fieldKeys.Should().ContainKey("paymentRecieved");
            fieldKeys["orderCreated"].Should().Contain(new [] {"amt", "fee"});
            fieldKeys["paymentRecieved"].Should().Contain("amt");
        }

        [Test]
        public async Task Should_return_field_keys_for_specific_measurement_When_measurement_is_specified()
        {
            var fieldKeys = await Client.GetFieldKeysAsync(_databaseName, "orderCreated");

            fieldKeys["orderCreated"].Should().Contain(new[] { "amt", "fee" });
            fieldKeys.Should().NotContainKey("paymentRecieved");
        }

        [Test]
        public async Task Should_return_empty_field_keys_result_When_non_existing_measurement_is_specified()
        {
            var fieldKeys = await Client.GetFieldKeysAsync(_databaseName, Guid.NewGuid().ToString("n"));

            fieldKeys.Should().BeEmpty();
        }
    }
}