﻿using System.Threading.Tasks;
using Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;

namespace Demo.AspNet.DesignTime;

public static class Program
{
    private const int Port = 8081;

    public static async Task Main()
    {
        using (var host = await StartWebHost())
        {
            var clientCalls = new ClientCalls();

            // register IPersonService proxy generated by ServiceModel.Grpc.DesignTime
            clientCalls.ClientFactory.AddPersonServiceClient();

            await clientCalls.CallPersonService(Port);
            await host.StopAsync();
        }
    }

    private static async Task<IHost> StartWebHost()
    {
        var host = Host
            .CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<WebHostStartup>();

                webBuilder.UseKestrel(o => o.ListenLocalhost(Port, l => l.Protocols = HttpProtocols.Http2));

            })
            .Build();

        await host.StartAsync();
        return host;
    }
}