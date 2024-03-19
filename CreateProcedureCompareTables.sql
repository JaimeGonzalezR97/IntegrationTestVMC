
CREATE PROCEDURE CompareTables (
    @dbName1 NVARCHAR(100),
    @dbName2 NVARCHAR(100),
    @tableName NVARCHAR(100),
    @compareField NVARCHAR(100)
)
AS
BEGIN
    DECLARE @query NVARCHAR(MAX);

    -- Construir la consulta dinámica
    SET @query = '
        SELECT *, ''Database1'' AS DatabaseName
        FROM ' + QUOTENAME(@dbName1) + '.dbo.' + QUOTENAME(@tableName) + '
        WHERE ' + QUOTENAME(@compareField) + ' NOT IN (
            SELECT ' + QUOTENAME(@compareField) + '
            FROM ' + QUOTENAME(@dbName2) + '.dbo.' + QUOTENAME(@tableName) + '
        )
    ';

    -- Ejecutar la consulta dinámica
    EXEC sp_executesql @query;
END;
