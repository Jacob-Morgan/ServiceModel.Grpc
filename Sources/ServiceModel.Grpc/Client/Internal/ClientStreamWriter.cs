﻿// <copyright>
// Copyright 2022 Max Ieremenko
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

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using ServiceModel.Grpc.Channel;

namespace ServiceModel.Grpc.Client.Internal;

internal sealed class ClientStreamWriter<TRequest> : IDisposable
{
    private readonly IAsyncEnumerable<TRequest> _request;
    private readonly IClientStreamWriter<Message<TRequest>> _stream;
    private readonly CancellationTokenSource _writeCancellation;
    private readonly Task _writer;

    public ClientStreamWriter(
        IAsyncEnumerable<TRequest> request,
        IClientStreamWriter<Message<TRequest>> stream,
        CancellationToken token)
    {
        _request = request;
        _stream = stream;
        _writeCancellation = CancellationTokenSource.CreateLinkedTokenSource(token, CancellationToken.None);

        _writer = Start(_writeCancellation.Token);
    }

    public Task Task => _writer;

    public async Task WaitAsync(CancellationToken token)
    {
        try
        {
            await _writer.ConfigureAwait(false);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.OK || ex.StatusCode == StatusCode.Cancelled || token.IsCancellationRequested)
        {
            // Grpc.Core.RpcException : Status(StatusCode="OK", Detail="")
            // one of the reasons the server does not read the whole request, see test MultipurposeServiceTestBase.ClientStreamingStopReading
        }
    }

    public void Dispose()
    {
        _writeCancellation.Cancel();
        _writeCancellation.Dispose();
    }

    private async Task Start(CancellationToken token)
    {
        await foreach (var i in _request.WithCancellation(token).ConfigureAwait(false))
        {
            await _stream.WriteAsync(new Message<TRequest>(i)).ConfigureAwait(false);
        }

        if (!token.IsCancellationRequested)
        {
            await _stream.CompleteAsync().ConfigureAwait(false);
        }
    }
}