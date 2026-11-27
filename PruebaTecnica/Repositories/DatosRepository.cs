using System.Data;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using PruebaTecnica.DTOs;
using PruebaTecnica.Entities;
using PruebaTecnica.Infrastructure;

namespace PruebaTecnica.Repositories;

public class DatosRepository : IDatosRepository
{
    private readonly OracleConnectionFactory _connectionFactory;

    public DatosRepository(OracleConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    // LISTAR PAGINADO
    public async Task<PaginacionResponse<DatoResponse>> ListarAsync(int pagina,CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();

        command.CommandText = "PKG_DATOS.SP_LISTAR_PAGINADO";
        command.CommandType = CommandType.StoredProcedure;
        command.BindByName = true;

        command.Parameters.Add("p_pagina",OracleDbType.Int32).Value = pagina;

        var totalParameter = command.Parameters.Add("p_total_registros",OracleDbType.Decimal);

        totalParameter.Direction = ParameterDirection.Output;

        var cursorParameter = command.Parameters.Add("p_cursor",OracleDbType.RefCursor);

        cursorParameter.Direction = ParameterDirection.Output;

        await command.ExecuteNonQueryAsync(cancellationToken);

        var datos = new List<DatoResponse>();

        using var cursor = (OracleRefCursor)cursorParameter.Value;
        using var reader = cursor.GetDataReader();

        while (await reader.ReadAsync(cancellationToken))
        {
            datos.Add(new DatoResponse
            {
                Id = reader.GetInt64(reader.GetOrdinal("ID")),

                Codigo = reader.GetString(reader.GetOrdinal("CODIGO")),

                Secuencia = reader.GetInt32(reader.GetOrdinal("SECUENCIA")),

                FechaRecepcion = reader.GetDateTime(reader.GetOrdinal("FECHARECEPCION")),

                Peso = reader.GetDecimal(reader.GetOrdinal("PESO")),

                Descripcion = reader.IsDBNull(reader.GetOrdinal("DESCRIPCION"))
                    ? null : reader.GetString(reader.GetOrdinal("DESCRIPCION")),

                TotalCodigo = reader.GetInt64(reader.GetOrdinal("TOTAL_CODIGO"))
            });
        }

        return new PaginacionResponse<DatoResponse>
        {
            Pagina = pagina,
            RegistrosPorPagina = 20,
            TotalRegistros = ((OracleDecimal)totalParameter.Value).ToInt64(),
            Datos = datos
        };
    }

    // OBTENER POR ID
    public async Task<Dato?> ObtenerAsync(long id,CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();

        command.CommandText = "PKG_DATOS.SP_OBTENER";
        command.CommandType = CommandType.StoredProcedure;
        command.BindByName = true;

        command.Parameters.Add("p_id",OracleDbType.Int64).Value = id;

        var cursorParameter = command.Parameters.Add("p_cursor",OracleDbType.RefCursor);

        cursorParameter.Direction = ParameterDirection.Output;

        await command.ExecuteNonQueryAsync(cancellationToken);

        using var cursor = (OracleRefCursor)cursorParameter.Value;
        using var reader = cursor.GetDataReader();

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new Dato
        {
            Id = reader.GetInt64(reader.GetOrdinal("ID")),

            Codigo = reader.GetString(
                reader.GetOrdinal("CODIGO")),

            Secuencia = reader.GetInt32(
                reader.GetOrdinal("SECUENCIA")),

            FechaRecepcion = reader.GetDateTime(
                reader.GetOrdinal("FECHARECEPCION")),

            Peso = reader.GetDecimal(
                reader.GetOrdinal("PESO")),

            Descripcion = reader.IsDBNull(
                reader.GetOrdinal("DESCRIPCION"))
                ? null
                : reader.GetString(
                    reader.GetOrdinal("DESCRIPCION"))
        };
    }

    
    // ACTUALIZAR DESCRIPCIÓN
    public async Task ActualizarDescripcionAsync(long id,string? descripcion,CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        using var transaction = (OracleTransaction)
        await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            using var command = connection.CreateCommand();

            command.Transaction = transaction;
            command.CommandText =
                "PKG_DATOS.SP_ACTUALIZAR_DESCRIPCION";

            command.CommandType = CommandType.StoredProcedure;
            command.BindByName = true;

            command.Parameters.Add(
                "p_id", OracleDbType.Int64
            ).Value = id;

            command.Parameters.Add(
                "p_descripcion", OracleDbType.Varchar2
            ).Value = (object?)descripcion ?? DBNull.Value;

            await command.ExecuteNonQueryAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);

            throw;
        }
    }

    // ELIMINAR
    public async Task EliminarAsync(long id,CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        using var transaction = (OracleTransaction)
    await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            using var command = connection.CreateCommand();

            command.Transaction = transaction;
            command.CommandText = "PKG_DATOS.SP_ELIMINAR";
            command.CommandType = CommandType.StoredProcedure;
            command.BindByName = true;

            command.Parameters.Add("p_id", OracleDbType.Int64).Value = id;

            await command.ExecuteNonQueryAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);

            throw;
        }
    }

    // INSERTAR
    public async Task<long> InsertarAsync(Dato dato,CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        using var transaction = (OracleTransaction)
    await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            using var command = connection.CreateCommand();

            command.Transaction = transaction;
            command.CommandText = "PKG_DATOS.SP_INSERTAR";
            command.CommandType = CommandType.StoredProcedure;
            command.BindByName = true;

            command.Parameters.Add(
                "p_codigo", OracleDbType.Varchar2
            ).Value = dato.Codigo;

            command.Parameters.Add(
                "p_secuencia", OracleDbType.Int32
            ).Value = dato.Secuencia;

            command.Parameters.Add(
                "p_fecha_recepcion", OracleDbType.TimeStamp
            ).Value = dato.FechaRecepcion;

            command.Parameters.Add(
                "p_peso", OracleDbType.Decimal
            ).Value = dato.Peso;

            command.Parameters.Add(
                "p_descripcion", OracleDbType.Varchar2
            ).Value = (object?)dato.Descripcion ?? DBNull.Value;

            var idParameter = command.Parameters.Add(
                "p_id", OracleDbType.Decimal
            );

            idParameter.Direction = ParameterDirection.Output;

            await command.ExecuteNonQueryAsync(cancellationToken);

            long id = ((OracleDecimal)idParameter.Value).ToInt64();

            await transaction.CommitAsync(cancellationToken);

            return id;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);

            throw;
        }
    }
}