﻿// <copyright>
// Copyright 2020 Max Ieremenko
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//  http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ServiceModel.Grpc.TestApi;
using ServiceModel.Grpc.TestApi.Domain;

namespace ServiceModel.Grpc.AspNetCore
{
    [TestFixture]
    public class ExceptionHandlingTest : ExceptionHandlingTestBase
    {
        private KestrelHost _host;

        [OneTimeSetUp]
        public async Task BeforeAll()
        {
            _host = new KestrelHost(configure: options => options.ErrorHandler = new ClientErrorHandler());

            await _host.StartAsync(
                services =>
                {
                    services.AddServiceModelGrpc(options => options.DefaultErrorHandler = new ServerErrorHandler());
                },
                configureEndpoints: endpoints =>
                {
                    endpoints.MapGrpcService<ErrorService>();
                });

            DomainService = _host.ClientFactory.CreateClient<IErrorService>(_host.Channel);
        }

        [OneTimeTearDown]
        public async Task AfterAll()
        {
            await _host.DisposeAsync();
        }
    }
}
