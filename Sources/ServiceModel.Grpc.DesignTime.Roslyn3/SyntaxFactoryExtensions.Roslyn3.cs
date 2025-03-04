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
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ServiceModel.Grpc.DesignTime.Generator;

internal static partial class SyntaxFactoryExtensions
{
    private static void TryGetAncestorMember(MemberDeclarationSyntax syntax, ref SyntaxKind kind, ref string? name)
    {
        var type = syntax.GetType();
        if ("FileScopedNamespaceDeclarationSyntax".Equals(type.Name, StringComparison.Ordinal))
        {
            var nameSyntax = (NameSyntax)syntax
                .GetType()
                .GetProperty(nameof(NamespaceDeclarationSyntax.Name), BindingFlags.Public | BindingFlags.Instance)!
                .GetValue(syntax);

            kind = SyntaxKind.NamespaceDeclaration;
            name = nameSyntax.WithoutTrivia().ToString();
        }
    }
}