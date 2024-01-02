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

using ArmoniK.Api.gRPC.V1;
using ArmoniK.Api.gRPC.V1.Results;

namespace ArmoniK.Contrib.DependencyExplorer;

public static class ResultsClientExt
{
  /// <summary>
  ///   Filter tasks on their sessionId
  /// </summary>
  /// <param name="sessionId"> the session id to filter on </param>
  /// <returns>the gRPC filter</returns>
  public static FiltersAnd FilterCreatedResults(string sessionId)
    => new()
       {
         And =
         {
           new FilterField
           {
             Field = new ResultField
                     {
                       ResultRawField = new ResultRawField
                                        {
                                          Field = ResultRawEnumField.SessionId,
                                        },
                     },
             FilterString = new FilterString
                            {
                              Value    = sessionId,
                              Operator = FilterStringOperator.Equal,
                            },
           },

           new FilterField
           {
             Field = new ResultField
                     {
                       ResultRawField = new ResultRawField
                                        {
                                          Field = ResultRawEnumField.Status,
                                        },
                     },
             FilterStatus = new FilterStatus
                            {
                              Operator = FilterStatusOperator.Equal,
                              Value    = ResultStatus.Created,
                            },
           },
         },
       };


  /// <summary>
  ///   List results while handling page size
  /// </summary>
  /// <param name="resultsClient"> the results client </param>
  /// <param name="filters"> filters to apply on the results </param>
  /// <param name="sort"> sorting order </param>
  /// <param name="pageSize"> page size </param>
  /// <returns></returns>
  public static async IAsyncEnumerable<ResultRaw> ListResultsAsync(this Results.ResultsClient    resultsClient,
                                                                   Filters                       filters,
                                                                   ListResultsRequest.Types.Sort sort,
                                                                   int                           pageSize = 50)
  {
    var                 page = 0;
    ListResultsResponse res;
    while ((res = await resultsClient.ListResultsAsync(new ListResultsRequest
                                                       {
                                                         Filters  = filters,
                                                         Sort     = sort,
                                                         PageSize = pageSize,
                                                         Page     = page,
                                                       })).Results.Count != 0)
    {
      foreach (var taskSummary in res.Results)
      {
        yield return taskSummary;
      }

      page++;
    }
  }
}
