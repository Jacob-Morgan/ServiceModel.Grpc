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

using NUnit.Framework;
using ServiceModel.Grpc.Configuration;
using ServiceModel.Grpc.TestApi;
using ServiceModel.Grpc.TestApi.Domain;

namespace ServiceModel.Grpc.Internal.Emit;

[TestFixture]
public class EmitClientBuilderGenericTest : ClientBuilderGenericTestBase
{
    [OneTimeSetUp]
    public void BeforeAllTests()
    {
        var description = new ContractDescription(typeof(IGenericContract<int, string>));

        var moduleBuilder = ProxyAssembly.CreateModule(nameof(EmitClientBuilderGenericTest));

        var contractBuilder = new EmitContractBuilder(description);
        var contractType = contractBuilder.Build(moduleBuilder);
        var contractFactory = EmitContractBuilder.CreateFactory(contractType);

        var sut = new EmitClientBuilder(description, contractType);
        var clientType = sut.Build(moduleBuilder);
        var clientFactory = sut.CreateFactory<IGenericContract<int, string>>(clientType);

        Factory = () => clientFactory(CallInvoker.Object, contractFactory(DataContractMarshallerFactory.Default), null);
    }
}