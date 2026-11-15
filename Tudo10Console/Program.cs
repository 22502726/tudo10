using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Tudo10Crud
{
    // =====================================================================
    // MODELO: um pedido do delivery TUDO 10 (tudo custa R$ 10,00)
    // =====================================================================
    class Pedido
    {
        public const decimal PrecoUnitario = 10m;

        public int Numero { get; set; }
        public string Cliente { get; set; } = "";
        public string Telefone { get; set; } = "";
        public string Endereco { get; set; } = "";
        public string Produto { get; set; } = "";
        public int Quantidade { get; set; }
        public string Pagamento { get; set; } = "";
        public bool Entregue { get; set; }

        public decimal Total => Quantidade * PrecoUnitario;
    }

    // =====================================================================
    // CRUD: guarda os pedidos em um VETOR (array) e salva em pedidos.json.
    // Quando for ligar o MySQL, é AQUI que você troca o vetor por comandos SQL
    // (INSERT, SELECT, UPDATE, DELETE). O resto do programa não muda.
    // =====================================================================
    class PedidoCrud
    {
        const int Capacidade = 100;
        const string ArquivoJson = "pedidos.json";

        Pedido[] pedidos = new Pedido[Capacidade];
        int quantidade = 0;      // quantos pedidos existem no vetor
        int proximoNumero = 1;   // "auto incremento" (no MySQL será AUTO_INCREMENT)

        // ---------- CREATE ----------
        public bool Criar(Pedido p)
        {
            if (quantidade >= Capacidade) return false;   // vetor cheio
            p.Numero = proximoNumero++;
            pedidos[quantidade++] = p;
            SalvarJson();
            return true;
        }

        // ---------- READ ----------
        public Pedido[] Listar() => pedidos.Take(quantidade).ToArray();

        public Pedido? Buscar(int numero)
        {
            int i = IndiceDe(numero);
            return i >= 0 ? pedidos[i] : null;            // operador ternário
        }

        // ---------- UPDATE ----------
        public bool Atualizar(Pedido p)
        {
            int i = IndiceDe(p.Numero);
            if (i < 0) return false;
            pedidos[i] = p;
            SalvarJson();
            return true;
        }

        // ---------- DELETE ----------
        public bool Excluir(int numero)
        {
            int i = IndiceDe(numero);
            if (i < 0) return false;

            // "puxa" os elementos seguintes uma posição para a esquerda
            for (int j = i; j < quantidade - 1; j++)
                pedidos[j] = pedidos[j + 1];

            pedidos[--quantidade] = null!;
            SalvarJson();
            return true;
        }

        // ---------- Busca sequencial no vetor ----------
        int IndiceDe(int numero)
        {
            for (int i = 0; i < quantidade; i++)
                if (pedidos[i].Numero == numero) return i;
            return -1;
        }

        // ---------- Arquivo JSON (entregável da 2ª entrega) ----------
        public void SalvarJson()
        {
            var opcoes = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping   // mantém acentos
            };
            File.WriteAllText(ArquivoJson, JsonSerializer.Serialize(Listar(), opcoes), Encoding.UTF8);
        }

        public void CarregarJson()
        {
            if (!File.Exists(ArquivoJson)) return;
            try
            {
                var opcoes = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var lista = JsonSerializer.Deserialize<Pedido[]>(File.ReadAllText(ArquivoJson), opcoes);
                if (lista == null) return;

                quantidade = Math.Min(lista.Length, Capacidade);
                Array.Copy(lista, pedidos, quantidade);
                proximoNumero = quantidade > 0 ? pedidos.Take(quantidade).Max(p => p.Numero) + 1 : 1;
            }
            catch (JsonException)
            {
                Console.WriteLine("Aviso: pedidos.json está inválido e foi ignorado.");
            }
        }
    }

    // =====================================================================
    // PROGRAMA: menu no console
    // =====================================================================
    class Program
    {
        // Vetores com as opções (iguais às do site)
        static readonly string[] Cardapio =
        {
            "X-Tudo Especial", "Hot Dog Completo", "X-Frango com Catupiry",
            "Mini Pizza Broto", "Mini Pizza de Calabresa",
            "Açaí 500ml Completo", "Sorvete Casquinha Dupla", "Milk-shake de Morango 300ml",
            "Refrigerante Lata 350ml", "Suco Natural de Laranja 400ml",
            "Batata Frita Média", "Coxinha (2 unidades)", "Pastel de Queijo", "Pão de Queijo (6 unidades)"
        };
        static readonly string[] Pagamentos = { "Pix", "Cartão na entrega", "Dinheiro" };

        static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            var crud = new PedidoCrud();
            crud.CarregarJson();

            int opcao;
            do
            {
                Console.WriteLine("\n===== TUDO 10 DELIVERY - PEDIDOS =====");
                Console.WriteLine("1 - Cadastrar pedido");
                Console.WriteLine("2 - Listar pedidos");
                Console.WriteLine("3 - Buscar pedido pelo número");
                Console.WriteLine("4 - Atualizar pedido");
                Console.WriteLine("5 - Excluir pedido");
                Console.WriteLine("0 - Sair");
                opcao = LerInteiro("Escolha", 0, 5);

                switch (opcao)
                {
                    case 1: Cadastrar(crud); break;
                    case 2: ListarTodos(crud); break;
                    case 3: BuscarPorNumero(crud); break;
                    case 4: Atualizar(crud); break;
                    case 5: Excluir(crud); break;
                }
            } while (opcao != 0);

            Console.WriteLine("Até logo!");
        }

        // ---------- Telas do CRUD ----------
        static void Cadastrar(PedidoCrud crud)
        {
            Console.WriteLine("\n--- NOVO PEDIDO ---");
            var p = new Pedido
            {
                Cliente = LerTexto("Nome do cliente", 50),
                Telefone = LerNumerico("Telefone (só números, com DDD)", 10, 11),
                Endereco = LerTexto("Endereço de entrega", 80),
                Produto = Escolher("Produto", Cardapio),
                Quantidade = LerInteiro("Quantidade (1 a 20)", 1, 20),
                Pagamento = Escolher("Pagamento", Pagamentos)
            };

            Console.WriteLine(crud.Criar(p)
                ? $"Pedido #{p.Numero} cadastrado! Total: R$ {p.Total:F2}"
                : "Limite de pedidos atingido.");
        }

        static void ListarTodos(PedidoCrud crud)
        {
            var lista = crud.Listar();
            if (lista.Length == 0) { Console.WriteLine("\nNenhum pedido cadastrado."); return; }

            Console.WriteLine("\n" + "Nº".PadRight(5) + "Cliente".PadRight(21) + "Produto".PadRight(31)
                              + "Qtd".PadRight(5) + "Total".PadRight(11) + "Status");
            foreach (var p in lista)
            {
                string status = p.Entregue ? "Entregue" : "Em andamento";
                Console.WriteLine(p.Numero.ToString().PadRight(5) + Cortar(p.Cliente, 19).PadRight(21)
                                  + Cortar(p.Produto, 29).PadRight(31) + p.Quantidade.ToString().PadRight(5)
                                  + ("R$ " + p.Total.ToString("F2")).PadRight(11) + status);
            }
            Console.WriteLine($"Total de pedidos: {lista.Length}");
        }

        static void BuscarPorNumero(PedidoCrud crud)
        {
            int n = LerInteiro("Número do pedido", 1, int.MaxValue);
            var p = crud.Buscar(n);
            if (p == null) Console.WriteLine("Pedido não encontrado.");
            else Detalhar(p);
        }

        static void Atualizar(PedidoCrud crud)
        {
            int n = LerInteiro("Número do pedido a atualizar", 1, int.MaxValue);
            var p = crud.Buscar(n);
            if (p == null) { Console.WriteLine("Pedido não encontrado."); return; }

            Detalhar(p);
            Console.WriteLine("\nPressione Enter para manter o valor atual.");
            p.Cliente = LerTexto("Nome do cliente", 50, p.Cliente);
            p.Telefone = LerNumerico("Telefone", 10, 11, p.Telefone);
            p.Endereco = LerTexto("Endereço de entrega", 80, p.Endereco);
            p.Produto = Escolher("Produto", Cardapio, p.Produto);
            p.Quantidade = LerInteiro("Quantidade (1 a 20)", 1, 20, p.Quantidade);
            p.Pagamento = Escolher("Pagamento", Pagamentos, p.Pagamento);
            p.Entregue = LerSimNao("Pedido entregue?", p.Entregue);

            Console.WriteLine(crud.Atualizar(p) ? "Pedido atualizado!" : "Erro ao atualizar.");
        }

        static void Excluir(PedidoCrud crud)
        {
            int n = LerInteiro("Número do pedido a excluir", 1, int.MaxValue);
            var p = crud.Buscar(n);
            if (p == null) { Console.WriteLine("Pedido não encontrado."); return; }

            Detalhar(p);
            if (LerSimNao("Confirma a exclusão?", null))
                Console.WriteLine(crud.Excluir(n) ? "Pedido excluído!" : "Erro ao excluir.");
            else
                Console.WriteLine("Exclusão cancelada.");
        }

        static void Detalhar(Pedido p)
        {
            Console.WriteLine($"\nPedido #{p.Numero}");
            Console.WriteLine($"  Cliente:   {p.Cliente}");
            Console.WriteLine($"  Telefone:  {p.Telefone}");
            Console.WriteLine($"  Endereço:  {p.Endereco}");
            Console.WriteLine($"  Produto:   {p.Quantidade}x {p.Produto}");
            Console.WriteLine($"  Pagamento: {p.Pagamento}");
            Console.WriteLine($"  Total:     R$ {p.Total:F2}");
            Console.WriteLine($"  Status:    {(p.Entregue ? "Entregue" : "Em andamento")}");
        }

        // ---------- Funções de leitura com validação ----------
        // Se "atual" for informado, apertar Enter mantém o valor atual (usado no Atualizar).

        static string LerTexto(string rotulo, int max, string? atual = null)
        {
            while (true)
            {
                string dica = atual != null ? $" [{atual}]" : "";
                Console.Write($"{rotulo}{dica}: ");
                string t = (Console.ReadLine() ?? "").Trim();
                if (t == "" && atual != null) return atual;
                if (t.Length > 0 && t.Length <= max) return t;
                Console.WriteLine($"Digite de 1 a {max} caracteres.");
            }
        }

        static string LerNumerico(string rotulo, int minDigitos, int maxDigitos, string? atual = null)
        {
            while (true)
            {
                string dica = atual != null ? $" [{atual}]" : "";
                Console.Write($"{rotulo}{dica}: ");
                string t = (Console.ReadLine() ?? "").Trim();
                if (t == "" && atual != null) return atual;
                if (t.All(char.IsDigit) && t.Length >= minDigitos && t.Length <= maxDigitos) return t;
                Console.WriteLine($"Digite apenas números ({minDigitos} a {maxDigitos} dígitos).");
            }
        }

        static int LerInteiro(string rotulo, int min, int max, int? atual = null)
        {
            while (true)
            {
                string dica = atual != null ? $" [{atual}]" : "";
                Console.Write($"{rotulo}{dica}: ");
                string t = (Console.ReadLine() ?? "").Trim();
                if (t == "" && atual != null) return atual.Value;
                if (int.TryParse(t, out int n) && n >= min && n <= max) return n;
                Console.WriteLine("Valor inválido. Digite um número" + (max == int.MaxValue ? "." : $" entre {min} e {max}."));
            }
        }

        static bool LerSimNao(string rotulo, bool? atual)
        {
            while (true)
            {
                string dica = atual != null ? (atual.Value ? " [S]" : " [N]") : "";
                Console.Write($"{rotulo} (S/N){dica}: ");
                string t = (Console.ReadLine() ?? "").Trim().ToUpper();
                if (t == "" && atual != null) return atual.Value;
                if (t == "S") return true;
                if (t == "N") return false;
                Console.WriteLine("Digite S ou N.");
            }
        }

        static string Escolher(string titulo, string[] opcoes, string? atual = null)
        {
            Console.WriteLine($"{titulo}:");
            for (int i = 0; i < opcoes.Length; i++)
                Console.WriteLine($"  {i + 1}) {opcoes[i]}");

            while (true)
            {
                string dica = atual != null ? $" [Enter mantém: {atual}]" : "";
                Console.Write($"Número da opção{dica}: ");
                string t = (Console.ReadLine() ?? "").Trim();
                if (t == "" && atual != null) return atual;
                if (int.TryParse(t, out int n) && n >= 1 && n <= opcoes.Length) return opcoes[n - 1];
                Console.WriteLine("Opção inválida.");
            }
        }

        static string Cortar(string s, int max) => s.Length > max ? s.Substring(0, max - 1) + "…" : s;
    }
}