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

using System;
using NUnit.Framework;
using Shouldly;

namespace ServiceModel.Grpc.Configuration;

[TestFixture]
public class MarshallerFactoryExtensionsTest
{
    private readonly IMarshallerFactory _factory = DataContractMarshallerFactory.Default;

    [Test]
    public void SerializeString()
    {
        var content = _factory.SerializeHeader("abc");

        _factory.DeserializeHeader(typeof(string), content).ShouldBe("abc");
    }

    [Test]
    public void SerializeBytes()
    {
        var expected = Guid.NewGuid().ToByteArray();

        var content = _factory.SerializeHeader(expected);
        content.ShouldBe(expected);

        _factory.DeserializeHeader(typeof(byte[]), content).ShouldBe(expected);
    }
}