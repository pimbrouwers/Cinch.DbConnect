﻿using FastMember;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Cinch.DbConnect
{
    public static class SqlConnectionExtensions
    {
        public static void OpenConnection(this SqlConnection conn)
        {
            if (conn.State != ConnectionState.Open)
                conn.Open();
        }

        public static async Task OpenConnectionAsync(this SqlConnection conn)
        {
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();
        }

        public static SqlTransaction BeginTrans(this SqlConnection conn)
        {
            conn.OpenConnection();

            var trans = conn.BeginTransaction();
            return trans;
        }

        public static async Task<SqlTransaction> BeginTransAsync(this SqlConnection conn)
        {
            await conn.OpenConnectionAsync();

            var trans = conn.BeginTransaction();
            return trans;
        }

        #region Commands
        public static int Execute(this SqlConnection conn, ISqlCommandBuilder cmdBuilder, Action<SqlCommand> afterExecution = null)
        {
            int recordsAffected = 0;

            using (var cmd = cmdBuilder.SetConnection(conn).Build())
            {
                cmd.Connection.OpenConnection();
                recordsAffected = cmd.ExecuteNonQuery();

                afterExecution?.Invoke(cmd);
            }

            return recordsAffected;
        }

        public static T Execute<T>(this SqlConnection conn, ISqlCommandBuilder cmdBuilder, Action<SqlCommand> afterExecution = null)
        {
            using (var cmd = cmdBuilder.SetConnection(conn).Build())
            using (var dbReader = cmd.GetReader())
            {
                afterExecution?.Invoke(cmd);

                return dbReader.Enumerate<T>().FirstOrDefault();
            }
        }

        public static async Task<int> ExecuteAsync(this SqlConnection conn, ISqlCommandBuilder cmdBuilder, Action<SqlCommand> afterExecution = null)
        {
            int recordsAffected = 0;

            using (var cmd = cmdBuilder.SetConnection(conn).Build())
            {
                await cmd.Connection.OpenConnectionAsync();
                recordsAffected = await cmd.ExecuteNonQueryAsync();

                afterExecution?.Invoke(cmd);
            }

            return recordsAffected;
        }

        public static async Task<T> ExecuteAsync<T>(this SqlConnection conn, ISqlCommandBuilder cmdBuilder, Action<SqlCommand> afterExecution = null)
        {
            using (var cmd = cmdBuilder.SetConnection(conn).Build())
            using (var dbReader = await cmd.GetReaderAsync())
            {
                afterExecution?.Invoke(cmd);

                return dbReader.Enumerate<T>().FirstOrDefault();
            }
        }
        #endregion

        #region Queries
        public static IEnumerable<T> Enumerate<T>(this SqlConnection conn, ISqlCommandBuilder cmdBuilder)
        {
            using (var dbReader = conn.Reader(cmdBuilder))
            {
                return dbReader.Enumerate<T>();
            }
        }

        public static async Task<IEnumerable<T>> EnumerateAsync<T>(this SqlConnection conn, ISqlCommandBuilder cmdBuilder)
        {
            using (var dbReader = await conn.ReaderAsync(cmdBuilder))
            {
                return await dbReader.EnumerateAsync<T>();
            }
        }
        #endregion

        #region Reader
        public static IDbReader Reader(this SqlConnection conn, ISqlCommandBuilder cmdBuilder)
        {
            var cmd = cmdBuilder.SetConnection(conn)
                                .Build();

            cmd.Connection.OpenConnection();
            
            var rd = cmd.GetReader();

            return new DbReader(cmd, rd);
        }

        public static async Task<IDbReader> ReaderAsync(this SqlConnection conn, ISqlCommandBuilder cmdBuilder)
        {
            var cmd = cmdBuilder.SetConnection(conn)
                                .Build();
                                
            await cmd.Connection.OpenConnectionAsync();

            var rd = await cmd.GetReaderAsync();

            return new DbReader(cmd, rd);
        }
        #endregion

        #region Bulk Copy
        public static void Bulk<T>(this SqlConnection conn, SqlBulkCopyBuilder bcpBuilder, IEnumerable<T> srcData, IEnumerable<string> ignoreCols = null) where T : class, new()
        {
            conn.OpenConnection();

            using (var bcp = bcpBuilder.SetConnection(conn).Build())
            {
                bcp.MapColumns<T>(ignoreCols);

                using (var dataReader = ObjectReader.Create(srcData))
                {
                    bcp.WriteToServer(dataReader);
                }
            }
        }

        public static async Task BulkAsync<T>(this SqlConnection conn, SqlBulkCopyBuilder bcpBuilder, IEnumerable<T> srcData, IEnumerable<string> ignoreCols = null) where T : class, new()
        {
            await conn.OpenConnectionAsync();

            using (var bcp = bcpBuilder.SetConnection(conn).Build())
            {
                bcp.MapColumns<T>(ignoreCols);

                using (var dataReader = ObjectReader.Create(srcData))
                {
                    await bcp.WriteToServerAsync(dataReader);
                }
            }
        }
        #endregion
    }
}
