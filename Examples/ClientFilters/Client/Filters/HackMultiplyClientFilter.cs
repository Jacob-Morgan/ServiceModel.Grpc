﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Contract;
using ServiceModel.Grpc.Filters;

namespace Client.Filters;

internal sealed class HackMultiplyClientFilter : IClientFilter
{
    // MultiplyByAsync is async operation
    public void Invoke(IClientFilterContext context, Action next) => next();

    public ValueTask InvokeAsync(IClientFilterContext context, Func<ValueTask> next)
    {
        // ignore "non MultiplyByAsync" operations
        if (context.ContractMethodInfo.Name != nameof(ICalculator.MultiplyByAsync))
        {
            return next();
        }

        return DoHack(context, next);
    }

    private async ValueTask DoHack(IClientFilterContext context, Func<ValueTask> next)
    {
        var inputMultiplier = (int)context.Request["multiplier"]!;
        var inputValues = (IAsyncEnumerable<int>)context.Request.Stream;

        // increase multiplier by 2
        context.Request["multiplier"] = inputMultiplier + 2;

        // increase each input value by 1
        context.Request.Stream = IncreaseValuesBy1(inputValues, context.CallOptions.CancellationToken);

        await next().ConfigureAwait(false);

        var outputValues = (IAsyncEnumerable<int>)context.Response.Stream;

        // increase each output value by 1
        context.Response.Stream = IncreaseValuesBy1(outputValues, context.CallOptions.CancellationToken);
    }

    private async IAsyncEnumerable<int> IncreaseValuesBy1(IAsyncEnumerable<int> values, [EnumeratorCancellation] CancellationToken token)
    {
        await foreach (var value in values.WithCancellation(token).ConfigureAwait(false))
        {
            yield return value + 1;
        }
    }
}