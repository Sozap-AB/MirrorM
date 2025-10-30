using MirrorM.AdapterInterface;
using MirrorM.AdapterInterface.Query;
using MirrorM.AdapterInterface.Query.Connections;
using MirrorM.Common;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace MirrorM.Npgsql.Internal
{
    internal class Connection : IDatabaseConnection
    {
        private class CommandBatchFailedException : Exception
        {
            public NpgsqlBatch Batch { get; private set; }

            public CommandBatchFailedException(NpgsqlBatch batch) : base("Batch failed")
            {
                Batch = batch;
            }
        }

        private NpgsqlConnection PgConnection { get; }

        public Connection(NpgsqlConnection connection)
        {
            PgConnection = connection;
        }

        private NpgsqlCommand CreateCommandFromExpression(SqlExpression sqlExpression)
        {
            return CreateCommandFromSqlAndParameters(sqlExpression.SqlString, sqlExpression.Parameters);
        }

        private NpgsqlCommand CreateCommandFromSqlAndParameters(string sql, IEnumerable<SqlParameter> parameters)
        {
            var cmd = new NpgsqlCommand(sql, PgConnection);

            foreach (var parameter in parameters)
            {
                cmd.Parameters.AddWithValue(parameter.Name, parameter.Value ?? DBNull.Value);
            }

            return cmd;
        }

        public async IAsyncEnumerable<T> ExecuteSelectAsync<T>(IEntityQuerySchema schema, Func<IDataReader, IEnumerable<T>> recordHandler)
        {
            var sql = SqlExpressionGenerator.GenerateEntitySelectSql(schema);

            await using var cmd = CreateCommandFromExpression(sql);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
                foreach (var item in recordHandler(reader))
                    yield return item;
        }

        public async Task<R> ExecuteSumAsync<R>(IEntityAggregateQuerySchema schema)
        {
            var sql = SqlExpressionGenerator.GenerateEntitySumSql(schema);

            await using var cmd = CreateCommandFromExpression(sql);

            return (R)Convert.ChangeType((await cmd.ExecuteScalarAsync())!, typeof(R));
        }

        public async Task<R?> ExecuteMaxAsync<R>(IEntityAggregateQuerySchema schema) where R : struct
        {
            var sql = SqlExpressionGenerator.GenerateEntityMaxSql(schema);

            await using var cmd = CreateCommandFromExpression(sql);
            await using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return !(await reader.IsDBNullAsync(0)) ? await reader.GetFieldValueAsync<R>(0) : null;
            }
            else
            {
                throw new InvalidOperationException("No rows provided for max query");
            }
        }

        public async Task<int> ExecuteCountAsync(IEntityAggregateQuerySchema schema)
        {
            var sql = SqlExpressionGenerator.GenerateEntityCountSql(schema);

            await using var cmd = CreateCommandFromExpression(sql);

            return (int)(await cmd.ExecuteScalarAsync())!;
        }

        public async Task<bool> ExecuteExistsAsync(IEntityAggregateQuerySchema schema)
        {
            var sql = SqlExpressionGenerator.GenerateEntityExistsSql(schema);

            await using var cmd = CreateCommandFromExpression(sql);

            return (bool)(await cmd.ExecuteScalarAsync())!;
        }

        public async IAsyncEnumerable<T> ExecuteRawSelectAsync<T>(string sql, SqlParameter[] parameters, Func<IDataReader, T> recordHandler)
        {
            await using var cmd = CreateCommandFromSqlAndParameters(sql, parameters);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
                yield return recordHandler(reader);
        }

        public async Task<int> ExecuteRawCommandAsync(string sql, SqlParameter[] parameters)
        {
            await using var cmd = new NpgsqlCommand(sql, PgConnection);

            foreach (var parameter in parameters)
            {
                cmd.Parameters.AddWithValue(parameter.Name, parameter.Value ?? DBNull.Value);
            }

            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task ExecuteWrappedInTransactionAsync(Func<Task> action)
        {
            await using NpgsqlTransaction transaction = await PgConnection.BeginTransactionAsync();

            try
            {
                await action();

                await transaction.CommitAsync();
            }
            catch (CommandBatchFailedException b)
            {
                await transaction.RollbackAsync();

                await FindFailingCommandInBatchAsync(b.Batch);
            }
            catch
            {
                await transaction.RollbackAsync();

                throw;
            }
        }

        private (object, NpgsqlDbType?) MapValueForSaving(object? v)
        {
            if (v == null)
            {
                return (DBNull.Value, null);
            }

            if (v is JsonNode node) //TODO: should it be here? (maybe add mechanics of custom converters)
            {
                return (node.ToJsonString(), NpgsqlDbType.Jsonb);
            }

            return (v, null);
        }

        public async Task ExecuteCommandBatchAsync(IEnumerable<IModificationSchema> expressions, bool createTransaction)
        {
            NpgsqlBatchCommand CreateBatchCommandFromSqlExpression(SqlExpression exp)
            {
                var command = new NpgsqlBatchCommand(exp.SqlString);

                foreach (var p in exp.Parameters)
                {
                    var (value, type) = MapValueForSaving(p.Value);

                    if (!type.HasValue)
                    {
                        command.Parameters.AddWithValue(p.Name, value);
                    }
                    else
                    {
                        command.Parameters.AddWithValue(p.Name, type.Value, value);
                    }
                }

                return command;
            }

            //TODO: connection schemas might be batched together for better performance

            IEnumerable<NpgsqlBatchCommand> commands = expressions.Select(e =>
            {
                switch (e)
                {
                    case IEntityInsertSchema insert:
                        return CreateBatchCommandFromSqlExpression(SqlExpressionGenerator.GenerateEntityInsertSql(insert));
                    case IEntityUpdateSchema update:
                        return CreateBatchCommandFromSqlExpression(SqlExpressionGenerator.GenerateEntityUpdateSql(update));
                    case IEntityDeleteSchema delete:
                        return CreateBatchCommandFromSqlExpression(SqlExpressionGenerator.GenerateEntityDeleteSql(delete));
                    case IConnectionInsertSchema connectionInsert:
                        return CreateBatchCommandFromSqlExpression(SqlExpressionGenerator.GenerateConnectionInsertSql(connectionInsert));
                    case IConnectionDeleteSchema connectionDelete:
                        return CreateBatchCommandFromSqlExpression(SqlExpressionGenerator.GenerateConnectionDeleteSql(connectionDelete));
                    default:
                        throw new NotSupportedException($"Modification type {e.GetType()} is not supported.");
                }
            });

            if (!commands.Any())
            {
                return;
            }

            await using var batch = new NpgsqlBatch(PgConnection);

            if (createTransaction)
            {
                batch.BatchCommands.Add(new NpgsqlBatchCommand("SET CONSTRAINTS ALL DEFERRED"));
            }

            foreach (var com in commands)
            {
                batch.BatchCommands.Add(com);
            }

            int expectedUpdated = createTransaction ? batch.BatchCommands.Count - 1 : batch.BatchCommands.Count;

            //OnExecuteSql(batch); //TODO: restore this functionality

            NpgsqlTransaction? transaction = createTransaction ? await PgConnection.BeginTransactionAsync() : null;
            try
            {
                var updated = await batch.ExecuteNonQueryAsync();

                if (updated != expectedUpdated)
                {
                    throw new CommandBatchFailedException(batch);
                }

                await (transaction?.CommitAsync() ?? Task.CompletedTask);
            }
            catch (CommandBatchFailedException b)
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync();

                    await FindFailingCommandInBatchAsync(b.Batch);
                }
                else
                {
                    throw;
                }
            }
            catch
            {
                await (transaction?.RollbackAsync() ?? Task.CompletedTask);

                throw;
            }
        }

        private async Task FindFailingCommandInBatchAsync(NpgsqlBatch batch)
        {
            var transaction = await PgConnection.BeginTransactionAsync();

            try
            {
                foreach (NpgsqlBatchCommand command in batch.BatchCommands.Cast<NpgsqlBatchCommand>())
                {
                    var immediateCommand = new NpgsqlCommand(command.CommandText, PgConnection, transaction);

                    CopyParameters(command, immediateCommand);

                    var updated = await immediateCommand.ExecuteNonQueryAsync();

                    if (!IsEntityCommand(command))
                    {
                        continue;
                    }

                    if (updated != 1)
                    {
                        throw new InvalidOperationException($"Command \"{command.CommandText}\" didn't affect anything");
                    }
                }
            }
            finally
            {
                await transaction.RollbackAsync();
            }
        }

        private void CopyParameters(NpgsqlBatchCommand from, NpgsqlCommand to)
        {
            to.Parameters.AddRange(from.Parameters.Select(x => new NpgsqlParameter(x.ParameterName, x.Value)
            {
                NpgsqlDbType = x.NpgsqlDbType
            }).ToArray());
        }

        private static bool IsEntityCommand(NpgsqlBatchCommand command)
        {
            return command.CommandText.StartsWith("INSERT INTO") ||
                   command.CommandText.StartsWith("UPDATE") ||
                   command.CommandText.StartsWith("DELETE FROM");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            PgConnection.Dispose();
        }
    }
}
