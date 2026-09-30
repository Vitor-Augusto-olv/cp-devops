/* =============================================================================
   DimDim - DDL das tabelas (Azure SQL Database)
   Modelo: Clientes (1) ----< (N) Transacoes

   Pode ser executado quantas vezes for necessário (cria apenas o que não existe).
   Formas de executar:
     a) bash scripts/02-criar-tabelas.sh                      (usa o sqlcmd)
     b) Portal do Azure > seu banco > Query editor > cole este arquivo > Run
   ============================================================================= */

IF OBJECT_ID(N'dbo.Clientes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Clientes
    (
        Id            INT IDENTITY(1,1) NOT NULL,
        Nome          NVARCHAR(100)     NOT NULL,
        Email         NVARCHAR(150)     NOT NULL,
        Telefone      NVARCHAR(20)      NULL,
        DataCadastro  DATETIME2(7)      NOT NULL
            CONSTRAINT DF_Clientes_DataCadastro DEFAULT (DATEADD(HOUR, -3, SYSUTCDATETIME())),  -- horário de Brasília
        Ativo         BIT               NOT NULL
            CONSTRAINT DF_Clientes_Ativo DEFAULT (1),
        CONSTRAINT PK_Clientes PRIMARY KEY (Id)
    );

    -- Um e-mail não pode se repetir entre clientes.
    CREATE UNIQUE INDEX IX_Clientes_Email ON dbo.Clientes (Email);
END;

IF OBJECT_ID(N'dbo.Transacoes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Transacoes
    (
        Id             INT IDENTITY(1,1) NOT NULL,
        ClienteId      INT               NOT NULL,
        Descricao      NVARCHAR(150)     NOT NULL,
        Valor          DECIMAL(18,2)     NOT NULL,
        Tipo           NVARCHAR(20)      NOT NULL,
        Status         NVARCHAR(20)      NOT NULL,
        DataTransacao  DATETIME2(7)      NOT NULL,
        CONSTRAINT PK_Transacoes PRIMARY KEY (Id),

        -- Relacionamento 1:N (não permite excluir cliente que tenha transações)
        CONSTRAINT FK_Transacoes_Clientes_ClienteId
            FOREIGN KEY (ClienteId) REFERENCES dbo.Clientes (Id),

        CONSTRAINT CK_Transacoes_Valor  CHECK (Valor > 0),
        CONSTRAINT CK_Transacoes_Tipo   CHECK (Tipo   IN (N'Pagamento', N'Recebimento', N'Transferencia')),
        CONSTRAINT CK_Transacoes_Status CHECK (Status IN (N'Pendente', N'Concluida', N'Cancelada'))
    );

    CREATE INDEX IX_Transacoes_ClienteId ON dbo.Transacoes (ClienteId);
END;
