// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers.Http;

internal sealed class WellKnownTypes
{
    public static bool TryCreate(Compilation compilation, [NotNullWhen(returnValue: true)] out WellKnownTypes? wellKnownTypes)
    {
        wellKnownTypes = default;

        const string RequestDelegate = "Microsoft.AspNetCore.Http.RequestDelegate";
        if (compilation.GetTypeByMetadataName(RequestDelegate) is not { } requestDelegate)
        {
            return false;
        }

        const string TaskOfT = "System.Threading.Tasks.Task`1";
        if (compilation.GetTypeByMetadataName(TaskOfT) is not { } taskOfT)
        {
            return false;
        }

        wellKnownTypes = new()
        {
            RequestDelegate = requestDelegate,
            TaskOfT = taskOfT
        };

        return true;
    }

    public INamedTypeSymbol RequestDelegate { get; private init; }
    public INamedTypeSymbol TaskOfT { get; private init; }
}
