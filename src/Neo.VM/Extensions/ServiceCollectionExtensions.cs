// Copyright (C) 2015-2026 The Neo Project.
//
// ServiceCollectionExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Neo.VM.Builder;
using Neo.VM.Middleware;
using System;

namespace Neo.VM.Extensions;

/// <summary>
/// Dependency-injection helpers for registering the Neo VM engine and middleware.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a scoped <see cref="ExecutionEngine"/> built with any registered middleware.
    /// When a <see cref="DebuggerMiddleware"/> is registered, it is placed first in the pipeline.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddExecutionEngine(this IServiceCollection services)
    {
        services.AddScoped(sp =>
        {
            var middleware = sp.GetServices<IEngineMiddleware>();
            var debugger = sp.GetService<DebuggerMiddleware>();

            if (debugger is not null)
                middleware = [debugger, .. middleware];

            var pipeline = ExecutionPipelineBuilder.Create()
                .Use(middleware)
                .Build();

            return ExecutionEngineBuilder.Create()
                .UsePipeline(pipeline)
                .Build();
        });

        return services;
    }

    /// <summary>
    /// Registers a middleware type as a singleton <see cref="IEngineMiddleware"/>.
    /// </summary>
    /// <typeparam name="TMiddleware">The middleware implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddEngineMiddleware<TMiddleware>(this IServiceCollection services)
        where TMiddleware : class, IEngineMiddleware
    {
        services.AddSingleton<IEngineMiddleware, TMiddleware>();
        return services;
    }

    /// <summary>
    /// Registers the <see cref="DebuggerMiddleware"/> as a scoped service.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddEngineMiddlewareDebugger(this IServiceCollection services)
    {
        services.AddScoped(sp =>
            new DebuggerMiddleware(
                () => sp.GetRequiredService<ExecutionEngine>(),
                sp.GetService<ILogger<DebuggerMiddleware>>() ?? NullLogger<DebuggerMiddleware>.Instance));
        return services;
    }

    /// <summary>
    /// Registers the <see cref="ExecuteLoggerMiddleware"/> as a scoped <see cref="IEngineMiddleware"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddEngineMiddlewareLogger(this IServiceCollection services)
    {
        services.AddScoped<IEngineMiddleware>(sp =>
            new ExecuteLoggerMiddleware(
                () => sp.GetRequiredService<ExecutionEngine>(),
                sp.GetService<ILogger<ExecuteLoggerMiddleware>>() ?? NullLogger<ExecuteLoggerMiddleware>.Instance));
        return services;
    }

    /// <summary>
    /// Registers one or more middleware types as singleton <see cref="IEngineMiddleware"/> implementations.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="middlewareTypes">Types that implement <see cref="IEngineMiddleware"/>.</param>
    /// <returns>The same service collection for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when a type does not implement <see cref="IEngineMiddleware"/>.</exception>
    public static IServiceCollection AddEngineMiddleware(this IServiceCollection services, params Type[] middlewareTypes)
    {
        foreach (var type in middlewareTypes)
        {
            if (!typeof(IEngineMiddleware).IsAssignableFrom(type))
                throw new ArgumentException($"Type {type} must implement {nameof(IEngineMiddleware)}");

            services.AddSingleton(typeof(IEngineMiddleware), type);
        }

        return services;
    }
}
