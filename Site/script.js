/* ===== TUDO 10 - script.js ===== */
// Armazenamento local (simula o banco enquanto o back-end C# não está ligado)
const ls = {
  get(k, d) { try { return JSON.parse(localStorage.getItem(k)) ?? d; } catch { return d; } },
  set(k, v) { localStorage.setItem(k, JSON.stringify(v)); }
};
const $ = id => document.getElementById(id);
const brl = n => n.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
const esc = s => String(s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
const norm = s => s.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase();
const dados = f => Object.fromEntries(new FormData(f));

// ----- Tema claro/escuro (aplicado já no <head> para não "piscar") -----
document.documentElement.setAttribute('data-theme', localStorage.getItem('theme') || 'dark');
function toggleTheme() {
  const novo = document.documentElement.getAttribute('data-theme') === 'light' ? 'dark' : 'light';
  document.documentElement.setAttribute('data-theme', novo);
  localStorage.setItem('theme', novo);
}

// ----- Produtos (se a foto falhar, aparece o emoji do produto) -----
const PRECO = 10, MAX_QTD = 20;
const U = 'https://images.unsplash.com/photo-';
const PRODUTOS = [
  { id: 1, nome: 'X-Tudo Especial', cat: 'Lanches', emoji: '🍔', foto: U + '1568901346375-23c9450c58cd?auto=format&fit=crop&w=500&q=80' },
  { id: 2, nome: 'Hot Dog Completo', cat: 'Lanches', emoji: '🌭' },
  { id: 3, nome: 'X-Frango com Catupiry', cat: 'Lanches', emoji: '🥪' },
  { id: 4, nome: 'Mini Pizza Broto', cat: 'Pizzas', emoji: '🍕', foto: U + '1590947132387-155cc02f3212?auto=format&fit=crop&w=500&q=80' },
  { id: 5, nome: 'Mini Pizza de Calabresa', cat: 'Pizzas', emoji: '🍕' },
  { id: 6, nome: 'Açaí 500ml Completo', cat: 'Açaí & Doces', emoji: '🍧', foto: 'acai500ml.jpg' },
  { id: 7, nome: 'Sorvete Casquinha Dupla', cat: 'Açaí & Doces', emoji: '🍦' },
  { id: 8, nome: 'Milk-shake de Morango 300ml', cat: 'Açaí & Doces', emoji: '🍓' },
  { id: 9, nome: 'Refrigerante Lata 350ml', cat: 'Bebidas', emoji: '🥤' },
  { id: 10, nome: 'Suco Natural de Laranja 400ml', cat: 'Bebidas', emoji: '🍊' },
  { id: 11, nome: 'Batata Frita Média', cat: 'Porções & Salgados', emoji: '🍟' },
  { id: 12, nome: 'Coxinha (2 unidades)', cat: 'Porções & Salgados', emoji: '🍗' },
  { id: 13, nome: 'Pastel de Queijo', cat: 'Porções & Salgados', emoji: '🥟' },
  { id: 14, nome: 'Pão de Queijo (6 unidades)', cat: 'Porções & Salgados', emoji: '🧀' }
];

function toast(msg, erro) {
  const t = document.createElement('div');
  t.className = 'toast'; t.textContent = msg;
  if (erro) t.style.background = '#ef4444';
  document.body.appendChild(t);
  setTimeout(() => t.remove(), 2500);
}

// ----- Máscaras (só aceitam números) e validação de CPF -----
const MASCARAS = {
  cpf: v => v.replace(/\D/g, '').slice(0, 11).replace(/(\d{3})(\d{1,3})?(\d{1,3})?(\d{1,2})?/,
    (m, a, b, c, d) => a + (b ? '.' + b : '') + (c ? '.' + c : '') + (d ? '-' + d : '')),
  telefone: v => {
    const d = v.replace(/\D/g, '').slice(0, 11);
    if (d.length <= 2) return d ? `(${d}` : '';
    if (d.length <= 6) return `(${d.slice(0, 2)}) ${d.slice(2)}`;
    if (d.length <= 10) return `(${d.slice(0, 2)}) ${d.slice(2, 6)}-${d.slice(6)}`;
    return `(${d.slice(0, 2)}) ${d.slice(2, 7)}-${d.slice(7)}`;
  },
  cep: v => { const d = v.replace(/\D/g, '').slice(0, 8); return d.length > 5 ? d.slice(0, 5) + '-' + d.slice(5) : d; }
};
function cpfValido(c) {
  c = c.replace(/\D/g, '');
  if (c.length !== 11 || /^(\d)\1+$/.test(c)) return false;
  for (let t = 9; t < 11; t++) {
    let s = 0;
    for (let i = 0; i < t; i++) s += c[i] * (t + 1 - i);
    if ((s * 10 % 11) % 10 != c[t]) return false;
  }
  return true;
}

// ----- Sessão e usuários -----
const API = 'http://localhost:5000/api';
// Chamada à API: devolve { ok, dados }
async function api(rota, metodo = 'GET', corpo) {
  try {
    const r = await fetch(API + rota, { method: metodo, headers: { 'Content-Type': 'application/json' }, body: corpo ? JSON.stringify(corpo) : undefined });
    const t = await r.text();
    let dados = t; try { dados = JSON.parse(t); } catch { }
    return { ok: r.ok, dados };
  } catch { toast('Não foi possível falar com a API. Ela está ligada?', 1); return { ok: false, dados: null }; }
}
const msgErro = (r, padrao) => typeof r.dados === 'string' && r.dados ? r.dados : padrao;

// Usuário logado (guardado no navegador após o login na API)
const usuario = () => ls.get('t10_user', null);
function exigirLogin() {
  if (usuario()) return true;
  location.href = 'login.html?next=' + location.pathname.split('/').pop();
  return false;
}
function entrar(user) {
  ls.set('t10_user', user);
  const n = new URLSearchParams(location.search).get('next') || '';
  location.href = /^[a-z]+\.html$/.test(n) ? n : 'index.html';
}

// ----- Carrinho -----
const carrinho = () => ls.get('t10_carrinho', {});
const qtdTotal = () => Object.values(carrinho()).reduce((s, q) => s + q, 0);
const itensCarrinho = () => Object.entries(carrinho()).map(([id, q]) => ({ p: PRODUTOS.find(x => x.id == id), q })).filter(i => i.p);
function salvarCarrinho(c) {
  ls.set('t10_carrinho', c);
  if ($('badge')) $('badge').textContent = qtdTotal();
  if ($('lista')) renderLista();
}
function mudarQtd(id, d) {
  const c = carrinho();
  c[id] = Math.min(MAX_QTD, (c[id] || 0) + d);
  if (c[id] <= 0) delete c[id];
  salvarCarrinho(c);
}
function removerItem(id) { const c = carrinho(); delete c[id]; salvarCarrinho(c); }
function adicionarCarrinho(id) {
  if ((carrinho()[id] || 0) >= MAX_QTD) return toast(`Máximo de ${MAX_QTD} unidades por item`, 1);
  mudarQtd(id, 1);
  toast(`"${PRODUTOS.find(p => p.id === id).nome}" adicionado ao carrinho!`);
}

// ----- Layout comum (cabeçalho, menu e rodapé) -----
function montarLayout() {
  const pag = location.pathname.split('/').pop() || 'index.html', u = usuario();
  $('topo').innerHTML = `
    <a href="index.html" class="logo-container"><div class="badge-10">10</div>
      <div class="logo-text"><h1>TUDO 10</h1><span>DELIVERY R$10,00</span></div></a>
    <form class="search-bar" id="buscaForm"><input type="text" id="searchInput" maxlength="40" autocomplete="off" placeholder="Buscar burguer, açaí, pizza..."></form>
    <div class="nav-actions"><button class="theme-toggle" onclick="toggleTheme()">🌓 Tema</button>
      ${u ? `<a href="perfil.html" class="btn-primary">Olá, ${esc(u.nome.split(' ')[0])}</a>` : '<a href="login.html" class="btn-primary">Entrar / Cadastrar</a>'}</div>`;
  const links = [['index.html', 'Início / Produtos'], ['carrinho.html', 'Carrinho'], ['pedidos.html', 'Pedidos Feitos'], ['perfil.html', 'Meu Perfil']];
  $('menu').innerHTML = links.map(([h, t]) => `<a href="${h}" class="${h === pag ? 'active' : ''}">${t}${h === 'carrinho.html' ? `<span class="badge" id="badge">${qtdTotal()}</span>` : ''}</a>`).join('');
  $('rodape').innerHTML = '<p>&copy; 2026 TUDO 10 Delivery - Todos os direitos reservados.</p><p>Todos os dias, das 11h às 23h · contato@tudo10.com.br</p>';
  $('buscaForm').onsubmit = e => {
    e.preventDefault();
    if ($('grid')) filtrar(); else location.href = 'index.html?q=' + encodeURIComponent($('searchInput').value.trim());
  };
}

// ----- Página inicial: filtro por categoria + busca em tempo real -----
let catAtual = 'Todos';
const card = p => `<div class="card-produto"><div class="thumb"><span>${p.emoji}</span>${p.foto ? `<img src="${p.foto}" alt="${esc(p.nome)}" loading="lazy" onerror="this.remove()">` : ''}</div>
  <span class="cat">${esc(p.cat)}</span><h3>${esc(p.nome)}</h3><p class="preco">${brl(PRECO)}</p>
  <button class="btn-primary" onclick="adicionarCarrinho(${p.id})">Comprar</button></div>`;
function filtrar() {
  const t = norm($('searchInput').value.trim());
  $('chips').innerHTML = ['Todos', ...new Set(PRODUTOS.map(p => p.cat))].map(c => `<button class="chip ${c === catAtual ? 'on' : ''}" data-cat="${esc(c)}">${esc(c)}</button>`).join('');
  const lista = PRODUTOS.filter(p => (catAtual === 'Todos' || p.cat === catAtual) && norm(p.nome + ' ' + p.cat).includes(t));
  $('grid').innerHTML = lista.map(card).join('') || '<p class="vazio">Nenhum produto encontrado. Tente outro nome ou categoria.</p>';
}
function initIndex() {
  $('searchInput').value = new URLSearchParams(location.search).get('q') || '';
  $('searchInput').addEventListener('input', filtrar);
  $('chips').onclick = e => { if (e.target.dataset.cat) { catAtual = e.target.dataset.cat; filtrar(); } };
  filtrar();
}

// ----- Carrinho / finalizar pedido -----
function renderLista() {
  const itens = itensCarrinho(), total = itens.reduce((s, i) => s + i.q * PRECO, 0);
  $('checkout').hidden = !itens.length;
  $('lista').innerHTML = itens.length
    ? itens.map(i => `<div class="linha"><span class="em">${i.p.emoji}</span>
        <div class="info"><strong>${esc(i.p.nome)}</strong><small>${brl(PRECO)} cada</small></div>
        <div class="qtd"><button onclick="mudarQtd(${i.p.id},-1)">−</button><span>${i.q}</span><button onclick="mudarQtd(${i.p.id},1)">+</button></div>
        <strong>${brl(i.q * PRECO)}</strong><button class="rem" title="Remover" onclick="removerItem(${i.p.id})">🗑</button></div>`).join('')
      + `<p class="total">Total: <strong>${brl(total)}</strong> <small>(entrega grátis)</small></p>`
    : '<p class="vazio">Seu carrinho está vazio. <a href="index.html">Ver cardápio</a></p>';
}
function initCarrinho() {
  const u = usuario();
  $('checkout').innerHTML = u
    ? `<form id="formPedido"><h3>Entrega e pagamento</h3>
        <div class="form-group"><label>Endereço de entrega</label><input name="endereco" required maxlength="80" value="${esc(u.endereco)}"></div>
        <div class="form-group"><label>Pagamento</label><select name="pagamento"><option>Pix</option><option>Cartão na entrega</option><option>Dinheiro</option></select></div>
        <div class="form-group"><label>Observações (opcional)</label><input name="obs" maxlength="100" placeholder="Ex.: sem cebola"></div>
        <button class="btn-primary" style="width: 100%;">Finalizar Pedido</button></form>`
    : '<p class="vazio">Entre na sua conta para finalizar o pedido.</p><a href="login.html?next=carrinho.html" class="btn-primary" style="display:block;text-align:center">Entrar / Cadastrar</a>';
  if (u) $('formPedido').onsubmit = finalizarPedido;
  renderLista();
}
async function finalizarPedido(e) {
  e.preventDefault();
  const d = dados(e.target), u = usuario();
  const itens = itensCarrinho().map(i => ({ produtoId: i.p.id, quantidade: i.q }));
  if (!itens.length) return;
  const r = await api('/pedidos', 'POST', { usuarioId: u.id, endereco: d.endereco, pagamento: d.pagamento, observacoes: d.obs, itens });
  if (!r.ok) return toast(msgErro(r, 'Não foi possível enviar o pedido.'), 1);
  ls.set('t10_carrinho', {});
  toast(`Pedido #${r.dados.numero} realizado com sucesso!`);
  setTimeout(() => location.href = 'pedidos.html', 1200);
}

// ----- Pedidos (histórico, exportar JSON, excluir) -----
const statusPedido = p => { const m = (Date.now() - new Date(p.criadoEm)) / 60000; return m < 1 ? 'Recebido' : m < 3 ? 'Em preparo' : m < 6 ? 'Saiu para entrega' : 'Entregue'; };
function baixarJSON(nome, obj) {
  const a = document.createElement('a');
  a.href = URL.createObjectURL(new Blob([JSON.stringify(obj, null, 2)], { type: 'application/json' }));
  a.download = nome; a.click();
  setTimeout(() => URL.revokeObjectURL(a.href), 1000);
}
function initPedidos() {
  if (!exigirLogin()) return;
  let ps = [];
  const render = async () => {
    const r = await api(`/pedidos?usuarioId=${usuario().id}`);
    if (!r.ok) return;
    ps = r.dados.reverse();
    $('btnTodos').hidden = !ps.length;
    $('listaPedidos').innerHTML = ps.map(p => `<div class="form-box wide pedido">
      <p><strong>Pedido #${p.numero}</strong> · ${new Date(p.criadoEm).toLocaleString('pt-BR')}</p>
      <ul>${p.itens.map(i => `<li>${i.quantidade}x ${esc(i.nome)} - ${brl(i.subtotal)}</li>`).join('')}</ul>
      <p>Total: <strong>${brl(p.total)}</strong> · ${esc(p.pagamento)}</p><p>Entrega: ${esc(p.endereco)}</p>
      <p class="status ${statusPedido(p) === 'Entregue' ? 'ok' : ''}">Status: ${statusPedido(p)}</p>
      <div class="acoes"><button class="btn-primary" data-json="${p.numero}">Baixar JSON</button>
      <button class="btn-primary" style="background:#ef4444" data-del="${p.numero}">Excluir</button></div></div>`).join('')
      || '<p class="vazio">Você ainda não fez pedidos. <a href="index.html">Ver cardápio</a></p>';
  };
  $('listaPedidos').onclick = async e => {
    const n = e.target.dataset.json || e.target.dataset.del;
    if (!n) return;
    if (e.target.dataset.json) return baixarJSON(`pedido-${n}.json`, ps.find(p => p.numero == n));
    if (confirm(`Excluir o pedido #${n}?`)) { await api(`/pedidos/${n}`, 'DELETE'); render(); }
  };
  $('btnTodos').onclick = () => baixarJSON('pedidos-tudo10.json', ps);
  render();
  setInterval(render, 30000);
}

// ----- Login / cadastro -----
function initLogin() {
  if (usuario()) { location.href = 'perfil.html'; return; }
  document.querySelectorAll('[data-tab]').forEach(b => b.onclick = () => {
    document.querySelectorAll('[data-tab]').forEach(x => x.classList.toggle('on', x === b));
    ['formLogin', 'formCadastro'].forEach(id => $(id).hidden = id !== b.dataset.tab);
  });
  $('formLogin').onsubmit = async e => {
    e.preventDefault();
    const d = dados(e.target);
    const r = await api('/login', 'POST', { email: d.email, senha: d.senha });
    if (!r.ok) return toast('E-mail ou senha incorretos', 1);
    entrar(r.dados);
  };
  $('formCadastro').onsubmit = async e => {
    e.preventDefault();
    const d = dados(e.target);
    if (!cpfValido(d.cpf)) return toast('CPF inválido', 1);
    if (d.telefone.length < 14) return toast('Telefone incompleto', 1);
    if (d.cep.length < 9) return toast('CEP incompleto', 1);
    const r = await api('/usuarios', 'POST', d);
    if (!r.ok) return toast(msgErro(r, 'Não foi possível cadastrar.'), 1);
    entrar(r.dados);
  };
}

// ----- Perfil (ler, atualizar, sair, excluir conta) -----
function initPerfil() {
  if (!exigirLogin()) return;
  const u = usuario(), f = $('formPerfil');
  ['nome', 'email', 'cpf', 'telefone', 'cep', 'endereco'].forEach(k => f.elements[k].value = u[k]);
  f.onsubmit = async e => {
    e.preventDefault();
    const d = dados(f);
    if (d.telefone.length < 14 || d.cep.length < 9) return toast('Telefone ou CEP incompleto', 1);
    const r = await api(`/usuarios/${u.id}`, 'PUT', d);
    if (!r.ok) return toast(msgErro(r, 'Não foi possível salvar.'), 1);
    ls.set('t10_user', r.dados);
    toast('Dados atualizados!');
  };
  $('btnSair').onclick = () => { localStorage.removeItem('t10_user'); location.href = 'index.html'; };
  $('btnExcluir').onclick = async () => {
    if (!confirm('Excluir sua conta e todos os seus pedidos?')) return;
    if (!(await api(`/usuarios/${u.id}`, 'DELETE')).ok) return toast('Não foi possível excluir.', 1);
    localStorage.removeItem('t10_user');
    location.href = 'index.html';
  };
}

// ----- Inicialização -----
window.addEventListener('DOMContentLoaded', () => {
  montarLayout();
  document.querySelectorAll('[data-mask]').forEach(inp => inp.addEventListener('input', () => inp.value = MASCARAS[inp.dataset.mask](inp.value)));
  if ($('grid')) initIndex();
  if ($('lista')) initCarrinho();
  if ($('listaPedidos')) initPedidos();
  if ($('formLogin')) initLogin();
  if ($('formPerfil')) initPerfil();
});