// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2024. All rights reserved.
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY, without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Threading.Tasks;

using ArmoniK.Api.Client.Options;
using ArmoniK.Api.Client.Submitter;
using ArmoniK.Utils;

using Grpc.Core;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Formatting.Compact;

namespace ArmoniK.Contrib.DependencyExplorer;

internal static class Program
{
  private static async Task Main(string[] args)
  {
    var builder = new ConfigurationBuilder().AddEnvironmentVariables()
                                            .AddCommandLine(args);
    var configuration = builder.Build();
    var seriLogger = new LoggerConfiguration().ReadFrom.Configuration(configuration)
                                              .Enrich.FromLogContext()
                                              .WriteTo.Console(new CompactJsonFormatter())
                                              .CreateLogger();

    var loggerFactory = new LoggerFactory().AddSerilog(seriLogger);

    var options = configuration.GetRequiredSection(GrpcClient.SettingSection)
                               .Get<GrpcClient>();

    var channelPool = new ObjectPool<ChannelBase>(() => GrpcChannelFactory.CreateChannel(options!));

    var exp = new DependencyExplorer(channelPool,
                                     configuration.GetValue<string>("session") ?? throw new InvalidOperationException("<session> parameter should not be empty"),
                                     loggerFactory.CreateLogger<DependencyExplorer>());

    await exp.GetCreatedResults();
    await exp.GetTasks();
    await exp.CheckResults();
  }
}
