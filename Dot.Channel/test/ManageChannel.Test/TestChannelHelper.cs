using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ManagedChannel;
using Xunit;

namespace ManageChannel.Test
{
    public class TestChannelHelper
    {
        [Theory]
        [InlineData(1, 1)]
        [InlineData(1, 1000)]
        [InlineData(10, 1000)]
        [InlineData(10, 1000000)]
        public async Task TestReadAllAsync(int numberOfConsumer, int totalRecords)
        {
            var list = Enumerable.Range(1, totalRecords).Select(x => x);
            var expectedResult = Enumerable.Range(1, totalRecords).Select(x => x + 10);
            var myChannel = new ChannelHelper<int>();
            var result = await myChannel.Source(list)
                .SetConsumer(numberOfConsumer)
                .ReadAllAsync<int>(t => t + 10,
                    (i, e) => { Console.WriteLine(e); });
            Assert.Equal(expectedResult.Count(), result.Count());
            var hash = new HashSet<int>();
            foreach (var item in expectedResult)
            {
                hash.Add(item);
            }

            foreach (var item in result)
            {
                Assert.Contains(item, hash);
            }
        }
        
        [Fact]
        public async Task TestReadAException()
        {
            var list = Enumerable.Range(1, 1000).Select(x => x);
            var expectedResult = Enumerable.Range(1, 100).Select(x => x*10);
            var exceptionList=new ConcurrentQueue<int>();
            var myChannel = new ChannelHelper<int>();
            var result = await myChannel.Source(list)
                .SetConsumer(5)
                .ReadAllAsync<int>(t => t /(t%10),
                    (i, e) =>
                    {
                        exceptionList.Enqueue(i);
                    });
            
            var hash = new HashSet<int>();
            foreach (var item in expectedResult)
            {
               hash.Add(item);
            }
            Assert.Equal(hash.Count(), exceptionList.Count());

            foreach (var item in exceptionList)
            {
                Assert.Contains(item, hash);
            }
        }
    }
}