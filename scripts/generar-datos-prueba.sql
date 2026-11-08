/*
    Generación de datos sintéticos para la verificación del requisito RNF-01.

    El umbral de respuesta establecido por dicho requisito solo resulta
    verificable sobre un catálogo de volumen apreciable: con unas decenas de
    registros, cualquier estrategia de consulta responde de forma inmediata y
    la medición carece de significado.

    El script genera ítems con dimensiones distribuidas en los rangos
    habituales de la chapa comercial, asociados a una empresa ya existente.
    Los valores no son aleatorios puros: se componen a partir de conjuntos de
    espesores y formatos realistas, de modo que la distribución de los datos
    se asemeje a la de un catálogo real y la medición resulte representativa.

    USO
        1. Sustituir el identificador fiscal por el de una empresa registrada.
        2. Ajustar @Cantidad al volumen deseado.
        3. Ejecutar mediante:

           docker exec -i metalflow-db /opt/mssql-tools18/bin/sqlcmd \
             -S localhost -U sa -P 'SU_CONTRASENA' -C -d MetalFlowDb \
             -i generar-datos-prueba.sql

    ADVERTENCIA
        Destinado exclusivamente al entorno de desarrollo. Los registros
        generados son ficticios y han de eliminarse antes de cualquier
        demostración del prototipo.
*/

SET NOCOUNT ON;

DECLARE @NifEmpresa NVARCHAR(15) = 'B09887766';   -- Sustituir por el NIF correcto
DECLARE @Cantidad   INT          = 100000;          -- Ajustar cantidad

DECLARE @EmpresaId UNIQUEIDENTIFIER =
    (SELECT TOP 1 Id FROM EMPRESA WHERE Nif = @NifEmpresa);

IF @EmpresaId IS NULL
BEGIN
    RAISERROR('No existe ninguna empresa con el identificador fiscal indicado.', 16, 1);
    RETURN;
END

-- Conjuntos de valores realistas -------------------------------------------

DECLARE @Espesores TABLE (Valor DECIMAL(7,2));
INSERT INTO @Espesores VALUES (1.5),(2),(3),(4),(5),(6),(8),(10),(12),(15),(20),(25);

DECLARE @Largos TABLE (Valor DECIMAL(9,2));
INSERT INTO @Largos VALUES (300),(450),(600),(800),(950),(1000),(1200),(1500),(2000),(2500);

DECLARE @Anchos TABLE (Valor DECIMAL(9,2));
INSERT INTO @Anchos VALUES (200),(300),(410),(480),(600),(750),(1000),(1250);

-- Generación ----------------------------------------------------------------

DECLARE @i INT = 0;
DECLARE @Espesor DECIMAL(7,2);
DECLARE @Largo DECIMAL(9,2);
DECLARE @Ancho DECIMAL(9,2);
DECLARE @MaterialId INT;
DECLARE @Peso DECIMAL(9,2);

WHILE @i < @Cantidad
BEGIN
    -- Extracción aleatoria segura garantizando un único valor no nulo
    SET @Espesor = (SELECT TOP 1 Valor FROM @Espesores ORDER BY NEWID());
    SET @Largo   = (SELECT TOP 1 Valor FROM @Largos ORDER BY NEWID());
    SET @Ancho   = (SELECT TOP 1 Valor FROM @Anchos ORDER BY NEWID());
    SET @MaterialId = (SELECT TOP 1 Id FROM MATERIAL ORDER BY NEWID());

    -- Peso aproximado para acero, con densidad de 7,85 kg/dm3
    SET @Peso = ROUND((@Largo / 1000.0) * (@Ancho / 1000.0) * (@Espesor / 1000.0) * 7850, 2);

INSERT INTO ITEM_INVENTARIO
(Id, EmpresaId, MaterialId, CargaId, LargoMm, AnchoMm, EspesorMm,
 Cantidad, PesoKg, Ubicacion, Estado, Clasificacion, FechaAlta)
VALUES
    (NEWID(), @EmpresaId, @MaterialId, NULL, @Largo, @Ancho, @Espesor,
     (ABS(CHECKSUM(NEWID())) % 10) + 1,
     @Peso,
     CONCAT('Nave ', (ABS(CHECKSUM(NEWID())) % 3) + 1),
     1, -- Disponible
     CASE WHEN (ABS(CHECKSUM(NEWID())) % 5) = 0 THEN 2 ELSE 1 END,
     DATEADD(MINUTE, -(ABS(CHECKSUM(NEWID())) % 525600), SYSUTCDATETIME()));

SET @i = @i + 1;
END

PRINT CONCAT('Generados ', @Cantidad, ' ítems de prueba.');

SELECT COUNT(*) AS TotalEnCatalogo FROM ITEM_INVENTARIO WHERE Estado = 1;
GO