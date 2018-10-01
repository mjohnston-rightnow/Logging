// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Microsoft.Extensions.Logging.Testing
{
    public class LoggedTestBase : ILoggedTest
    {
        private IDisposable _testLog;

        // Obsolete but keeping for back compat
        public LoggedTestBase(ITestOutputHelper output = null)
        {
            TestOutputHelper = output;
        }

        // Internal for testing
        internal string ResolvedTestMethodName { get; set; }

        // Internal for testing
        internal string ResolvedTestClassName { get; set; }

        public int TestRetries { get; set; } = 1;

        public ILogger Logger { get; set; }

        public ILoggerFactory LoggerFactory { get; set; }

        public ITestOutputHelper TestOutputHelper { get; set; }

        public void AddTestLogging(IServiceCollection services) => services.AddSingleton(LoggerFactory);

        // For back compat
        public IDisposable StartLog(out ILoggerFactory loggerFactory, [CallerMemberName] string testName = null) => StartLog(out loggerFactory, LogLevel.Information, testName);

        // For back compat
        public IDisposable StartLog(out ILoggerFactory loggerFactory, LogLevel minLogLevel, [CallerMemberName] string testName = null)
        {
            return AssemblyTestLog.ForAssembly(GetType().GetTypeInfo().Assembly).StartTestLog(TestOutputHelper, GetType().FullName, out loggerFactory, minLogLevel, testName);
        }

        public virtual void Initialize(MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
        {
            TestOutputHelper = testOutputHelper;

            var classType = GetType();
            var logLevelAttribute = methodInfo.GetCustomAttribute<LogLevelAttribute>() as LogLevelAttribute;
            var testName = testMethodArguments.Aggregate(methodInfo.Name, (a, b) => $"{a}-{(b ?? "null")}");

            var useShortClassName = methodInfo.DeclaringType.GetCustomAttribute<ShortClassNameAttribute>()
                ?? methodInfo.DeclaringType.Assembly.GetCustomAttribute<ShortClassNameAttribute>();
            var resolvedClassName = useShortClassName == null ? classType.FullName : classType.Name;

            _testLog = AssemblyTestLog
                .ForAssembly(classType.GetTypeInfo().Assembly)
                .StartTestLog(
                    TestOutputHelper,
                    resolvedClassName,
                    out var loggerFactory,
                    logLevelAttribute?.LogLevel ?? LogLevel.Trace,
                    out var resolvedTestName,
                    testName);

            // internal for testing
            ResolvedTestMethodName = resolvedTestName;
            ResolvedTestClassName = resolvedClassName;

            LoggerFactory = loggerFactory;
            Logger = loggerFactory.CreateLogger(classType);
        }

        public virtual void Dispose() => _testLog.Dispose();
    }
}
