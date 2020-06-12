# Managechannel

It is wrapper on dot net core channel.This wrapper uses one producer and multiple consumer model.

You can supply a IEnumerable datasource,set the batch size and parallel factor.
This will parallelly process the source using parallel tasks.Default batch size is 100.
In the following example the producer will first fetch 100 records and push into the channel.The 5 consumer(here set as 5) will process that 100 tasks parallely.Once the producer fetch another 100 task -the consumer will continue the process until all the records area processed.
 

    var list = Enumerable.Range(1, 100000).Select(x => x);
                var expectedResult = Enumerable.Range(1, totalRecords).Select(x => x + 10);
                var myChannel = new ChannelHelper<int>();
                var result = await myChannel.Source(list)
                    .SetConsumer(5)
                    .ReadAllAsync<int>(t => t + 10,
                        (i, e) => { Console.WriteLine(e); });
  If any exception happened it will notify the user about the exception and continue processing it



     var list = Enumerable.Range(1, 1000).Select(x => x);
       var myChannel = new ChannelHelper<int>();
                var result = await myChannel.Source(list)
                    .SetConsumer(5)
                    .ReadAllAsync<int>(t => t /(t%10),
                        (i, e) =>
                        {
                            exceptionList.Enqueue(i);
                        });
						
 The default batch size is 100.You can increase it.
 

    var result = await myChannel.Source(list)
                    .SetConsumer(5)
                    .SetBatchSize(1000)
                    .ReadAllAsync<int>(t => t /(t%10),
                        (i, e) =>
                        {
                            exceptionList.Enqueue(i);
                        });
