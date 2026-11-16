using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using MySqlConnector;

const string ConnStr = "Server=localhost;Port=3306;Database=tudo10;User ID=root;Password=SUA_SENHA;";

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins("http://127.0.0.1:5500", "http://localhost:5500")
    .AllowAnyHeader().AllowAnyMethod()));
var app = builder.Build();
app.UseCors();

async Task<MySqlConnection> Abrir() { var c = new MySqlConnection(ConnStr); await c.OpenAsync(); return c; }

string Hash(string senha)
{
    var salt = RandomNumberGenerator.GetBytes(16);
    var h = Rfc2898DeriveBytes.Pbkdf2(senha, salt, 100_000, HashAlgorithmName.SHA256, 32);
    return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(h);
}
bool Confere(string senha, string guardado)
{
    var p = guardado.Split(':');
    var h = Rfc2898DeriveBytes.Pbkdf2(senha, Convert.FromBase64String(p[0]), 100_000, HashAlgorithmName.SHA256, 32);
    return CryptographicOperations.FixedTimeEquals(h, Convert.FromBase64String(p[1]));
}

string? Validar(Usuario u, bool comSenha) =>
    string.IsNullOrWhiteSpace(u.Nome) || u.Nome.Length > 50 ? "Nome inválido." :
    !u.Email.Contains('@') || u.Email.Length > 60 ? "E-mail inválido." :
    u.Cpf.Length != 14 ? "CPF inválido." :
    u.Telefone.Length < 14 || u.Telefone.Length > 15 ? "Telefone inválido." :
    u.Cep.Length != 9 ? "CEP inválido." :
    string.IsNullOrWhiteSpace(u.Endereco) || u.Endereco.Length > 80 ? "Endereço inválido." :
    comSenha && (u.Senha.Length < 6 || u.Senha.Length > 20) ? "A senha deve ter de 6 a 20 caracteres." : null;

async Task<List<Dictionary<string, object?>>> ListarPedidos(int? usuarioId, int? numero = null)
{
    await using var c = await Abrir();
    var lista = new List<Dictionary<string, object?>>();
    await using (var cmd = new MySqlCommand(@"SELECT p.numero, p.criado_em, p.endereco, p.pagamento, p.observacoes,
            p.status, p.total, u.nome, u.email, u.cpf, u.telefone
        FROM pedidos p JOIN usuarios u ON u.id = p.usuario_id
        WHERE (@u IS NULL OR p.usuario_id = @u) AND (@n IS NULL OR p.numero = @n) ORDER BY p.numero", c))
    {
        cmd.Parameters.AddWithValue("@u", (object?)usuarioId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@n", (object?)numero ?? DBNull.Value);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            lista.Add(new()
            {
                ["numero"] = r.GetInt32(0), ["criadoEm"] = r.GetDateTime(1), ["endereco"] = r.GetString(2),
                ["pagamento"] = r.GetString(3), ["observacoes"] = r.GetString(4), ["status"] = r.GetString(5),
                ["total"] = r.GetDecimal(6),
                ["cliente"] = new { nome = r.GetString(7), email = r.GetString(8), cpf = r.GetString(9), telefone = r.GetString(10) },
                ["itens"] = new List<object>()
            });
    }
    foreach (var p in lista)
    {
        await using var cmd = new MySqlCommand(@"SELECT i.produto_id, pr.nome, i.quantidade, i.preco_unitario, i.subtotal
            FROM itens_pedido i JOIN produtos pr ON pr.id = i.produto_id WHERE i.pedido_numero = @n", c);
        cmd.Parameters.AddWithValue("@n", p["numero"]);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            ((List<object>)p["itens"]!).Add(new { produtoId = r.GetInt32(0), nome = r.GetString(1),
                quantidade = r.GetInt32(2), precoUnitario = r.GetDecimal(3), subtotal = r.GetDecimal(4) });
    }
    return lista;
}

async Task ExportarJson()
{
    var op = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    await File.WriteAllTextAsync("pedidos.json", JsonSerializer.Serialize(await ListarPedidos(null), op));
}

app.MapGet("/api/produtos", async () =>
{
    await using var c = await Abrir();
    await using var cmd = new MySqlCommand("SELECT id, nome, categoria, preco FROM produtos WHERE ativo = 1 ORDER BY id", c);
    await using var r = await cmd.ExecuteReaderAsync();
    var l = new List<object>();
    while (await r.ReadAsync()) l.Add(new { id = r.GetInt32(0), nome = r.GetString(1), cat = r.GetString(2), preco = r.GetDecimal(3) });
    return l;
});

app.MapPost("/api/usuarios", async (Usuario u) =>
{
    var erro = Validar(u, true);
    if (erro != null) return Results.BadRequest(erro);
    u = u with { Email = u.Email.Trim().ToLower() };
    try
    {
        await using var c = await Abrir();
        await using var cmd = new MySqlCommand(@"INSERT INTO usuarios (nome, email, cpf, telefone, cep, endereco, senha_hash)
            VALUES (@n, @e, @c, @t, @cep, @end, @s)", c);
        cmd.Parameters.AddWithValue("@n", u.Nome.Trim());
        cmd.Parameters.AddWithValue("@e", u.Email);
        cmd.Parameters.AddWithValue("@c", u.Cpf);
        cmd.Parameters.AddWithValue("@t", u.Telefone);
        cmd.Parameters.AddWithValue("@cep", u.Cep);
        cmd.Parameters.AddWithValue("@end", u.Endereco.Trim());
        cmd.Parameters.AddWithValue("@s", Hash(u.Senha));
        await cmd.ExecuteNonQueryAsync();
        return Results.Created($"/api/usuarios/{cmd.LastInsertedId}", u with { Id = (int)cmd.LastInsertedId, Senha = "" });
    }
    catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
    {
        return Results.Conflict("E-mail ou CPF já cadastrado.");
    }
});

app.MapPost("/api/login", async (LoginReq l) =>
{
    await using var c = await Abrir();
    await using var cmd = new MySqlCommand("SELECT id, nome, email, cpf, telefone, cep, endereco, senha_hash FROM usuarios WHERE email = @e", c);
    cmd.Parameters.AddWithValue("@e", l.Email.Trim().ToLower());
    await using var r = await cmd.ExecuteReaderAsync();
    if (!await r.ReadAsync() || !Confere(l.Senha, r.GetString(7))) return Results.Unauthorized();
    return Results.Ok(new Usuario(r.GetInt32(0), r.GetString(1), r.GetString(2), r.GetString(3),
        r.GetString(4), r.GetString(5), r.GetString(6), ""));
});

app.MapPut("/api/usuarios/{id:int}", async (int id, Usuario u) =>
{
    var erro = Validar(u, false);
    if (erro != null) return Results.BadRequest(erro);
    await using var c = await Abrir();
    await using var cmd = new MySqlCommand("UPDATE usuarios SET nome=@n, telefone=@t, cep=@cep, endereco=@end WHERE id=@id", c);
    cmd.Parameters.AddWithValue("@n", u.Nome.Trim());
    cmd.Parameters.AddWithValue("@t", u.Telefone);
    cmd.Parameters.AddWithValue("@cep", u.Cep);
    cmd.Parameters.AddWithValue("@end", u.Endereco.Trim());
    cmd.Parameters.AddWithValue("@id", id);
    return await cmd.ExecuteNonQueryAsync() >= 0 ? Results.Ok(u with { Id = id, Senha = "" }) : Results.NotFound();
});

app.MapDelete("/api/usuarios/{id:int}", async (int id) =>
{
    await using var c = await Abrir();
    await using var cmd = new MySqlCommand("DELETE FROM usuarios WHERE id = @id", c);
    cmd.Parameters.AddWithValue("@id", id);
    var apagou = await cmd.ExecuteNonQueryAsync() > 0;
    if (apagou) await ExportarJson();
    return apagou ? Results.NoContent() : Results.NotFound();
});

app.MapGet("/api/pedidos", async (int? usuarioId) => await ListarPedidos(usuarioId));

app.MapGet("/api/pedidos/{n:int}", async (int n) =>
{
    var l = await ListarPedidos(null, n);
    return l.Count > 0 ? Results.Ok(l[0]) : Results.NotFound();
});

app.MapPost("/api/pedidos", async (NovoPedido n) =>
{
    if (n.Itens == null || n.Itens.Count == 0) return Results.BadRequest("O pedido precisa ter itens.");
    if (string.IsNullOrWhiteSpace(n.Endereco) || n.Endereco.Length > 80) return Results.BadRequest("Endereço inválido.");
    try
    {
        await using var c = await Abrir();
        await using var tx = await c.BeginTransactionAsync();
        var itens = new List<(int id, int qtd, decimal preco)>();
        foreach (var i in n.Itens)
        {
            if (i.Quantidade < 1 || i.Quantidade > 20) return Results.BadRequest("Quantidade inválida.");
            await using var q = new MySqlCommand("SELECT preco FROM produtos WHERE id = @id AND ativo = 1", c, tx);
            q.Parameters.AddWithValue("@id", i.ProdutoId);
            var preco = await q.ExecuteScalarAsync();   // o preço vem do banco, não do site
            if (preco == null) return Results.BadRequest($"Produto {i.ProdutoId} não existe.");
            itens.Add((i.ProdutoId, i.Quantidade, Convert.ToDecimal(preco)));
        }
        await using var ins = new MySqlCommand(@"INSERT INTO pedidos (usuario_id, endereco, pagamento, observacoes, total)
            VALUES (@u, @e, @p, @o, @t)", c, tx);
        ins.Parameters.AddWithValue("@u", n.UsuarioId);
        ins.Parameters.AddWithValue("@e", n.Endereco.Trim());
        ins.Parameters.AddWithValue("@p", n.Pagamento);
        ins.Parameters.AddWithValue("@o", n.Observacoes ?? "");
        ins.Parameters.AddWithValue("@t", itens.Sum(i => i.qtd * i.preco));
        await ins.ExecuteNonQueryAsync();
        int numero = (int)ins.LastInsertedId;
        foreach (var i in itens)
        {
            await using var it = new MySqlCommand(@"INSERT INTO itens_pedido (pedido_numero, produto_id, quantidade, preco_unitario, subtotal)
                VALUES (@n, @p, @q, @pr, @s)", c, tx);
            it.Parameters.AddWithValue("@n", numero);
            it.Parameters.AddWithValue("@p", i.id);
            it.Parameters.AddWithValue("@q", i.qtd);
            it.Parameters.AddWithValue("@pr", i.preco);
            it.Parameters.AddWithValue("@s", i.qtd * i.preco);
            await it.ExecuteNonQueryAsync();
        }
        await tx.CommitAsync();
        await ExportarJson();
        return Results.Created($"/api/pedidos/{numero}", new { numero });
    }
    catch (MySqlException) { return Results.BadRequest("Não foi possível salvar o pedido (usuário inválido?)."); }
});

app.MapPut("/api/pedidos/{n:int}", async (int n, StatusReq s) =>
{
    await using var c = await Abrir();
    await using var cmd = new MySqlCommand("UPDATE pedidos SET status = @s WHERE numero = @n", c);
    cmd.Parameters.AddWithValue("@s", s.Status);
    cmd.Parameters.AddWithValue("@n", n);
    var ok = await cmd.ExecuteNonQueryAsync() >= 0;
    if (ok) await ExportarJson();
    return ok ? Results.Ok() : Results.NotFound();
});

app.MapDelete("/api/pedidos/{n:int}", async (int n) =>
{
    await using var c = await Abrir();
    await using var cmd = new MySqlCommand("DELETE FROM pedidos WHERE numero = @n", c);
    cmd.Parameters.AddWithValue("@n", n);
    var apagou = await cmd.ExecuteNonQueryAsync() > 0;
    if (apagou) await ExportarJson();
    return apagou ? Results.NoContent() : Results.NotFound();
});

app.Run("http://localhost:5000");

// ---------- Modelos ----------
record Usuario(int Id, string Nome, string Email, string Cpf, string Telefone, string Cep, string Endereco, string Senha);
record LoginReq(string Email, string Senha);
record ItemReq(int ProdutoId, int Quantidade);
record NovoPedido(int UsuarioId, string Endereco, string Pagamento, string? Observacoes, List<ItemReq>? Itens);
record StatusReq(string Status);
