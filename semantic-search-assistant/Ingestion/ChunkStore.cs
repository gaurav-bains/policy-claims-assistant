using Npgsql;
using Pgvector;

namespace SemanticSearchAssistant.Ingestion;

public record ChunkSearchResult(
    string SourceDocument,
    int ChunkIndex,
    string PageReference,
    string Content,
    double Similarity);

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

        await using var command = connection.CreateCommand();
        command.CommandText = schemaSql;
        await command.ExecuteNonQueryAsync(cancellationToken);

        // Refresh the type catalog so the pool picks up the vector type
        // immediately after CREATE EXTENSION, in case this is a fresh database.
        await connection.ReloadTypesAsync();
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

    public async Task<List<ChunkSearchResult>> SearchAsync(
        float[] queryEmbedding,
        int topK,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT source_document, chunk_index, page_reference, content, 1 - (embedding <=> $1) AS similarity
            FROM document_chunks
            ORDER BY embedding <=> $1
            LIMIT $2
            """;
        command.Parameters.AddWithValue(new Vector(queryEmbedding));
        command.Parameters.AddWithValue(topK);

        var results = new List<ChunkSearchResult>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new ChunkSearchResult(
                reader.GetString(0),
                reader.GetInt32(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetDouble(4)));
        }

        return results;
    }
}
