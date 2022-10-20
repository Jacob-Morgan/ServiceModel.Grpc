﻿using System;
using System.Threading.Tasks;
using Contract;
using Grpc.Core;
using ServiceModel.Grpc.Client;

namespace Client;

public static class Program
{
    private static readonly IClientFactory DefaultClientFactory = new ClientFactory();

    public static async Task Main(string[] args)
    {
        try
        {
            await Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        Console.WriteLine("...");
        Console.ReadLine();
    }

    private static async Task Run()
    {
        // create gRPC channel
        var channel = new Channel("localhost", 5000, ChannelCredentials.Insecure);

        // create IGreeter client proxy
        var client = DefaultClientFactory.CreateClient<IGreeter>(channel);

        var person = new Person { FirstName = "John", SecondName = "X" };
            
        var greet1 = await client.SayHelloAsync(person.FirstName, person.SecondName);
        Console.WriteLine(greet1);

        var greet2 = await client.SayHelloToAsync(person);
        Console.WriteLine(greet2);
    }
}