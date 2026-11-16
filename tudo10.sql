CREATE DATABASE IF NOT EXISTS tudo10 CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE tudo10;

DROP TABLE IF EXISTS itens_pedido;
DROP TABLE IF EXISTS pedidos;
DROP TABLE IF EXISTS produtos;
DROP TABLE IF EXISTS usuarios;

CREATE TABLE usuarios (
  id         INT AUTO_INCREMENT PRIMARY KEY,
  nome       VARCHAR(50)  NOT NULL,
  email      VARCHAR(60)  NOT NULL UNIQUE,
  cpf        VARCHAR(14)  NOT NULL UNIQUE,
  telefone   VARCHAR(15)  NOT NULL,
  cep        VARCHAR(9)   NOT NULL,
  endereco   VARCHAR(80)  NOT NULL,
  senha_hash VARCHAR(255) NOT NULL,
  criado_em  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE produtos (
  id        INT PRIMARY KEY,
  nome      VARCHAR(60)   NOT NULL,
  categoria VARCHAR(30)   NOT NULL,
  preco     DECIMAL(10,2) NOT NULL DEFAULT 10.00,
  ativo     TINYINT(1)    NOT NULL DEFAULT 1
);

CREATE TABLE pedidos (
  numero      INT AUTO_INCREMENT PRIMARY KEY,
  usuario_id  INT           NOT NULL,
  criado_em   DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
  endereco    VARCHAR(80)   NOT NULL,
  pagamento   VARCHAR(30)   NOT NULL,
  observacoes VARCHAR(100)  NOT NULL DEFAULT '',
  status      VARCHAR(20)   NOT NULL DEFAULT 'Recebido',
  total       DECIMAL(10,2) NOT NULL,
  FOREIGN KEY (usuario_id) REFERENCES usuarios(id) ON DELETE CASCADE
);

CREATE TABLE itens_pedido (
  id             INT AUTO_INCREMENT PRIMARY KEY,
  pedido_numero  INT           NOT NULL,
  produto_id     INT           NOT NULL,
  quantidade     INT           NOT NULL CHECK (quantidade BETWEEN 1 AND 20),
  preco_unitario DECIMAL(10,2) NOT NULL,
  subtotal       DECIMAL(10,2) NOT NULL,
  FOREIGN KEY (pedido_numero) REFERENCES pedidos(numero) ON DELETE CASCADE,
  FOREIGN KEY (produto_id)    REFERENCES produtos(id)
);

INSERT INTO produtos (id, nome, categoria) VALUES
 (1,  'X-Tudo Especial',               'Lanches'),
 (2,  'Hot Dog Completo',              'Lanches'),
 (3,  'X-Frango com Catupiry',         'Lanches'),
 (4,  'Mini Pizza Broto',              'Pizzas'),
 (5,  'Mini Pizza de Calabresa',       'Pizzas'),
 (6,  'Açaí 500ml Completo',           'Açaí & Doces'),
 (7,  'Sorvete Casquinha Dupla',       'Açaí & Doces'),
 (8,  'Milk-shake de Morango 300ml',   'Açaí & Doces'),
 (9,  'Refrigerante Lata 350ml',       'Bebidas'),
 (10, 'Suco Natural de Laranja 400ml', 'Bebidas'),
 (11, 'Batata Frita Média',            'Porções & Salgados'),
 (12, 'Coxinha (2 unidades)',          'Porções & Salgados'),
 (13, 'Pastel de Queijo',              'Porções & Salgados'),
 (14, 'Pão de Queijo (6 unidades)',    'Porções & Salgados');