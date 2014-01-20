﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlServerCe;
using Nohros.Data;
using Nohros.Extensions;
using Nohros.Logging;
using Nohros.Resources;

namespace Nohros.Data.SqlCe
{
  public class AddStateQuery
  {
    const string kClassName = "Nohros.Data.SqlServer.AddStateQuery";

    readonly MustLogger logger_ = MustLogger.ForCurrentProcess;
    readonly SqlCeConnectionProvider sql_connection_provider_;

    public AddStateQuery(SqlCeConnectionProvider sql_connection_provider) {
      sql_connection_provider_ = sql_connection_provider;
      logger_ = MustLogger.ForCurrentProcess;
    }

    public void Execute(string state_name, string table_name, object state) {
      using (SqlCeConnection conn = sql_connection_provider_.CreateConnection())
      using (var builder = new CommandBuilder(conn)) {
        IDbCommand cmd = builder
          .SetText(@"
insert into " + table_name + @"(name, state)
values(@name, @state)")
          .SetType(CommandType.Text)
          .AddParameter("@name", state_name)
          .AddParameterWithValue("@state", state)
          .Build();
        try {
          conn.Open();
          cmd.ExecuteNonQuery();
        } catch (SqlCeException e) {
          throw new ProviderException(e);
        }
      }
    }
  }
}