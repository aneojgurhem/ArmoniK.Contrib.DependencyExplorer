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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using ArmoniK.Api.gRPC.V1.Results;
using ArmoniK.Api.gRPC.V1.SortDirection;
using ArmoniK.Api.gRPC.V1.Tasks;
using ArmoniK.Utils;

using Grpc.Core;

using Microsoft.Extensions.Logging;

using Filters = ArmoniK.Api.gRPC.V1.Results.Filters;

namespace ArmoniK.Contrib.DependencyExplorer;

internal class DependencyExplorer(ObjectPool<ChannelBase>     channelPool,
                                  string                      sessionId,
                                  ILogger<DependencyExplorer> logger)
{
  private readonly Dictionary<string, ResultRaw>          results_ = new();
  private readonly Dictionary<string, List<TaskDetailed>> tasks_   = new();

  public async Task GetCreatedResults()
  {
    var results = await channelPool.WithInstanceAsync(channel =>
                                                      {
                                                        var resultsClient = new Results.ResultsClient(channel);
                                                        return resultsClient.ListResultsAsync(new Filters
                                                                                              {
                                                                                                Or =
                                                                                                {
                                                                                                  ResultsClientExt.FilterCreatedResults(sessionId),
                                                                                                },
                                                                                              },
                                                                                              new ListResultsRequest.Types.Sort
                                                                                              {
                                                                                                Field = new ResultField
                                                                                                        {
                                                                                                          ResultRawField = new ResultRawField
                                                                                                                           {
                                                                                                                             Field = ResultRawEnumField.ResultId,
                                                                                                                           },
                                                                                                        },
                                                                                                Direction = SortDirection.Asc,
                                                                                              });
                                                      });

    await foreach (var raw in results)
    {
      results_[raw.ResultId] = raw;
    }
  }


  public async Task GetTasks()
  {
    await GetTasks(results_.Values.Select(r => r.OwnerTaskId));
    await GetTasks(tasks_.Values.SelectMany(l => l.SelectMany(t => t.ParentTaskIds)));
    logger.LogInformation("Retrieved {nb} tasks",
                          tasks_.Values.Sum(l => l.Count));
  }

  private async Task GetTasks(IEnumerable<string> initialTaskIds)
  {
    var tasks = await channelPool.WithInstanceAsync(channel =>
                                                    {
                                                      var client = new Tasks.TasksClient(channel);
                                                      return client.ListTasksAsync(new Api.gRPC.V1.Tasks.Filters
                                                                                   {
                                                                                     Or =
                                                                                     {
                                                                                       initialTaskIds.Select(TasksClientExt.TaskInitialIdFilter),
                                                                                     },
                                                                                   },
                                                                                   new ListTasksRequest.Types.Sort
                                                                                   {
                                                                                     Direction = SortDirection.Asc,
                                                                                     Field = new TaskField
                                                                                             {
                                                                                               TaskSummaryField = new TaskSummaryField
                                                                                                                  {
                                                                                                                    Field = TaskSummaryEnumField.InitialTaskId,
                                                                                                                  },
                                                                                             },
                                                                                   });
                                                    });

    await foreach (var t in tasks)
    {
      if (tasks_.TryGetValue(t.InitialTaskId,
                             out var list))
      {
        list.Add(t);
        tasks_[t.InitialTaskId] = list.DistinctBy(detailed => detailed.Id)
                                      .ToList();
      }
      else
      {
        tasks_.Add(t.InitialTaskId,
                   new List<TaskDetailed>
                   {
                     t,
                   });
      }
    }
  }

  public async Task CheckResults()
  {
    foreach (var raw in results_.Values)
    {
      await CheckResult(raw);
    }
  }

  private async Task CheckResult(ResultRaw result)
  {
    var ownerTaskList = tasks_[result.OwnerTaskId];

    if (ownerTaskList.Count == 1)
    {
      var parentList = tasks_[ownerTaskList.Single()
                                           .ParentTaskIds.Last()];

      logger.LogInformation("{resultId} {ownerTasks} {parentTasks}",
                            result.ResultId,
                            ownerTaskList.Select(t => (t.Id, t.Status)),
                            parentList.Select(t => (t.Id, t.Status)));
    }
    else
    {
      logger.LogInformation("{resultId} {ownerTasks}",
                            result.ResultId,
                            ownerTaskList.Select(t => (t.Id, t.Status)));
    }
  }
}
