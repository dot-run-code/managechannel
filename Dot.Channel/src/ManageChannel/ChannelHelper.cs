using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ManagedChannel
{
    public class ChannelHelper<T>
    {
        private int _consumer;
        private IEnumerable<T> _source;
        private int _batchSize;
        private int _page;


        public ChannelHelper()
        {
            _page = 0;
            _batchSize = 100;
            _consumer = 1;
        }

        public ChannelHelper<T> Source(IEnumerable<T> source)
        {
            ArgumentCheck(nameof(source), () => source == null || !source.Any());
            _source = source;
            return this;
        }

        public ChannelHelper<T> SetBatchSize(int batchSize)
        {
            ArgumentCheck(nameof(batchSize), () => batchSize < 1);
            _batchSize = batchSize;
            return this;
        }

        public ChannelHelper<T> SetConsumer(int consumer)
        {
            ArgumentCheck(nameof(consumer), () => consumer < 1);
            _consumer = consumer;
            return this;
        }


        public async Task ReadAllAsync(Action<T> action, Action<T, Exception> onError = null)
        {
            var channel = Channel.CreateBounded<T>(_batchSize);
            var taskList = new List<Task>();
            for (var i = 0; i < _consumer; i++)
            {
                taskList.Add(Task.Run(async () =>
                {
                    await foreach (var item in channel.Reader.ReadAllAsync())
                    {
                        try
                        {
                            action(item);
                        }
                        catch (Exception e)
                        {
                            onError?.Invoke(item, e);
                        }
                    }
                }));
            }

            while (true)
            {
                var itemList = _source.Skip(_page * _batchSize).Take(_batchSize);
                var enumerable = itemList as T[] ?? itemList.ToArray();
                if (!enumerable.Any()) break;
                foreach (var item in enumerable)
                {
                    await channel.Writer.WriteAsync(item);
                }

                _page++;
            }


            channel.Writer.Complete();
            await Task.WhenAll(taskList);
            await channel.Reader.Completion;
        }

        public async Task<IEnumerable<TResult>> ReadAllAsync<TResult>(Func<T, TResult> func,
            Action<T, Exception> onError = null)
        {
            var channel = Channel.CreateBounded<T>(_batchSize);
            var taskList = new List<Task<List<TResult>>>();
            for (var i = 0; i < _consumer; i++)
            {
                taskList.Add(Task.Run(async () =>
                {
                    var resultList = new List<TResult>();
                    await foreach (var item in channel.Reader.ReadAllAsync())
                    {
                        try
                        {
                            resultList.Add(func(item));
                        }
                        catch (Exception e)
                        {
                            onError?.Invoke(item, e);
                        }
                    }

                    return resultList;
                }));
            }

            while (true)
            {
                var itemList = _source.Skip(_page * _batchSize).Take(_batchSize);
                var enumerable = itemList as T[] ?? itemList.ToArray();
                if (!enumerable.Any()) break;
                foreach (var item in enumerable)
                {
                    await channel.Writer.WriteAsync(item);
                }

                _page++;
            }


            channel.Writer.Complete();
            await Task.WhenAll(taskList);
            await channel.Reader.Completion;
            var result = new List<TResult>();
            foreach (var task in taskList)
            {
                result.AddRange(task.Result);
            }

            return result;
        }


        private void ArgumentCheck(string source, Func<bool> fn)
        {
            if (fn())
                throw new ArgumentException(source);
        }
    }
}