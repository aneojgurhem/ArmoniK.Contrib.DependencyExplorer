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

using ArmoniK.Api.gRPC.V1;
using ArmoniK.Api.gRPC.V1.Tasks;

namespace ArmoniK.Contrib.DependencyExplorer;

public static class TasksClientExt
{
  /// <summary>
  ///   Filter on task id
  /// </summary>
  /// <param name="taskId"> the task id to filter on </param>
  /// <returns></returns>
  public static FiltersAnd TaskIdFilter(string taskId)
    => new()
       {
         And =
         {
           new FilterField
           {
             Field = new TaskField
                     {
                       TaskSummaryField = new TaskSummaryField
                                          {
                                            Field = TaskSummaryEnumField.TaskId,
                                          },
                     },
             FilterString = new FilterString
                            {
                              Value    = taskId,
                              Operator = FilterStringOperator.Equal,
                            },
           },
         },
       };

  /// <summary>
  ///   Filter on task id
  /// </summary>
  /// <param name="taskId"> the task id to filter on </param>
  /// <returns></returns>
  public static FiltersAnd TaskInitialIdFilter(string taskId)
    => new()
       {
         And =
         {
           new FilterField
           {
             Field = new TaskField
                     {
                       TaskSummaryField = new TaskSummaryField
                                          {
                                            Field = TaskSummaryEnumField.InitialTaskId,
                                          },
                     },
             FilterString = new FilterString
                            {
                              Value    = taskId,
                              Operator = FilterStringOperator.Equal,
                            },
           },
         },
       };

  /// <summary>
  ///   Filter tasks on their sessionId
  /// </summary>
  /// <param name="sessionId"> the session id to filter on </param>
  /// <returns></returns>
  public static FiltersAnd TaskSessionIdFilter(string sessionId)
    => new()
       {
         And =
         {
           new FilterField
           {
             Field = new TaskField
                     {
                       TaskSummaryField = new TaskSummaryField
                                          {
                                            Field = TaskSummaryEnumField.SessionId,
                                          },
                     },
             FilterString = new FilterString
                            {
                              Value    = sessionId,
                              Operator = FilterStringOperator.Equal,
                            },
           },
         },
       };

  /// <summary>
  ///   Filter on task status and session id
  /// </summary>
  /// <param name="status"> the task status to filter on </param>
  /// <param name="sessionId"> the session id to filter on </param>
  /// <returns></returns>
  public static FiltersAnd TaskStatusFilter(TaskStatus status,
                                            string     sessionId)
    => new()
       {
         And =
         {
           new FilterField
           {
             Field = new TaskField
                     {
                       TaskSummaryField = new TaskSummaryField
                                          {
                                            Field = TaskSummaryEnumField.Status,
                                          },
                     },
             FilterStatus = new FilterStatus
                            {
                              Operator = FilterStatusOperator.Equal,
                              Value    = status,
                            },
           },
           new FilterField
           {
             Field = new TaskField
                     {
                       TaskSummaryField = new TaskSummaryField
                                          {
                                            Field = TaskSummaryEnumField.SessionId,
                                          },
                     },
             FilterString = new FilterString
                            {
                              Operator = FilterStringOperator.Equal,
                              Value    = sessionId,
                            },
           },
         },
       };

  /// <summary>
  ///   List tasks while handling page size
  /// </summary>
  /// <param name="tasksClient"> the tasks client </param>
  /// <param name="filters"> filters to apply on the tasks </param>
  /// <param name="sort"> sorting order </param>
  /// <param name="pageSize"> page size </param>
  /// <returns></returns>
  public static async IAsyncEnumerable<TaskDetailed> ListTasksAsync(this Tasks.TasksClient      tasksClient,
                                                                    Filters                     filters,
                                                                    ListTasksRequest.Types.Sort sort,
                                                                    int                         pageSize = 50)
  {
    var                       page = 0;
    ListTasksDetailedResponse res;
    while ((res = await tasksClient.ListTasksDetailedAsync(new ListTasksRequest
                                                           {
                                                             Filters  = filters,
                                                             Sort     = sort,
                                                             PageSize = pageSize,
                                                             Page     = page,
                                                           })).Tasks.Any())
    {
      foreach (var taskDetailed in res.Tasks)
      {
        yield return taskDetailed;
      }

      page++;
    }
  }
}
