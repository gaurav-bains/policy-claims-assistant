using Npgsql;
using Pgvector;

namespace SemanticSearchAssistant.Ingestion;

public class ChunkStore
{
    private readonly NpgsqlDataSource _dataSource;

    public ChunkStore(string connectionString)
    {
        var builder = new NpgsqlDataSourceBuilder(connectionString);
        builder.UseVector();
        _dataSource = builder.Build();
    }

    public async Task EnsureSchemaAsync(CancellationToken cancellationToken = default)
    {
        var schemaPath = Path.Combine(AppContext.BaseDirectory, "db", "schema.sql");
        var schemaSql = await File.ReadAllTextAsync(schemaPath, cancellationToken);

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ReloadTypesAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = schemaSql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task InsertChunkAsync(
        string sourceDocument,
        int chunkIndex,
        string pageReference,
        string content,
        float[] embedding,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO document_chunks (source_document, chunk_index, page_reference, content, embedding)
            VALUES ($1, $2, $3, $4, $5)
            """;
        command.Parameters.AddWithValue(sourceDocument);
        command.Parameters.AddWithValue(chunkIndex);
        command.Parameters.AddWithValue(pageReference);
        command.Parameters.AddWithValue(content);
        command.Parameters.AddWithValue(new Vector(embedding));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
