/* Consultas para mostrar no vídeo a persistência após CADA operação de CRUD.
   Rode no Portal do Azure > seu banco > Query editor. */

-- Tabela 1: Clientes
SELECT * FROM dbo.Clientes ORDER BY Id;

-- Tabela 2: Transacoes
SELECT * FROM dbo.Transacoes ORDER BY Id;

-- Relacionamento entre as duas tabelas
SELECT t.Id, c.Nome AS Cliente, t.Descricao, t.Valor, t.Tipo, t.Status, t.DataTransacao
FROM dbo.Transacoes AS t
JOIN dbo.Clientes  AS c ON c.Id = t.ClienteId
ORDER BY t.Id;

-- Estrutura criada (prova do DDL aplicado)
SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo'
ORDER BY TABLE_NAME, ORDINAL_POSITION;
