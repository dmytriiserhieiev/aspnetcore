// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Analyzers.Http;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.CodeAnalysis;
using System.Globalization;

public class sdfsdf
{
    public void sdfsdfss()
    {
        var webApp = WebApplication.Create();
        webApp.MapGet("/", async (HttpContext context) =>
        {
            await Task.Yield();
            return "hello world";
        });
        webApp.MapGet("/", async (HttpContext context) =>
        {
            return await context.Request.ReadFromJsonAsync<object>();
        });
        webApp.MapGet("/", (HttpContext context) =>
        {
            return context.Request.ReadFromJsonAsync<object>().AsTask();
        });
        webApp.Use(next =>
        {
            return new RequestDelegate((HttpContext context) =>
            {
                next(context).Wait();
                return Task.FromResult("hello world");
            });
        });
        webApp.Use(async (HttpContext context, Func<Task> next) =>
        {
            context.SetEndpoint(new Endpoint(c => { return Task.FromResult(""); }, EndpointMetadataCollection.Empty, "Test"));
            await next();
        });
        webApp.MapGet("/", (HttpContext context) =>
        {
            if (Task.CompletedTask.IsCompleted)
            {
                return Task.CompletedTask;
            }
            return Task.FromResult("hello world");
        });
    }
}

public class RequestDelegateReturnTypeAnalyzerTests
{
    private TestDiagnosticAnalyzerRunner Runner { get; } = new(new RequestDelegateReturnTypeAnalyzer());

    private string GetMessage(string type) =>
        $"The method used to create a RequestDelegate returns Task<{type}>. RequestDelegate discards this value. If this isn't intended then don't return a value or change the method signature to not match RequestDelegate.";

    [Fact]
    public async Task AnonymousDelegate_RequestDelegate_ReturnType_EndpointCtor_ReportDiagnostics()
    {
        // Arrange
        var source = TestSource.Read(@"
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.Use(async (HttpContext context, Func<Task> next) =>
{
    context.SetEndpoint(new Endpoint(/*MM*/c => { return Task.FromResult(DateTime.Now); }, EndpointMetadataCollection.Empty, ""Test""));
    await next();
});
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DoNotReturnValueFromRequestDelegate, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.StartsWith(GetMessage("System.DateTime"), diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task AnonymousDelegate_RequestDelegate_ReturnType_AsTask_ReportDiagnostics()
    {
        // Arrange
        var source = TestSource.Read(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.MapGet(""/"", /*MM*/(HttpContext context) =>
{
    return context.Request.ReadFromJsonAsync<object>().AsTask();
});
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DoNotReturnValueFromRequestDelegate, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.StartsWith(GetMessage("object?"), diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task AnonymousDelegate_RequestDelegate_ReturnType_DelegateCtor_ReportDiagnostics()
    {
        // Arrange
        var source = TestSource.Read(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.Use(next =>
{
    return new RequestDelegate(/*MM*/(HttpContext context) =>
    {
        next(context).Wait();
        return Task.FromResult(""hello world"");
    });
});
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DoNotReturnValueFromRequestDelegate, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.StartsWith(GetMessage("string"), diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task AnonymousDelegate_RequestDelegate_ReturnTypeMethodCall_ReportDiagnostics()
    {
        // Arrange
        var source = TestSource.Read(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.MapGet(""/"", /*MM*/(HttpContext context) => Task.FromResult(""hello world""));
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DoNotReturnValueFromRequestDelegate, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.StartsWith(GetMessage("string"), diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task AnonymousDelegate_RequestDelegate_ReturnTypeVariable_ReportDiagnostics()
    {
        // Arrange
        var source = TestSource.Read(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.MapGet(""/"", /*MM*/(HttpContext context) =>
{
    var t = Task.FromResult(""hello world"");
    return t;
});
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DoNotReturnValueFromRequestDelegate, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.StartsWith(GetMessage("string"), diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task AnonymousDelegate_RequestDelegate_ReturnTypeTernary_ReportDiagnostics()
    {
        // Arrange
        var source = TestSource.Read(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.MapGet(""/"", /*MM*/(HttpContext context) =>
{
    var t1 = Task.FromResult(""hello world"");
    var t2 = Task.FromResult(""hello world"");
    return t1.IsCompleted ? t1 : t2;
});
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DoNotReturnValueFromRequestDelegate, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.StartsWith(GetMessage("string"), diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task AnonymousDelegate_RequestDelegate_ReturnTypeCoalesce_ReportDiagnostics()
    {
        // Arrange
        var source = TestSource.Read(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.MapGet(""/"", /*MM*/(HttpContext context) =>
{
    var t1 = Task.FromResult(""hello world"");
    var t2 = Task.FromResult(""hello world"");
    return t1 ?? t2;
});
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DoNotReturnValueFromRequestDelegate, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.StartsWith(GetMessage("string"), diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task AnonymousDelegate_RequestDelegate_MultipleReturns_ReportDiagnostics()
    {
        // Arrange
        var source = TestSource.Read(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.MapGet(""/"", /*MM*/(HttpContext context) =>
{
    var t1 = Task.FromResult(""hello world"");
    var t2 = Task.FromResult(""hello world"");
    if (t1.IsCompleted)
    {
        return t1;
    }
    else
    {
        return t2;
    }
});
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DoNotReturnValueFromRequestDelegate, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.StartsWith(GetMessage("string"), diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task AnonymousDelegate_RequestDelegate_MixReturnValues_ReportDiagnostics()
    {
        // Arrange
        var source = TestSource.Read(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.MapGet(""/"", /*MM*/(HttpContext context) =>
{
    var t1 = Task.FromResult(""hello world"");
    var t2 = Task.FromResult(1);
    if (t1.IsCompleted)
    {
        return Task.CompletedTask;
    }
    else
    {
        return t2;
    }
});
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DoNotReturnValueFromRequestDelegate, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.StartsWith(GetMessage("int"), diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task AnonymousDelegate_NotRequestDelegate_Async_HasReturnType_NoDiagnostics()
    {
        // Arrange
        var source = TestSource.Read(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.MapGet(""/"", async (HttpContext context) => ""hello world"");
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task AnonymousDelegate_RequestDelegate_Async_HasReturns_NoReturnType_NoDiagnostics()
    {
        // Arrange
        var source = TestSource.Read(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.MapGet(""/"", async (HttpContext context) =>
{
    if (Task.CompletedTask.IsCompleted)
    {
        await Task.Yield();
        return;
    }
    else
    {
        await Task.Delay(1000);
        return;
    }
});
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task AnonymousDelegate_RequestDelegate_NoReturnType_NoDiagnostics()
    {
        // Arrange
        var source = TestSource.Read(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.MapGet(""/"", (HttpContext context) => Task.CompletedTask);
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task AnonymousDelegate_RequestDelegate_MultipleReturns_NoReturnType_NoDiagnostics()
    {
        // Arrange
        var source = TestSource.Read(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.MapGet(""/"", (HttpContext context) =>
{
    if (Task.CompletedTask.IsCompleted)
    {
        return Task.CompletedTask;
    }
    else
    {
        return Task.CompletedTask;
    }
});
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task MethodReference_RequestDelegate_HasReturnType_ReportDiagnostics()
    {
        // Arrange
        var source = TestSource.Read(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.MapGet(""/"", /*MM*/HttpMethod);

static Task<string> HttpMethod(HttpContext context) => Task.FromResult(""hello world"");
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DoNotReturnValueFromRequestDelegate, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.StartsWith(GetMessage("string"), diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task MethodReference_RequestDelegate_NoReturnType_NoDiagnostics()
    {
        // Arrange
        var source = TestSource.Read(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.MapGet(""/"", HttpMethod);

static Task HttpMethod(HttpContext context) => Task.CompletedTask;
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        Assert.Empty(diagnostics);
    }
}
